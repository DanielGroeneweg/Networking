using System;
using System.Collections.Generic;
public class TexasHoldemBoard
{
    #region variables

    public delegate void UpdatePotEvent(int potMoney);
    event UpdatePotEvent OnUpdatePot;

    public delegate void UpdatePlayerMoneyEvent(int player, int playerMoney);
    event UpdatePlayerMoneyEvent OnUpdatePlayerMoney;

    public delegate void NextPlayerEvent(int activePlayer, int actionTaken);
    event NextPlayerEvent OnNextPlayer;

    public delegate void ChangePlayerEvent(int actionTaken, int player);
    event ChangePlayerEvent OnChangePlayerOptions;

    public delegate void NextPhaseEvent(int phase);
    event NextPhaseEvent OnNextPhase;

    public delegate void NewRoundEvent();
    event NewRoundEvent OnNewRound;

    public delegate void DealCardsEvent(Card card1, Card card2, int player);
    event DealCardsEvent OnDealPlayerCards;

    public delegate void DealTableCardsEvent(Card[] cards);
    event DealTableCardsEvent OnDealTableCards;

    public delegate void InvalidActionEvent(string error, int player);
    event InvalidActionEvent OnInvalidAction;

    public delegate void InvalidNewRoundEvent(string error, int player);
    event InvalidNewRoundEvent OnInvalidNewRound;

    public delegate void PlayerInfoEvent(int playerID);
    event PlayerInfoEvent OnPlayerInfoReceived;

    // The amount of players
    int _playerAmount;

    // The current active player: 1 and above for player, 0 on game over.
    int _activePlayer = 0;

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
    List<Player> players = new();
    public TexasHoldemBoard() { }
    #endregion

    #region Actions
    public void Bet(int player, int money)
    {
        if (!ValidAction(player)) return;

        if (money <= 0) return;

        if (players[_activePlayer - 1].money < money)
        {
            Logger.LogInfo($"Player {player} tried betting ${money} but only has ${players[player - 1].money}");
            OnInvalidAction?.Invoke($"Tried betting ${money} but only has ${players[player - 1].money}", player);
            return;
        }

        pot += money;
        OnUpdatePot?.Invoke(pot);
        OnUpdatePlayerMoney?.Invoke(_activePlayer, players[_activePlayer - 1].money);
        players[_activePlayer - 1].Bet(money);

        betToBeMatched = players[_activePlayer - 1].betMoney;

        lastPickedAction = BettingActions.Bet;

        Logger.LogInfo($"Player {_activePlayer} took action: {lastPickedAction}, money in pot: {pot} | player money: {players[activePlayer - 1].money}");

        FinishTurn();
    }
    public void Check(int player)
    {
        if (!ValidAction(player)) return;

        lastPickedAction = BettingActions.Check;

        Logger.LogInfo($"Player {_activePlayer} took action: {lastPickedAction}, money in pot: {pot} | player money: {players[activePlayer - 1].money}");

        FinishTurn();
    }
    public void Fold(int player)
    {
        if (!ValidAction(player)) return;

        Logger.LogInfo($"Player {_activePlayer} took action: {BettingActions.Fold}, money in pot: {pot} | player money: {players[activePlayer - 1].money}");

        players[_activePlayer - 1].isInHand = false;
        // Check if only one player remains
        int playersStillIn = 0;
        int lastPlayerIndex = -1;

        for (int i = 0; i < players.Count; i++)
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

        FinishTurn();
    }
    public void Raise(int player, int money)
    {
        if (!ValidAction(player)) return;

        if (money <= 0)
        {
            Logger.LogInfo($"Player {_activePlayer} put in a bet that is too small");
            OnInvalidAction?.Invoke($"Put in a bet that is too small", player);
            return;
        }

        int moneyIncrease = betToBeMatched - players[_activePlayer - 1].betMoney + money;

        if (players[_activePlayer - 1].money < moneyIncrease)
        {
            Logger.LogInfo($"Player {_activePlayer} doesn't have enough money to bet {moneyIncrease}. has: {players[_activePlayer - 1].money}");
            OnInvalidAction?.Invoke($"You don't have enough money to bet {moneyIncrease}. Money: {players[_activePlayer - 1].money}", player);
            return;
        }

        players[_activePlayer - 1].Bet(moneyIncrease);
        betToBeMatched = players[_activePlayer - 1].betMoney;
        pot += moneyIncrease;
        OnUpdatePot?.Invoke(pot);
        OnUpdatePlayerMoney?.Invoke(_activePlayer, players[_activePlayer - 1].money);
        lastPickedAction = BettingActions.Raise;

        Logger.LogInfo($"Player {_activePlayer} took action: {lastPickedAction}, money in pot: {pot} | player money: {players[activePlayer - 1].money}");

        FinishTurn();
    }
    public void Call(int player)
    {
        if (!ValidAction(player)) return;

        int moneyForPot = (int)MathF.Min(players[_activePlayer - 1].money, betToBeMatched - players[_activePlayer - 1].betMoney);
        pot += moneyForPot;
        OnUpdatePot?.Invoke(pot);
        players[_activePlayer - 1].Bet(moneyForPot);
        OnUpdatePlayerMoney?.Invoke(_activePlayer, players[_activePlayer - 1].money);
        lastPickedAction = BettingActions.Call;

        Logger.LogInfo($"Player {_activePlayer} took action: {lastPickedAction}, money in pot: {pot} | player money: {players[activePlayer - 1].money}");

        FinishTurn();
    }
    bool ValidAction(int player)
    {
        // Prevent actions taken when there is no game being played
        if (!gameRunning)
        {
            Logger.LogInfo($"There is no game being played player {player}!");
            OnInvalidAction?.Invoke($"There is no game being played player {player}!", player);
            return false;
        }

        // Prevent players from making moves out of turn
        if (player != _activePlayer)
        {
            Logger.LogInfo($"It is not your turn player {player}. active player: {_activePlayer}");
            OnInvalidAction?.Invoke($"It is not your turn player {player}. active player: {_activePlayer}", player);
            return false;
        }

        // Skip player who already folded
        if (!players[_activePlayer - 1].isInHand)
        {
            Logger.LogInfo($"Player {_activePlayer} is out! Skipping turn!");
            players[_activePlayer - 1].tookAction = true;

            NextTurn(lastPickedAction);
            return false;
        }

        return true;
    }
    #endregion

    #region GameLogic
    void FinishTurn()
    {
        players[_activePlayer - 1].tookAction = true;

        if (IsBettingRoundComplete())
        {
            Logger.LogInfo($"Phase {currentPhase} completed, advancing to phase {(GamePhases)(currentPhase + 1)}");
            AdvancePhase();
        }

        else
            NextTurn(lastPickedAction);
    }
    void NextTurn(BettingActions actionTaken)
    {
        // Don't assume two players!
        Logger.LogInfo($"Moving from Player {_activePlayer} to Player {3 - _activePlayer}");
        _activePlayer = 3 - _activePlayer;

        OnNextPlayer?.Invoke(_activePlayer, (int)actionTaken);
        OnChangePlayerOptions?.Invoke((int)actionTaken, _activePlayer);
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
                OnDealTableCards?.Invoke(cardsOnBoard);
                break;
            case GamePhases.Turn:
                cardsOnBoard[3] = deckOfCards.DrawCard();
                Logger.LogInfo($"New cards: {cardsOnBoard[3].ToString()}");
                OnDealTableCards?.Invoke(cardsOnBoard);
                break;
            case GamePhases.River:
                cardsOnBoard[4] = deckOfCards.DrawCard();
                Logger.LogInfo($"New cards: {cardsOnBoard[4].ToString()}");
                OnDealTableCards?.Invoke(cardsOnBoard);
                break;
        }

        OnNextPhase?.Invoke((int)currentPhase);

        if (currentPhase == GamePhases.Showdown)
        {
            DetermineWinner();
        }

        else NextTurn(BettingActions.None);
    }
    void EndRound(int winner)
    {
        Logger.LogInfo($"round ended with winner: player {winner}");
        
        if (winner == -1)
        {
            List<Player> winners = new();
            foreach(Player player in players)
            {
                if (player.isInHand) winners.Add(player);
            }

            foreach(Player _winner in winners)
            {
                _winner.AddMoney(pot/winners.Count);
            }
        }
        
        // TODO remove 2 player assumption
        else 
            players[winner - 1].AddMoney(pot);

        OnUpdatePlayerMoney.Invoke(1, players[0].money);
        OnUpdatePlayerMoney.Invoke(2, players[1].money);

        /*if (players[0].money <= 0 || players[1].money <= 0) OnGameOver.Invoke(winner);
        
        else
        {
            gameRunning = false;

            //OnEndRound.Invoke(winner);

            deckOfCards = new DeckOfCards(true);
        }
        */
    }
    void DetermineWinner()
    {
        int winner = HandEvaluator.Compare(players, cardsOnBoard);
        EndRound(winner);
    }
    public void StartGame(int playerAmount, int startingMoney)
    {
        if (gameRunning) return;

       _playerAmount = playerAmount;

        for (int i = 0; i < _playerAmount; i++)
        {
            players.Add(null);
        }
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

        for (int i = 0; i <= players.Count - 1; i++)
        {
            cards[0] = deckOfCards.DrawCard();
            cards[1] = deckOfCards.DrawCard();

            if (cards[0] != null && cards[1] != null) { players[i] = new Player(players[i].money, cards); }

            Logger.LogInfo($"Player {i + 1} has been dealt cards: {cards[0].ToString()}, {cards[1].ToString()}");

            OnDealPlayerCards?.Invoke(cards[0], cards[1], i + 1);
        }

        NextTurn(BettingActions.None);

        OnNewRound.Invoke();
    }
    #endregion
}