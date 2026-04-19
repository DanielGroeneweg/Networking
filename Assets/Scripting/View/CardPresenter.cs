using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class CardPresenter : MonoBehaviour
{
    [SerializeField] private Image cardImage;
    public void PresentCard(Card card)
    {
        cardImage.sprite = CardDataBase.instance.GetCardImage(card.suit, card.rank);
    }
}