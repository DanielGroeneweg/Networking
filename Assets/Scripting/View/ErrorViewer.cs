using UnityEngine;
public class ErrorViewer : MonoBehaviour
{
    [SerializeField] ErrorPresenter errorPrefab;
    [SerializeField] Canvas parent;
    Client client;
    void CreateErrorMessage(string error)
    {
        ErrorPresenter errorPresenter = Instantiate(errorPrefab, Vector3.zero, Quaternion.identity, parent.transform);
        errorPresenter.transform.localPosition = Vector3.zero;   
        errorPresenter.SetText(error);
    }
    private void Start()
    {
        client = FindFirstObjectByType<Client>();
        if (client != null)
        {
            client.OnInvalidAction += CreateErrorMessage;
            client.OnInvalidNewGame += CreateErrorMessage;
            client.OnInvalidNewRound += CreateErrorMessage;
        }
    }
}