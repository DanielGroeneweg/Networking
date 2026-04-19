using UnityEngine;
using System.Collections.Generic;
public class CardDataBase : MonoBehaviour
{
    /// <summary>
    /// index 0-12: Spades starting at Ace
    /// index 13-25: Hearts starting at Ace
    /// index 26-38: Clubs starting at Ace
    /// index 39-51: Diamonds starting at Ace
    /// </summary>
    /// <param name="suit"></param>
    /// <param name="rank"></param>
    /// <returns></returns>
    [SerializeField] List<Sprite> cardImages = new();
    
    public static CardDataBase instance;
    private void Start()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    public Sprite GetCardImage(Suits suit, Ranks rank)
    {
        int index = 0;
        switch(suit)
        {
            case Suits.Spades:
                index = 0;
                break;
            case Suits.Hearts:
                index = 13;
                break;
            case Suits.Clubs:
                index = 26;
                break;
            case Suits.Diamonds:
                index = 39;
                break;
        }

        index += (int)rank - 1;

        return cardImages[index];
    }
}