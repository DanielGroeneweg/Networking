using UnityEngine;

public class NewRoundStarter : MonoBehaviour
{
    Client client;
    private void Start()
    {
        Client client = FindFirstObjectByType<Client>();
    }
    public void StartRound()
    {
        if (client == null) return;
        client.NewRoundRequest();
    }
}
