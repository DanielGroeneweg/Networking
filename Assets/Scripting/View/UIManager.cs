using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
public class UIManager : MonoBehaviour
{
    [Header("Game")]
    [SerializeField]
    TMP_Text activePlayerText;
    [SerializeField]
    TMP_Text gameOverText;
    [SerializeField]
    GameObject restartScreen;
    [SerializeField]
    TMP_Text resultText;
    [SerializeField]
    List<TMP_Text> moneyDisplayers = new();
    [SerializeField]
    TMP_Text pot;

    [Header("Player")]
    [SerializeField]
    CardPresenter card1;
    [SerializeField]
    CardPresenter card2;

    [Header("Actions")]
    [SerializeField] Button checkButton;
    [SerializeField] Button betButton;
    [SerializeField] Button callButton;
    [SerializeField] Button raiseButton;
    [SerializeField] Button foldButton;

    TexasHoldemBoard board;
    void Start()
    {
        ModelOwner owner = FindFirstObjectByType<ModelOwner>();
        if (owner != null)
        {
            board = owner.board;
            board.OnActivePlayerChange += PlayerChange;
            board.OnGameOver += GameOver;
            board.OnStartRound += PresentCards;
            board.OnPlayerMoneyChange += PresentPlayerMoney;
            board.OnEndRound += EndRound;
        }
    }
    private void OnDestroy()
    {
        if (board != null)
        {
            board.OnActivePlayerChange -= PlayerChange;
            board.OnGameOver -= GameOver;
            board.OnStartRound -= PresentCards;
            board.OnPlayerMoneyChange -= PresentPlayerMoney;
            board.OnEndRound -= EndRound;
        }
    }
    void GameOver(int winner)
    {
        switch (winner)
        {
            case 1: gameOverText.text = "player wins!"; break;
            case 2: gameOverText.text = "CPU wins!"; break;
            default: gameOverText.text = "it's a draw"; break;
        }
    }
    void PlayerChange(int player, BettingActions chosenAction, int pot)
    {
        Debug.Log("Active player: " + player);
        switch (player)
        {
            case 1: activePlayerText.text = "active player: Player"; break;
            case 2: activePlayerText.text = "active player: CPU"; break;
            default: activePlayerText.text = ""; break;
        }

        // Update Pot
        this.pot.text = $"Pot: {pot}";

        // Set actions allowed to be taken
        if (player == 1)
        {
            List<Button> all = new List<Button>();
            all.Add(checkButton);
            all.Add(betButton);
            all.Add(raiseButton);
            all.Add(foldButton);
            all.Add(callButton);
            HashSet<Button> allowed = new();

            switch(chosenAction)
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

                // None will only be sent at the start of a round/phase
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
    }
    void PresentPlayerMoney(int player, int money)
    {
        moneyDisplayers[player - 1].text = $"Player {player}: {money}";
    }
    void PresentCards(Player playerData)
    {
        card1.PresentCard(playerData.cards[0]);
        card2.PresentCard(playerData.cards[1]);
    }
    void EndRound(int winner)
    {
        restartScreen.SetActive(true);
        resultText.text = $"Player {winner} wins!";
    }
}
