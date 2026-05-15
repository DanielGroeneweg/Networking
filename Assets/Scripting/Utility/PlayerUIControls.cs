using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerUIControls : MonoBehaviour
{
    [SerializeField]
    GameObject menu;
    public void OnPause(InputValue inputValue)
    {
        menu.SetActive(!menu.activeSelf);
    }
}