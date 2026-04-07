using UnityEngine;
using System.Collections.Generic;
public class BoardManager : MonoBehaviour
{
    [SerializeField] CardPresenter cardPresenterPrefab;

    List<CardPresenter> cards = new();
    TexasHoldemBoard board;
    void Start()
    {
        ModelOwner owner = FindFirstObjectByType<ModelOwner>();
        if (owner != null)
        {
            board = owner.board;
            board.OnPhaseChange += DisplayBoard;
            board.OnStartRound += ClearBoard;
        }
    }
    void OnDestroy()
    {
        if (board != null)
        {
            board.OnPhaseChange -= DisplayBoard;
            board.OnStartRound -= ClearBoard;
        }
    }
    public void DisplayBoard(int pot, int gamePhase, Card[] cardsOnBoard)
    {
        ClearBoard();

        foreach(Card card in cardsOnBoard)
        {
            if (card == null) continue;
            CardPresenter obj = Instantiate(cardPresenterPrefab, Vector3.zero, Quaternion.identity, transform);
            obj.PresentCard(card);
            cards.Add(obj);
        }
    }
    public void ClearBoard(int card1Rank = 0, int card1Suit = 0, int card2Rank = 0, int card2Suit = 0)
    {
        foreach (CardPresenter card in cards) if (card != null) Destroy(card.gameObject);
        cards.Clear();
    }
}