using UnityEngine;
using System.Collections;
public class ModelOwner : MonoBehaviour
{
    [SerializeField] int playerStartingMoney;
    public TexasHoldemBoard board
    {
        get
        {
            return _board;
        }
    }
    TexasHoldemBoard _board;

    void Awake()
    {
        _board = new TexasHoldemBoard(playerStartingMoney);
        StartCoroutine(DelayedStart());
    }
    private IEnumerator DelayedStart()
    {
        yield return new WaitForEndOfFrame();
        _board.StartRound();
    }
}