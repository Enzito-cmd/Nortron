using TMPro;
using UnityEngine;


public class MenuUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField text;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private TMP_Text errorText;

    private void Awake()
    {
        if (nameInput != null)
        {
            nameInput.characterLimit = NetworkManager.MaxNameLength;
        }

        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }
    }

    private void Start()
    {
        string message = NetworkManager.ConsumeDisconnectMessage();

        if (message.Length == 0)
        {
            return;
        }

        if (errorText != null)
        {
            errorText.text = message;
        }

        if (errorPanel != null)
        {
            errorPanel.SetActive(true);
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

    public void CloseError()
    {
        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }
    }

    private void ApplyPlayerName()
    {
        NetworkManager.Instance.PlayerName = nameInput != null ? nameInput.text : string.Empty;
    }
}
