using Unity.VisualScripting;
using UnityEngine;

public class MoveMaker : MonoBehaviour
{
    TexasHoldemBoard board;
    int betValue;
    void Start()
    {
        ModelOwner owner = FindFirstObjectByType<ModelOwner>();
        if (owner != null)
        {
            board = owner.board;
        }
    }
    public void SetBetValue(string value)
    {
        try { betValue = int.Parse(value); }
        catch { Debug.Log($"Something Went wrong when trying to parse '{value}'"); }
    }
    public void Check()
    {
        MoveData moveData = new MoveData(BettingActions.Check, 0, 1);
        board.MakeMove(moveData);
    }
    public void Bet() {
        MoveData moveData = new MoveData(BettingActions.Bet, betValue, 1);
        board.MakeMove(moveData);
    }
    public void Call() {
        MoveData moveData = new MoveData(BettingActions.Call, 0, 1);
        board.MakeMove(moveData);
    }
    public void Raise()
    {
        MoveData moveData = new MoveData(BettingActions.Raise, betValue, 1);
        board.MakeMove(moveData);
    }
    public void Fold() {
        MoveData moveData = new MoveData(BettingActions.Fold, 0, 1);
        board.MakeMove(moveData);
    }
}