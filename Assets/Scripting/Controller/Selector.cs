using System;
using UnityEngine;

public class Selector : MonoBehaviour
{
    [SerializeField] private BettingActions action;
    MoveMaker moveMaker;
    private void Start()
    {
        MoveMaker moveMaker = FindFirstObjectByType<MoveMaker>();
        if (moveMaker != null)
        {
            this.moveMaker = moveMaker;
        }
    }
    public void DoAction()
    {
        switch(action)
        {
            case BettingActions.Check:
                moveMaker.Check();
                break;

            case BettingActions.Bet:
                moveMaker.Bet();
                break;

            case BettingActions.Call:
                moveMaker.Call();
                break;

            case BettingActions.Raise:
                moveMaker.Raise();
                break;

            case BettingActions.Fold:
                moveMaker.Fold();
                break;
        }
    }
}