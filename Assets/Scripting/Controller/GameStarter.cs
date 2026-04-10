using UnityEngine;
using TMPro;
public class GameStarter : MonoBehaviour
{
    [SerializeField] TMP_InputField startMoneyInput;

    int startMoney = 500;

    Client client;
    private void OnEnable()
    {
        client = FindFirstObjectByType<Client>();
    }
    public void TrySetStartingMoney(string input)
    {
        int money = int.Parse(input);
        
        if (money <= 0)
            startMoneyInput.text = startMoney.ToString();

        else
            startMoney = money;
    }
    public void StartGame() { client.NewGameRequest(startMoney); }
}