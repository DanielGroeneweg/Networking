using UnityEngine;

public class NewRoundStarter : MonoBehaviour
{
    TexasHoldemBoard board;
    private void Start()
    {
        ModelOwner owner = FindFirstObjectByType<ModelOwner>();
        if (owner != null)
        {
            board = owner.board;
        }
    }
    public void StartRound()
    {
        board.StartRound();
    }
}
