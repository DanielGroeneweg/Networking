using UnityEngine;
using TMPro;
public class ErrorPresenter : MonoBehaviour
{
    [SerializeField] int duration;
    [SerializeField] TMP_Text tmpText;
    public void SetText(string text)
    {
        tmpText.text = text;
    }
    private void Start()
    {
        Invoke(nameof(LateDestroy), duration);
    }
    void LateDestroy()
    {
        Destroy(gameObject);
    }
}