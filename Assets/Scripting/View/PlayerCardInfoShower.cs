using TMPro;
using UnityEngine;
public class PlayerCardInfoShower : MonoBehaviour
{
    [SerializeField] CardPresenter card1;
    [SerializeField] CardPresenter card2;
    [SerializeField] TMP_Text playeridText;
    public void Display(PlayerCardCombo combo)
    {
        playeridText.text = $"Player {combo.player}";
        card1.PresentCard(combo.cards[0]);
        card2.PresentCard(combo.cards[1]);
    }
}