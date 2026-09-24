using TMPro;
using UnityEngine;


public class MenuUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField text;

    public void CreateGame()
    {
        NetworkManager.Instance.StartGameHost(text.text);
    }

    public void JoinGame()
    {
        NetworkManager.Instance.StartGameClient(text.text);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
