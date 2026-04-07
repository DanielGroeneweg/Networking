using System.Collections.Generic;
using UnityEngine;
public class BotController : MonoBehaviour
{
    TexasHoldemBoard board;
    void Start()
    {
        ModelOwner owner = FindFirstObjectByType<ModelOwner>();
        if (owner != null)
        {
            board = owner.board;
            board.OnBotMove += ChangePlayer;
        }
    }
    private void OnDestroy()
    {
        board.OnBotMove -= ChangePlayer;
    }
    void ChangePlayer(BettingActions chosenAction, int botMoney, int pot)
    {
        Debug.Log("random cpu move");
        List<BettingActions> allowedActions = new();
        switch (chosenAction)
        {
            case BettingActions.None:
                allowedActions.Add(BettingActions.Bet);
                if (pot > 0) allowedActions.Add(BettingActions.Check);
                break;

            case BettingActions.Bet:
                allowedActions.Add(BettingActions.Raise);
                allowedActions.Add(BettingActions.Call);
                break;

            case BettingActions.Raise:
                allowedActions.Add(BettingActions.Raise);
                allowedActions.Add(BettingActions.Call);
                break;

            case BettingActions.Call:
                allowedActions.Add(BettingActions.Raise);
                allowedActions.Add(BettingActions.Call);
                break;

            case BettingActions.Check:
                allowedActions.Add(BettingActions.Bet);
                allowedActions.Add(BettingActions.Check);
                break;
        }

        MakeMove(botMoney, allowedActions);
    }
    public void MakeMove(int moneyLeft, List<BettingActions> allowedActions)
    {
        BettingActions action = allowedActions[Random.Range(0, allowedActions.Count)];
        int money = 0;
        if (action == BettingActions.Bet || action == BettingActions.Raise)
        {
            money = Random.Range(1, moneyLeft);
        }

        MoveData moveData = new MoveData(action, money, 2);
        board.MakeMove(moveData);
    }
}