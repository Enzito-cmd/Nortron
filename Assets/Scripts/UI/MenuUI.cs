using TMPro;
using UnityEngine;


public class MenuUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField text;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private TMP_Text errorText;

    private bool nameFilled;

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
        GameConfig.RefreshIfStale();

        string message = NetworkManager.ConsumeDisconnectMessage();

        if (message.Length > 0)
        {
            ShowError(message);
        }
    }

    private void Update()
    {
        if (nameFilled || PlayerStats.Loaded == false)
        {
            return;
        }

        nameFilled = true;

        if (nameInput != null && nameInput.text.Length == 0 && PlayerStats.SavedName.Length > 0)
        {
            nameInput.text = PlayerStats.SavedName;
        }
    }

    public void CreateGame()
    {
        if (CanPlay() == false)
        {
            return;
        }

        ApplyPlayerName();
        NetworkManager.Instance.StartGameHost(text.text);
    }

    public void JoinGame()
    {
        if (CanPlay() == false)
        {
            return;
        }

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

    private bool CanPlay()
    {
        if (GameConfig.Maintenance)
        {
            ShowError(GameConfig.MaintenanceMessage);
            return false;
        }

        if (GameConfig.IsVersionAllowed == false)
        {
            ShowError("There is a new version (" + GameConfig.MinVersion + "). Update the game to play.");
            return false;
        }

        return true;
    }

    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
        }

        if (errorPanel != null)
        {
            errorPanel.SetActive(true);
        }
    }

    private void ApplyPlayerName()
    {
        string playerName = nameInput != null ? nameInput.text : string.Empty;

        NetworkManager.Instance.PlayerName = playerName;
        PlayerStats.SaveName(playerName);
    }
}
