using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    public static bool IsOpen { get; private set; }

    private void Awake()
    {
        SetOpen(false);
    }

    private void OnDestroy()
    {
        IsOpen = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetOpen(IsOpen == false);
        }
    }

    public void Resume()
    {
        SetOpen(false);
    }

    public void ExitToMenu()
    {
        SetOpen(false);
        NetworkManager.Instance.LeaveSession();
    }

    private void SetOpen(bool open)
    {
        IsOpen = open;

        if (panel != null)
        {
            panel.SetActive(open);
        }
    }
}
