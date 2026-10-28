using Fusion;
using TMPro;
using UnityEngine;

public class PlayerIdentity : NetworkBehaviour
{
    private const float FontSize = 2.5f;

    [SerializeField] private float tagHeight = 2.2f;
    [SerializeField] private bool hideForOwner;

    private Transform label;

    [Networked] public NetworkString<_16> Nickname { get; set; }

    public static PlayerIdentity Local { get; private set; }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            Local = this;
        }

        CreateLabel();
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
        if (label != null && Camera.main != null)
        {
            label.rotation = Camera.main.transform.rotation;
        }
    }

    public void SendChat(string message)
    {
        RPC_Say(message);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_Say(NetworkString<_64> message)
    {
        RPC_Show(Nickname, message);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Show(NetworkString<_16> sender, NetworkString<_64> message)
    {
        ChatBox.AddMessage(sender.Value, message.Value);
    }

    private void CreateLabel()
    {
        GameObject labelObject = new GameObject("NameTag");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, tagHeight, 0f);

        TextMeshPro text = labelObject.AddComponent<TextMeshPro>();
        text.text = Nickname.Value;
        text.fontSize = FontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black;
        text.rectTransform.sizeDelta = new Vector2(8f, 1f);

        labelObject.SetActive(hideForOwner == false || Object.HasInputAuthority == false);
        label = labelObject.transform;
    }
}
