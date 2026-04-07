using TMPro;
using UnityEngine;
public class CardPresenter : MonoBehaviour
{
    [SerializeField] private TMP_Text cardLabel;
    public void PresentCard(Card card)
    {
        cardLabel.text = $"{card.rank}\nof\n{card.suit}";
    }
}