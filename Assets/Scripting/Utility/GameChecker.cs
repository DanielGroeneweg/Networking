using UnityEngine;
using System;
using System.Collections.Generic;
[Serializable]
public class CheatCard
{
    public Ranks rank;
    public Suits suit;
    public Card ToCard()
    {
        return new Card(suit, rank);
    }
}
[Serializable]
public class CheatPlayerHand
{
    public CheatCard card1;
    public CheatCard card2;
}
public class GameChecker : MonoBehaviour
{
    [SerializeField]
    List<CheatPlayerHand> players = new();

    [SerializeField]
    List<CheatCard> boardCards = new();

    public void DoCheck()
    {
        List<Player> playerList = new List<Player>();
        foreach (CheatPlayerHand player in players)
        {
            Card[] cards = new Card[2];
            cards[0] = player.card1.ToCard();
            cards[1] = player.card2.ToCard();
            playerList.Add(new Player(100, cards));
        }

        Card[] board = new Card[5];
        board[0] = boardCards[0].ToCard();
        board[1] = boardCards[1].ToCard();
        board[2] = boardCards[2].ToCard();
        board[3] = boardCards[3].ToCard();
        board[4] = boardCards[4].ToCard();

        List<int> winners = HandEvaluator.GetWinners(playerList, board);
        foreach (int winner in winners) Debug.Log(winner);
        Debug.Log("---------------");
    }
}