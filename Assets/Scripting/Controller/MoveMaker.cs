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
        board.Check(1);
    }
    public void Bet() {
        board.Bet(1, betValue);
    }
    public void Call() {
        board.Call(1);
    }
    public void Raise()
    {
        board.Raise(1, betValue);
    }
    public void Fold() {
        board.Fold(1);
    }
}