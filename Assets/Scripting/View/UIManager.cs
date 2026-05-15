using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.Rendering.Universal;
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
    PlayerMoneyAction playerMoneyDisplayerPrefab;
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
    [SerializeField]
    TMP_Text phaseText;

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
    [SerializeField] UnityEvent onBankrupt;

    List<PlayerCardInfoShower> playerCardPresenters = new();
    List<BoardCardShower> boardPresenters = new();
    Dictionary<int, PlayerMoneyAction> moneyDisplayers = new();
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
            client.OnNextPhase += NextPhase;
            client.OnValidPlayerAction += ValidPlayerAction;
            client.OnPlayerDC += DCPlayer;
            client.OnBankrupt += Bankrupt;
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
            client.OnNextPhase -= NextPhase;
            client.OnValidPlayerAction -= ValidPlayerAction;
            client.OnPlayerDC -= DCPlayer;
            client.OnBankrupt -= Bankrupt;
        }
    }
    void NewRoundStart()
    {
        onRoundStart?.Invoke();

        foreach (int id in moneyDisplayers.Keys)
        {
            PlayerMoneyAction presenter = moneyDisplayers[id];

            string txt = moneyDisplayers[id].moneyText.text.ToString();
            int dollarIndex = txt.IndexOf('$');
            if (dollarIndex != -1)
            {
                string amountText = txt.Substring(dollarIndex + 1);

                Debug.Log(id);
                if (float.TryParse(amountText, out float amount))
                {
                    if (amount > 0)
                    {
                        presenter.cards.gameObject.SetActive(true);
                        Debug.Log("succeeded parse, enabling");
                    }
                    else
                    {
                        Debug.Log("succeeded parse, disabling");
                        presenter.cards.gameObject.SetActive(false);
                    }
                }
                else
                {
                    Debug.Log("failed parse");
                    presenter.cards.gameObject.SetActive(true);
                }
            }
        }
    }
    void EnableLobbyPanel(int id) { hostPanel.SetActive(true); }
    /// <summary>
    /// Fires an event that enables all buttons for the host to start games and rounds.
    /// </summary>
    void EnableHostButtons() { enableHostButtons?.Invoke(); }
    void DisableLobbyPanel(List<int> ids, int money = 0) { hostPanel.SetActive(false); }
    void GameOver(int winner)
    {
        gameOverText.text = $"player {winner} wins!";
        gameOverPanel.gameObject.SetActive(true);
        
        foreach (int id in moneyDisplayers.Keys)
        {
            if (moneyDisplayers[id].gameObject != null) Destroy(moneyDisplayers[id].gameObject);
        }
        moneyDisplayers.Clear();
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
        moneyDisplayers[player].moneyText.text = $"Player {player}: ${money}";
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
    void SetUpMoneyUI(List<int> ids, int startingMoney)
    {
        for (int i = moneyDisplayers.Count - 1; i >= 0; i--)
        {
            PlayerMoneyAction moneyDisplayer = moneyDisplayers[i];
            Destroy(moneyDisplayer.gameObject);
        }
        moneyDisplayers.Clear();

        foreach (int id in ids)
        {
            PlayerMoneyAction playerText = Instantiate(playerMoneyDisplayerPrefab, Vector3.zero, Quaternion.identity, moneyParent.transform);
            playerText.moneyText.text = $"Player {id}: ${startingMoney}";
            moneyDisplayers.Add(id, playerText);
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
    void NextPhase(int phase)
    {
        foreach (int id in moneyDisplayers.Keys)
        {
            PlayerMoneyAction playerMoneyDisplayer = moneyDisplayers[id];
            playerMoneyDisplayer.actionText.text = "";
        }

        GamePhases currentPhase = (GamePhases)phase;
        phaseText.text = $"Current Phase: {currentPhase}";
    }
    void ValidPlayerAction(int player, int action)
    {
        moneyDisplayers[player].actionText.text = $"{(BettingActions)action}";
        if ((BettingActions)action == BettingActions.Fold) moneyDisplayers[player].cards.gameObject.SetActive(false);
    }
    void DCPlayer(int player)
    {
        moneyDisplayers[player].actionText.text = "Disconnected";
        moneyDisplayers[player].actionText.color = Color.red;
        moneyDisplayers[player].moneyText.color = Color.red;
        StartCoroutine(LateDestroyDisconnect(player));
    }
    IEnumerator LateDestroyDisconnect(int player)
    {
        yield return new WaitForSeconds(5);
        Destroy(moneyDisplayers[player].gameObject);
        moneyDisplayers.Remove(player);
    }
    void Bankrupt()
    {
        onBankrupt?.Invoke();
    }
}
