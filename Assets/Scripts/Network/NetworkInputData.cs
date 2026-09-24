using Fusion;

public enum Buttons
{
    Jump,
}

public struct NetworkInputData : INetworkInput
{
    public float Forward;
    public float Turn;
    public NetworkButtons buttons;
}
