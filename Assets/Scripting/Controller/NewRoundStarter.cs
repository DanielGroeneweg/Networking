using UnityEngine;

public class NewRoundStarter : MonoBehaviour
{
    Client client;
    private void Start()
    {
        client = FindFirstObjectByType<Client>();
    }
    public void StartRound()
    {
        if (client == null)
        {
            Debug.Log("V: client is null");
            return;
        }
        client.NewRoundRequest();
    }
}
