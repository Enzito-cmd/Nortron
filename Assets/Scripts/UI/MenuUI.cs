using TMPro;
using UnityEngine;


public class MenuUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField text;
    [SerializeField] private TMP_InputField nameInput;

    private void Awake()
    {
        if (nameInput != null)
        {
            nameInput.characterLimit = NetworkManager.MaxNameLength;
        }
    }

    public void CreateGame()
    {
        ApplyPlayerName();
        NetworkManager.Instance.StartGameHost(text.text);
    }

    public void JoinGame()
    {
        ApplyPlayerName();
        NetworkManager.Instance.StartGameClient(text.text);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    private void ApplyPlayerName()
    {
        NetworkManager.Instance.PlayerName = nameInput != null ? nameInput.text : string.Empty;
    }
}
