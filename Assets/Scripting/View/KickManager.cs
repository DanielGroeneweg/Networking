using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class KickManager : MonoBehaviour
{
    [SerializeField]
    TMP_Text kickCountDownText;
    [SerializeField]
    int secondsDelayBeforeKick;
    [SerializeField]
    UnityEvent OnStartKick;
    [SerializeField]
    UnityEvent OnKick;

    Client client;
    void Start()
    {
        client = FindFirstObjectByType<Client>();
        if (client != null)
        {
            client.onKickPlayer += KickPlayer;
        }
    }
    private void OnDestroy()
    {
        client.onKickPlayer -= KickPlayer;
    }
    void KickPlayer()
    {
        OnStartKick?.Invoke();
        StartCoroutine(KickPlayerAfterDelay());
    }
    IEnumerator KickPlayerAfterDelay()
    {
        int i = secondsDelayBeforeKick;

        while (i > 0)
        {
            kickCountDownText.text = $"No players left! kicked in: {i}";
            yield return new WaitForSeconds(1);
            i--;
        }

        OnKick?.Invoke();
    }
}