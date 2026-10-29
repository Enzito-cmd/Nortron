using Fusion;
using TMPro;
using UnityEngine;

public class PlayerIdentity : NetworkBehaviour
{
    public const int ColorCount = 4;

    private const string ColorMaterialName = "BikeColor";
    private const float ChatCooldownSeconds = 0.5f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Transform tagRoot;
    [SerializeField] private bool hideForOwner;
    [SerializeField] private float emissionIntensity = 2f;
    [SerializeField] private Color[] bikeColors =
    {
        new Color(1f, 0.1f, 0.1f),
        new Color(0f, 0.85f, 1f),
        new Color(0.6f, 0.2f, 1f),
        new Color(1f, 0.9f, 0.1f)
    };

    private float nextChatTime;

    [Networked] public NetworkString<_16> Nickname { get; set; }
    [Networked] public int ColorIndex { get; set; }

    public static PlayerIdentity Local { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public override void Spawned()
    {
        DisplayName = Nickname.Value;

        if (Object.HasInputAuthority)
        {
            Local = this;
        }

        ApplyColor();
        ShowName();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Local == this)
        {
            Local = null;
        }
    }

    public override void Render()
    {
        if (tagRoot != null && Camera.main != null)
        {
            tagRoot.rotation = Camera.main.transform.rotation;
        }
    }

    public void SendChat(string message)
    {
        RPC_Say(message);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_Say(NetworkString<_64> message)
    {
        string text = message.Value.Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ').Trim();

        if (text.Length == 0 || Time.time < nextChatTime)
        {
            return;
        }

        nextChatTime = Time.time + ChatCooldownSeconds;
        RPC_Show(Nickname, text);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Show(NetworkString<_16> sender, NetworkString<_64> message)
    {
        ChatBox.AddMessage(sender.Value, message.Value);
    }

    private void ApplyColor()
    {
        if (bikeColors.Length == 0)
        {
            return;
        }

        Color color = bikeColors[ColorIndex % bikeColors.Length];

        foreach (Renderer meshRenderer in GetComponentsInChildren<Renderer>())
        {
            Material[] shared = meshRenderer.sharedMaterials;
            Material[] instances = null;

            for (int i = 0; i < shared.Length; i++)
            {
                if (shared[i] == null || shared[i].name != ColorMaterialName)
                {
                    continue;
                }

                if (instances == null)
                {
                    instances = meshRenderer.materials;
                }

                instances[i].SetColor(BaseColorId, color);
                instances[i].SetColor(EmissionColorId, color * emissionIntensity);
                instances[i].EnableKeyword("_EMISSION");
            }
        }
    }

    private void ShowName()
    {
        if (tagRoot == null && nameText != null)
        {
            tagRoot = nameText.transform;
        }

        if (nameText != null)
        {
            nameText.text = DisplayName;
        }

        if (tagRoot != null)
        {
            tagRoot.gameObject.SetActive(hideForOwner == false || Object.HasInputAuthority == false);
        }
    }
}
