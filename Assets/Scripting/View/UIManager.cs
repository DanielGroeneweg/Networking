using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Events;
public class UIManager : MonoBehaviour
{
    [Header("Game")]
    [SerializeField]
    TMP_Text activePlayerText;
    [SerializeField]
    TMP_Text gameOverText;
    [SerializeField]
    Image endRoundPanel;
    [SerializeField]
    Image gameOverPanel;
    [SerializeField]
    GameObject restartScreen;
    [SerializeField]
    TMP_Text resultText;
    [SerializeField]
    List<TMP_Text> moneyDisplayers = new();
    [SerializeField]
    TMP_Text displayPrefab;
    [SerializeField]
    GameObject moneyParent;
    [SerializeField]
    TMP_Text pot;
    [SerializeField]
    PlayerCardInfoShower playerCardInfoPrefab;
    [SerializeField]
    GameObject playerCardInfoParent;
    [SerializeField]
    BoardCardShower boardShowerPrefab;
    [SerializeField]
    GameObject boardCardParent;

    [Header("Player")]
    [SerializeField]
    CardPresenter card1;
    [SerializeField]
    CardPresenter card2;
    [SerializeField]
    TMP_Text playerIDLabel;

    [Header("Actions")]
    [SerializeField] Button checkButton;
    [SerializeField] Button betButton;
    [SerializeField] Button callButton;
    [SerializeField] Button raiseButton;
    [SerializeField] Button foldButton;

    [Header("host")]
    [SerializeField] GameObject hostPanel;
    [SerializeField] UnityEvent enableHostButtons;

    [Header("Other")]
    [SerializeField] UnityEvent onRoundStart;
    Client client;
    [SerializeField]
    TMP_Text panelPlayerIDLabel;

    [Header("Events")]
    [SerializeField] UnityEvent onSpectator;

    List<PlayerCardInfoShower> playerCardPresenters = new();
    List<BoardCardShower> boardPresenters = new();
    int myID;
    void Start()
    {
        client = FindFirstObjectByType<Client>();
        if (client != null)
        {
            client.OnNextPlayer += PlayerChange;
            client.OnChangePlayerOptions += MyTurn;
            client.OnUpdatePot += PresentPotMoney;
            client.OnDealCards += PresentCards;
            client.OnUpdatePlayerMoney += PresentPlayerMoney;
            client.OnPlayerID += EnableLobbyPanel;
            client.OnSendHostInformation += EnableHostButtons;
            client.OnPlayerInformation += DisableLobbyPanel;
            client.OnPlayerInformation += SetUpMoneyUI;
            client.OnGameEnd += GameOver;
            client.OnRoundEnd += EndRound;
            client.OnNewRound += NewRoundStart;
            client.OnNewRound += ClearCards;
            client.OnJoinedAsSpectator += onSpectator.Invoke;
            client.OnPlayerCardInformation += ShowPlayerCards;
            client.OnPlayerID += ShowPlayerID;
        }
    }
    private void OnDestroy()
    {
        if (client != null)
        {
            client.OnNextPlayer -= PlayerChange;
            client.OnChangePlayerOptions -= MyTurn;
            client.OnUpdatePot -= PresentPotMoney;
            client.OnDealCards -= PresentCards;
            client.OnUpdatePlayerMoney -= PresentPlayerMoney;
            client.OnPlayerID -= EnableLobbyPanel;
            client.OnSendHostInformation -= EnableHostButtons;
            client.OnPlayerInformation -= DisableLobbyPanel;
            client.OnPlayerInformation -= SetUpMoneyUI;
            client.OnGameEnd -= GameOver;
            client.OnRoundEnd -= EndRound;
            client.OnNewRound -= NewRoundStart;
            client.OnNewRound += ClearCards;
            client.OnJoinedAsSpectator -= onSpectator.Invoke;
            client.OnPlayerCardInformation -= ShowPlayerCards;
            client.OnPlayerID -= ShowPlayerID;
        }
    }
    void NewRoundStart()
    {
        onRoundStart?.Invoke();
    }
    void EnableLobbyPanel(int id) { hostPanel.SetActive(true); }
    /// <summary>
    /// Fires an event that enables all buttons for the host to start games and rounds.
    /// </summary>
    void EnableHostButtons() { enableHostButtons?.Invoke(); }
    void DisableLobbyPanel(int players = 0, int money = 0) { hostPanel.SetActive(false); }
    void GameOver(int winner)
    {
        gameOverText.text = $"player {winner} wins!";
        gameOverPanel.gameObject.SetActive(true);
    }
    void PresentPotMoney(int pot)
    {
        this.pot.text = $"Pot: ${pot}";
    }
    void PlayerChange(int player, int chosenAction)
    {
        Debug.Log("Active player: " + player);
        activePlayerText.text = $"active player: Player {player}";
        activePlayerText.color = player == myID ? Color.white : Color.red;
    }
    /// <summary>
    /// Enables and disables action buttons that allow players to take actions.
    /// </summary>
    /// <param name="chosenAction"></param>
    /// <param name="pot"></param>
    void MyTurn(int chosenAction, int pot)
    {
        List<Button> all = new List<Button>();
        all.Add(checkButton);
        all.Add(betButton);
        all.Add(raiseButton);
        all.Add(foldButton);
        all.Add(callButton);
        HashSet<Button> allowed = new();

        switch ((BettingActions)chosenAction)
        {
            case BettingActions.Check:
                allowed.Add(checkButton);
                allowed.Add(betButton);
                allowed.Add(foldButton);
                break;

            case BettingActions.Bet:
                allowed.Add(callButton);
                allowed.Add(raiseButton);
                allowed.Add(foldButton);
                break;

            case BettingActions.Call:
                allowed.Add(callButton);
                allowed.Add(raiseButton);
                allowed.Add(foldButton);
                break;

            case BettingActions.Raise:
                allowed.Add(callButton);
                allowed.Add(raiseButton);
                allowed.Add(foldButton);
                break;

            case BettingActions.Fold:
                Debug.LogError("Action is set to fold, should not be possible though!");
                break;

            // None will only be sent at the start of a new round/phase when no betting action has been taken yet.
            case BettingActions.None:
                allowed.Add(betButton);
                allowed.Add(foldButton);
                if (pot > 0) allowed.Add(checkButton);
                break;
        }

        foreach (Button button in all)
        {
            bool b = allowed.Contains(button) ? true : false;
            button.gameObject.SetActive(b);
        }
    }
    void PresentPlayerMoney(int player, int money)
    {
        moneyDisplayers[player - 1].text = $"Player {player}: ${money}";
    }
    void PresentCards(int card1Rank, int card1Suit, int card2Rank, int card2Suit)
    {
        Card firstCard = new Card((Suits)card1Suit, (Ranks)card1Rank);
        Card secondCard = new Card((Suits)card2Suit, (Ranks)card2Rank);

        card1.PresentCard(firstCard);
        card2.PresentCard(secondCard);
    }
    /// <summary>
    /// Enables the post-round screen and displays all players that won.
    /// </summary>
    /// <param name="winners"></param>
    void EndRound(bool[] winners)
    {
        restartScreen.SetActive(true);
        string text = "Winning Player(s): ";

        for (int i = 0; i < winners.Length; i++)
        {
            if (winners[i]) text += $"|{i + 1}| ";
        }
        resultText.text = text;
    }
    /// <summary>
    /// shows the cards on the board and each player that didn't fold in the post-round screen.
    /// </summary>
    /// <param name="info"></param>
    void ShowPlayerCards(PlayerCardInfo info)
    {
        foreach (PlayerCardCombo combo in info.players)
        {
            // Board is registered on -1
            if (combo.player == -1)
            {
                BoardCardShower boardshower = Instantiate(boardShowerPrefab, boardCardParent.transform);
                boardshower.transform.localPosition = Vector3.zero;
                boardshower.Display(combo.cards);
                boardPresenters.Add(boardshower);
                continue;
            }

            PlayerCardInfoShower shower = Instantiate(playerCardInfoPrefab, playerCardInfoParent.transform);
            shower.transform.localPosition = Vector3.zero;
            shower.Display(combo);
            playerCardPresenters.Add(shower);
        }
    }
    /// <summary>
    /// Create a tab for each player to display their money.
    /// </summary>
    /// <param name="playerAmount"></param>
    /// <param name="startingMoney"></param>
    void SetUpMoneyUI(int playerAmount, int startingMoney)
    {
        for (int i = 1; i <= playerAmount; i++)
        {
            TMP_Text playerText = Instantiate(displayPrefab, Vector3.zero, Quaternion.identity, moneyParent.transform);
            playerText.text = $"Player {i}: ${startingMoney}";
            moneyDisplayers.Add(playerText);
        }

        gameOverPanel.gameObject.SetActive(false);
        endRoundPanel.gameObject.SetActive(false);
    }
    /// <summary>
    /// Clears the cards in the post-round screen that displays winners and their cards
    /// </summary>
    void ClearCards()
    {
        for(int i = playerCardPresenters.Count - 1; i >= 0; i--)
        {
            PlayerCardInfoShower shower = playerCardPresenters[i];
            Destroy(shower.gameObject);
        }

        for (int i = boardPresenters.Count - 1; i >= 0; i--)
        {
            BoardCardShower shower = boardPresenters[i];
            Destroy(shower.gameObject);
        }

        playerCardPresenters.Clear();
        boardPresenters.Clear();
    }
    void ShowPlayerID(int id)
    {
        myID = id;
        panelPlayerIDLabel.text = $"You are Player {id}";
        playerIDLabel.text = $"You are Player {id}";
    }
}
