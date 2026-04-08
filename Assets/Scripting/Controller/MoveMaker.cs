using Unity.VisualScripting;
using UnityEngine;

public class MoveMaker : MonoBehaviour
{
    Client client;
    int betValue;
    void Start()
    {
        client = FindFirstObjectByType<Client>();
    }
    public void SetBetValue(string value)
    {
        try { betValue = int.Parse(value); }
        catch { Debug.Log($"Something Went wrong when trying to parse '{value}'"); }
    }
    public void Check()
    {
        client.CheckRequest();
    }
    public void Bet() {
        client.BetRequest(betValue);
    }
    public void Call() {
        client.CallRequest();
    }
    public void Raise()
    {
        client.RaiseRequest(betValue);
    }
    public void Fold() {
        client.FoldRequest();
    }
}