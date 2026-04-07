using UnityEngine;
using UnityEngine.Rendering.LookDev;
public class Player
{
    public int money {  get; private set; }
    public Card[] cards { get; private set; }
    public int betMoney { get; private set; }

    public bool isInHand = true;
    public bool tookAction = false;
    public Player(int startingMoney, Card[] cards)
    {
        if (cards.Length != 2)
        {
            Logger.LogInfo($"Not two cards have been dealed. dealed cards: {cards.Length}");
            return;
        }

        if (startingMoney <= 0)
        {
            Logger.LogInfo($"Too little starting money: {startingMoney}");
            return;
        }

        money = startingMoney;
        this.cards = new Card[2];
        this.cards[0] = cards[0];
        this.cards[1] = cards[1];
    }
    public void DealNewCards(Card[] cards)
    {
        if (cards.Length != 2)
        {
            Logger.LogInfo($"Not two cards have been dealed. dealed cards: {cards.Length}");
            return;
        }
    }
    public void AddMoney(int value) { money += Mathf.Abs(value); }
    public void Bet(int value)
    {
        money -= Mathf.Abs(value);
        betMoney += Mathf.Abs(value);
    }
}