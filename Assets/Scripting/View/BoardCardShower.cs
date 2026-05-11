using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
public class BoardCardShower : MonoBehaviour
{
    [SerializeField] List<CardPresenter> presenters = new();
    public void Display(List<Card> cards)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            presenters[i].PresentCard(cards[i]);
        }
    }
}
