using UnityEngine;
using System.Collections.Generic;
public class BoardManager : MonoBehaviour
{
    [SerializeField] CardPresenter cardPresenterPrefab;

    List<CardPresenter> cards = new();
    Client client;
    void Start()
    {
        client = FindFirstObjectByType<Client>();
        if (client != null)
        {
            client.OnDealTableCards += DisplayBoard;
            client.OnNewRound += ClearBoard;
        }
    }
    void OnDestroy()
    {
        if (client != null)
        {
            client.OnDealTableCards -= DisplayBoard;
            client.OnNewRound -= ClearBoard;
        }
    }
    public void DisplayBoard(Card[] cardsOnBoard)
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
    public void ClearBoard()
    {
        foreach (CardPresenter card in cards) if (card != null) Destroy(card.gameObject);
        cards.Clear();
    }
}