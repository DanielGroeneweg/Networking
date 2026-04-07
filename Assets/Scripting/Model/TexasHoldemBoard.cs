using System;
using System.Collections.Generic;
using System.Diagnostics;
public class TexasHoldemBoard
{
    public delegate void StartRoundEvent(Player playerData);
    public event StartRoundEvent OnStartRound;

    public delegate void EndRoundEvent(int winner);
    public event EndRoundEvent OnEndRound;

    public delegate void ActivePlayerChangeEvent(int activePlayer, BettingActions chosenAction, int pot);
    public event ActivePlayerChangeEvent OnActivePlayerChange;

    public delegate void BotMoveEvent(BettingActions chosenAction, int botMoney, int pot);
    public event BotMoveEvent OnBotMove;

    public delegate void PhaseChangeEvent(int pot, GamePhases gamePhase, Card[] cardsOnBoard);
    public event PhaseChangeEvent OnPhaseChange;

    public delegate void GameOverEvent(int winner);
    public event GameOverEvent OnGameOver;

    public delegate void PlayerMoneyChangeEvent(int player, int money);
    public event PlayerMoneyChangeEvent OnPlayerMoneyChange;

    public delegate void PotMoneyChangeEvent(int money);
    public event PotMoneyChangeEvent OnPotMoneyChange;

    // The amount of players
    int _playerAmount;

    // The current active player: 1 for player, 2 for CPU, 0 on game over.
    int _activePlayer = 2;

    // The amount of money in the pot
    int pot = 0;

    int betToBeMatched;

    bool gameRunning = false;

    GamePhases currentPhase = GamePhases.PreFlop;

    Card[] cardsOnBoard = new Card[5];

    BettingActions lastPickedAction;

    public int activePlayer
    {
        get
        {
            return _activePlayer;
        }
    }
    DeckOfCards deckOfCards;
    Player[] players = new Player[2];
    public TexasHoldemBoard(int startingMoney)
    {
        deckOfCards = new DeckOfCards(true);
        Card[] cards = new Card[2];

        for(int i = players.Length - 1; i >= 0; i--)
        {
            cards[0] = deckOfCards.DrawCard();
            cards[1] = deckOfCards.DrawCard();

            if (cards[0] != null && cards[1] != null) { players[i] = new Player(startingMoney, cards); }
        }
    }
    public void MakeMove(MoveData moveData)
    {
        if (!gameRunning)
        {
            Logger.LogInfo($"There is no game being played player {moveData.player}!");
            return;
        }

        // Prevent players from making moves out of turn
        if (moveData.player != _activePlayer)
        {
            Logger.LogInfo($"It is not your turn player {moveData.player}. active player: {_activePlayer}");
            return;
        }

        // Skip player who already folded
        if (!players[_activePlayer - 1].isInHand)
        {
            Logger.LogInfo($"Player {_activePlayer} is out! Skipping turn!");
            players[_activePlayer - 1].tookAction = true;

            MoveData _moveData = new MoveData(lastPickedAction, 0, _activePlayer);

            NextTurn(_moveData);
            return;
        }

        switch(moveData.actionTaken)
        {
            case BettingActions.Check:
                break;

            case BettingActions.Bet:
                if (moveData.moneyBet <= 0) return;

                if (players[_activePlayer - 1].money < moveData.moneyBet)
                {
                    Logger.LogInfo($"Player {moveData.player} tried betting {moveData.moneyBet} but only has {players[moveData.player - 1].money} money");
                    if (_activePlayer == 2) { OnBotMove.Invoke(lastPickedAction, pot, players[1].money); }
                    return;
                }

                pot += moveData.moneyBet;
                players[_activePlayer - 1].Bet(moveData.moneyBet);

                betToBeMatched = players[_activePlayer - 1].betMoney;
                break;

            case BettingActions.Call:
                int moneyForPot = (int)MathF.Min(players[_activePlayer - 1].money, betToBeMatched - players[_activePlayer - 1].betMoney);
                pot += moneyForPot;
                players[_activePlayer - 1].Bet(moneyForPot);
                break;

            case BettingActions.Raise:
                if (moveData.moneyBet <= 0)
                {
                    Logger.LogInfo($"Player {_activePlayer} put in a bet that is too small");
                    return;
                }

                int moneyIncrease = betToBeMatched - players[_activePlayer - 1].betMoney + moveData.moneyBet;

                if (players[_activePlayer - 1].money < moneyIncrease)
                {
                    Logger.LogInfo($"Player {_activePlayer} doesn't have enough money to bet {moneyIncrease}. has: {players[_activePlayer - 1].money}");
                    if (_activePlayer == 2) { OnBotMove.Invoke(lastPickedAction, pot, players[1].money); }
                    return;
                }

                players[_activePlayer - 1].Bet(moneyIncrease);
                betToBeMatched = players[_activePlayer - 1].betMoney;
                pot += moneyIncrease;
                break;

            case BettingActions.Fold:
                players[_activePlayer - 1].isInHand = false;
                // Check if only one player remains
                int playersStillIn = 0;
                int lastPlayerIndex = -1;
                moveData.actionTaken = lastPickedAction;

                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i].isInHand)
                    {
                        playersStillIn++;
                        lastPlayerIndex = i;
                    }
                }

                if (playersStillIn == 1)
                {
                    EndRound(lastPlayerIndex + 1);
                    return;
                }
                break;
        }

        OnPlayerMoneyChange.Invoke(_activePlayer, players[_activePlayer - 1].money);

        Logger.LogInfo($"Player {_activePlayer} took action: {moveData.actionTaken}, money in pot: {pot} | player money: {players[activePlayer - 1].money}");

        players[_activePlayer - 1].tookAction = true;

        lastPickedAction = moveData.actionTaken;

        if (IsBettingRoundComplete())
        {
            Logger.LogInfo($"Phase {currentPhase} completed, advancing to phase {(GamePhases)(currentPhase + 1)}");
            AdvancePhase();
        }
        
        else 
            NextTurn(moveData);
    }
    void NextTurn(MoveData moveData)
    {
        // Don't assume two players!
        Logger.LogInfo($"Moving from Player {_activePlayer} to Player {3 - _activePlayer}");
        _activePlayer = 3 - _activePlayer;

        OnActivePlayerChange?.Invoke(_activePlayer, moveData.actionTaken, pot);

        if (_activePlayer == 2) { OnBotMove.Invoke(moveData.actionTaken, pot, players[1].money); }
    }
    bool IsBettingRoundComplete()
    {
        foreach (Player player in players)
        {
            if (!player.isInHand) continue;
            else
            {
                if (player.tookAction && (player.betMoney == betToBeMatched || player.money == 0)) continue;
                else return false;
            }
        }

        Logger.LogInfo("betting round ended!");
        return true;
    }
    void AdvancePhase()
    {
        // Reset Players
        foreach (Player player in players)
        {
            player.tookAction = false;
        }

        currentPhase++;

        switch (currentPhase)
        {
            case GamePhases.Flop:
                cardsOnBoard[0] = deckOfCards.DrawCard();
                cardsOnBoard[1] = deckOfCards.DrawCard();
                cardsOnBoard[2] = deckOfCards.DrawCard();
                Logger.LogInfo($"New cards: {cardsOnBoard[0].ToString()}, {cardsOnBoard[1].ToString()}, {cardsOnBoard[2].ToString()}");
                break;
            case GamePhases.Turn:
                cardsOnBoard[3] = deckOfCards.DrawCard();
                Logger.LogInfo($"New cards: {cardsOnBoard[3].ToString()}");
                break;
            case GamePhases.River:
                cardsOnBoard[4] = deckOfCards.DrawCard();
                Logger.LogInfo($"New cards: {cardsOnBoard[4].ToString()}");
                break;
        }

        OnPhaseChange?.Invoke(pot, currentPhase, cardsOnBoard);

        if (currentPhase == GamePhases.Showdown)
        {
            DetermineWinner();
        }

        else NextTurn(new MoveData(BettingActions.None, 0, _activePlayer));
    }
    void EndRound(int winner)
    {
        Logger.LogInfo($"round ended with winner: player {winner}");
        
        if (winner == -1)
        {
            players[0].AddMoney(pot / 2);
            players[1].AddMoney(pot / 2);
        }
        
        else 
            players[winner - 1].AddMoney(pot);

        OnPlayerMoneyChange.Invoke(1, players[0].money);
        OnPlayerMoneyChange.Invoke(2, players[1].money);

        if (players[0].money <= 0 || players[1].money <= 0) OnGameOver.Invoke(winner);
        
        else
        {
            gameRunning = false;

            OnEndRound.Invoke(winner);

            deckOfCards = new DeckOfCards(true);
            //StartRound();
        }
    }
    void DetermineWinner()
    {
        int winner = HandEvaluator.Compare(players, cardsOnBoard);
        EndRound(winner);
    }
    public void StartRound()
    {
        if (gameRunning) return;


        gameRunning = true;
        pot = 0;
        currentPhase = GamePhases.PreFlop;
        players[0].isInHand = true;
        players[1].isInHand = true;

        deckOfCards = new DeckOfCards(true);
        Card[] cards = new Card[2];
        cardsOnBoard = new Card[5];

        for (int i = 0; i <= players.Length - 1; i++)
        {
            cards[0] = deckOfCards.DrawCard();
            cards[1] = deckOfCards.DrawCard();

            if (cards[0] != null && cards[1] != null) { players[i] = new Player(players[i].money, cards); }

            Logger.LogInfo($"Player {i + 1} has been dealt cards: {cards[0].ToString()}, {cards[1].ToString()}");
        }

        NextTurn(new MoveData(BettingActions.None, 0, 2));
        OnStartRound.Invoke(players[0]);
    }
}