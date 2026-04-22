using System;
using System.Collections.Generic;
public class TexasHoldemBoard
{
    #region variables

    public delegate void UpdatePotEvent(int potMoney);
    public event UpdatePotEvent OnUpdatePot;

    public delegate void UpdatePlayerMoneyEvent(int player, int playerMoney);
    public event UpdatePlayerMoneyEvent OnUpdatePlayerMoney;

    public delegate void NextPlayerEvent(int activePlayer, int actionTaken);
    public event NextPlayerEvent OnNextPlayer;

    public delegate void ChangePlayerEvent(int actionTaken, int player, int pot);
    public event ChangePlayerEvent OnChangePlayerOptions;

    public delegate void NextPhaseEvent(int phase);
    public event NextPhaseEvent OnNextPhase;

    public delegate void NewRoundEvent();
    public event NewRoundEvent OnNewRound;

    public delegate void DealCardsEvent(Card card1, Card card2, int player);
    public event DealCardsEvent OnDealPlayerCards;

    public delegate void DealTableCardsEvent(Card[] cards);
    public event DealTableCardsEvent OnDealTableCards;

    public delegate void InvalidActionEvent(string error, int player);
    public event InvalidActionEvent OnInvalidAction;

    public delegate void InvalidNewRoundEvent(string error);
    public event InvalidNewRoundEvent OnInvalidNewRound;

    public delegate void InvalidNewGameEvent(string error);
    public event InvalidNewGameEvent OnInvalidNewGame;

    public delegate void PlayerInformationEvent(int playerAmount, int startingMoney);
    public event PlayerInformationEvent OnPlayerInformation;

    public delegate void RoundEndEvent(List<int> winners);
    public event RoundEndEvent OnRoundEnd;

    public delegate void GameEndEvent(int winner);
    public event GameEndEvent OnGameEnd;

    // The amount of players
    int _playerAmount;

    // The current active player: 1 and above for player, 0 on game over.
    int _activePlayer = 0;

    // The amount of money in the pot
    int pot = 0;

    int betToBeMatched;

    bool roundRunning = false;

    public bool gameRunning { get; private set; } = false;

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

        if (money <= 0)
        {
            Logger.LogInfo($"Player {_activePlayer} put in a bet that is too small");
            OnInvalidAction?.Invoke($"Put in a bet that is too small", player);
            return;
        }

        if (players[_activePlayer - 1].money < money)
        {
            Logger.LogInfo($"Player {player} tried betting ${money} but only has ${players[player - 1].money}");
            OnInvalidAction?.Invoke($"Tried betting ${money} but only has ${players[player - 1].money}", player);
            return;
        }

        pot += money;
        OnUpdatePot?.Invoke(pot);

        players[_activePlayer - 1].Bet(money);
        OnUpdatePlayerMoney?.Invoke(_activePlayer, players[_activePlayer - 1].money);

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

        // Do a check for if everyone but one person folded
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

        // More than two people still in so we move to the next turn!
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
        pot += moneyIncrease;
        OnUpdatePot?.Invoke(pot);

        players[_activePlayer - 1].Bet(moneyIncrease);
        OnUpdatePlayerMoney?.Invoke(_activePlayer, players[_activePlayer - 1].money);

        betToBeMatched = players[_activePlayer - 1].betMoney;

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
    /// <summary>
    /// Returns whether a player taking an action is doing so while a game is being player, it's their turn
    /// and they are still participating.
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    bool ValidAction(int player)
    {
        // Prevent actions taken when there is no game being played
        if (!roundRunning)
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
            Logger.LogInfo($"Player {_activePlayer} is out!");
            OnInvalidAction?.Invoke($"Player {_activePlayer} is out!", player);
            return false;
        }

        return true;
    }
    #endregion

    #region GameLogic
    /// <summary>
    /// Finishes a player's turn, also checks whether a betting round has been completed or not.
    /// </summary>
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
    /// <summary>
    /// Finds the next player that is still playing and not yet all-in.
    /// </summary>
    /// <param name="actionTaken"></param>
    void NextTurn(BettingActions actionTaken)
    {
        int startPlayer = _activePlayer;
        bool foundNextPlayer = false;
        while (!foundNextPlayer)
        {
            if (EveryoneAllIn()) DealRemainingCards();

            int newActivePlayer = _activePlayer >= players.Count ? 1 : _activePlayer + 1;
            Logger.LogInfo($"Moving from Player {_activePlayer} to Player {newActivePlayer}");
            _activePlayer = newActivePlayer;

            if (!players[_activePlayer - 1].isInHand)
                Logger.LogInfo($"Player {_activePlayer} is not in hand, next player!");

            else if (players[_activePlayer - 1].money <= 0)
                Logger.LogInfo($"Player {_activePlayer} is all-in, next player!");

            else
                foundNextPlayer = true;

            // Safety break
            if (_activePlayer == startPlayer) AdvancePhase();
        }

        OnNextPlayer?.Invoke(_activePlayer, (int)actionTaken);
        OnChangePlayerOptions?.Invoke((int)actionTaken, _activePlayer, pot);
    }
    /// <summary>
    /// Returns whether every player in the game is all-in or not.
    /// </summary>
    /// <returns></returns>
    bool EveryoneAllIn()
    {
        foreach(Player player in players)
        {
            if (player.isInHand && player.money > 0) return false; 
        }
        return true;
    }
    /// <summary>
    /// Deals cards to the board up until all cards are dealt. Then moves game to the showdown where a winner is determined.
    /// Is only called when every active player is all-in.
    /// </summary>
    void DealRemainingCards()
    {
        for (int i = cardsOnBoard.Length - 1; i >= 0; i--)
        {
            Card card = cardsOnBoard[i];
            if (card == null)
            {
                card = deckOfCards.DrawCard();
                cardsOnBoard[i] = card;
                Logger.LogInfo($"New cards: {cardsOnBoard[i].ToString()}");
            }
        }

        OnDealTableCards?.Invoke(cardsOnBoard);
        currentPhase = GamePhases.Showdown;
        OnNextPhase?.Invoke((int)currentPhase);
        DetermineWinner();
    }
    /// <summary>
    /// Returns true when all apply:
    /// 1) All players who can take an action took one.
    /// 2) Everyone who is not (yet) all-in, has bet the same amount of money.
    /// </summary>
    /// <returns></returns>
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
    /// <summary>
    /// Advances a round to the next phase, dealing cards for the flop, river and turn, while determining a winner in the showdown.
    /// </summary>
    void AdvancePhase()
    {
        // Reset Players
        foreach (Player player in players)
        {
            // Mark all-in players as already having taken an action
            player.tookAction = player.money == 0;
        }

        currentPhase++;

        // Deal board cards
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
    /// <summary>
    /// Finish up a round by sending information about who won the round.
    /// Additionally, also check if there is a winner for the entire game yet.
    /// </summary>
    /// <param name="winner"></param>
    void EndRound(int winner)
    {
        Logger.LogInfo($"round ended with winner: player {winner}");

        List<int> winnersIDList = new();

        // Multiple winners (tied), add all winners to a list, then give them money
        if (winner == -1)
        {
            HashSet<Player> winners = new();
            foreach (Player player in players)
            {
                if (player.isInHand) winners.Add(player);
            }

            for (int i = 0; i < players.Count; i++)
            {
                if (winners.Contains(players[i]))
                {
                    winnersIDList.Add(i + 1);
                    players[i].AddMoney(pot/winners.Count);
                    OnUpdatePlayerMoney(i + 1, players[i].money);
                }
            }
        }
        
        // only 1 winner, give them the money!
        else
        {
            winnersIDList.Add(winner);
            players[winner - 1].AddMoney(pot);
            OnUpdatePlayerMoney(winner, players[winner - 1].money);
        }
            
        // Reset Pot
        pot = 0;
        OnUpdatePot?.Invoke(pot);

        // Get a list of all players who have not yet gone bankrupt
        List<Player> playersInGame = new();
        foreach(Player player in players)
        {
            if (player.money > 0) playersInGame.Add(player);
        }

        // only 1 player with money remains, game ended with a winner!
        if (playersInGame.Count == 1)
        {
            roundRunning = false;
            gameRunning = false;
            OnGameEnd.Invoke(winner);
        }

        // Still multiple people with money in the game, send everyone winning player(s) information!
        else
        {
            roundRunning = false;

            OnRoundEnd.Invoke(winnersIDList);
        }
    }
    void DetermineWinner()
    {
        int winner = HandEvaluator.GetWinners(players, cardsOnBoard)[0];
        EndRound(winner);
    }
    /// <summary>
    /// Starts a new game. It creates a player class for each player and grants them the starting money amount.
    /// Should only be called from the host.
    /// </summary>
    /// <param name="playerAmount"></param>
    /// <param name="startingMoney"></param>
    public void StartGame(int playerAmount, int startingMoney)
    {
        if (gameRunning)
        {
            OnInvalidNewGame.Invoke($"Game is already running with {_playerAmount} players!");
            return;
        }

       _playerAmount = playerAmount;

        for (int i = 0; i < _playerAmount; i++)
        {
            players.Add(new Player(startingMoney, new Card[2]));
        }

        gameRunning = true;

        OnPlayerInformation.Invoke(playerAmount, startingMoney);

        StartRound();
    }
    /// <summary>
    /// Starts a new round, it starts by dealing each participating player a hand of 2 cards.
    /// Should only be called from the host.
    /// </summary>
    public void StartRound()
    {
        if (!gameRunning)
        {
            OnInvalidNewRound.Invoke("No game is currently running!");
            return;
        }

        if (roundRunning)
        {
            OnInvalidNewRound.Invoke($"round is already running, currently in phase: {currentPhase}");
            return;
        }

        roundRunning = true;
        pot = 0;
        currentPhase = GamePhases.PreFlop;

        foreach (Player player in players)
        {
            if (player.money > 0)
            {
                player.isInHand = true;
                player.tookAction = false;
            }
            else
                player.isInHand = false;
        }

        deckOfCards = new DeckOfCards(true);
        Card[] cards = new Card[2];
        cardsOnBoard = new Card[5];

        for (int i = 0; i <= players.Count - 1; i++)
        {
            if (!players[i].isInHand)
            {
                Logger.LogInfo($"Player {i + 1} has no money and can thus not participate anymore!");
                continue;
            }

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