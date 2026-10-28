using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChatBox : MonoBehaviour
{
    private const int MaxLength = 64;
    private const int MaxStoredMessages = 50;
    private const int VisibleLines = 8;
    private const float MessageSeconds = 8f;
    private const float FadeSeconds = 1f;
    private const float LineHeight = 26f;
    private const float Margin = 16f;
    private const string InputControlName = "ChatInput";

    private struct Message
    {
        public string Text;
        public float Timestamp;
    }

    private static readonly List<Message> messages = new List<Message>();

    private string draft = string.Empty;
    private bool focusPending;
    private GUIStyle lineStyle;
    private GUIStyle inputStyle;

    public static bool IsTyping { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindFirstObjectByType<ChatBox>() == null)
        {
            DontDestroyOnLoad(new GameObject("ChatBox", typeof(ChatBox)));
        }
    }

    public static void AddMessage(string sender, string text)
    {
        messages.Add(new Message { Text = sender + ": " + text, Timestamp = Time.time });

        if (messages.Count > MaxStoredMessages)
        {
            messages.RemoveAt(0);
        }
    }

    private static bool IsInLobby()
    {
        return SceneManager.GetActiveScene().buildIndex == NetworkManager.LobbySceneIndex && PlayerIdentity.Local != null;
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == NetworkManager.MenuSceneIndex)
        {
            messages.Clear();
        }

        CloseChat();
    }

    private void Update()
    {
        if (IsInLobby() == false)
        {
            CloseChat();
            return;
        }

        if (IsTyping && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChat();
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (IsTyping)
            {
                SendDraft();
            }
            else
            {
                OpenChat();
            }
        }
    }

    private void OnGUI()
    {
        if (IsInLobby() == false)
        {
            return;
        }

        if (lineStyle == null)
        {
            CreateStyles();
        }

        float width = Mathf.Min(Screen.width * 0.45f, 520f);
        float bottom = Screen.height - Margin;

        if (IsTyping)
        {
            GUI.SetNextControlName(InputControlName);
            draft = GUI.TextField(new Rect(Margin, bottom - LineHeight, width, LineHeight), draft, MaxLength, inputStyle);

            if (focusPending)
            {
                GUI.FocusControl(InputControlName);
                focusPending = Event.current.type != EventType.Repaint;
            }

            bottom -= LineHeight + 4f;
        }

        DrawMessages(bottom, width);
    }

    private void DrawMessages(float bottom, float width)
    {
        Color previous = GUI.color;
        int shown = 0;

        for (int i = messages.Count - 1; i >= 0 && shown < VisibleLines; i--)
        {
            float age = Time.time - messages[i].Timestamp;
            float alpha = IsTyping ? 1f : Mathf.Clamp01((MessageSeconds - age) / FadeSeconds);

            if (alpha <= 0f)
            {
                break;
            }

            shown++;
            Rect line = new Rect(Margin, bottom - shown * LineHeight, width, LineHeight);

            GUI.color = new Color(0f, 0f, 0f, alpha * 0.55f);
            GUI.DrawTexture(line, Texture2D.whiteTexture);

            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(line, messages[i].Text, lineStyle);
        }

        GUI.color = previous;
    }

    private void CreateStyles()
    {
        lineStyle = new GUIStyle(GUI.skin.label);
        lineStyle.fontSize = 16;
        lineStyle.alignment = TextAnchor.MiddleLeft;
        lineStyle.padding = new RectOffset(8, 8, 0, 0);
        lineStyle.normal.textColor = Color.white;

        inputStyle = new GUIStyle(GUI.skin.textField);
        inputStyle.fontSize = 16;
        inputStyle.alignment = TextAnchor.MiddleLeft;
    }

    private void OpenChat()
    {
        IsTyping = true;
        focusPending = true;
    }

    private void CloseChat()
    {
        IsTyping = false;
        focusPending = false;
        draft = string.Empty;
    }

    private void SendDraft()
    {
        string message = draft.Trim();

        if (message.Length > 0)
        {
            PlayerIdentity.Local.SendChat(message);
        }

        CloseChat();
    }
}
