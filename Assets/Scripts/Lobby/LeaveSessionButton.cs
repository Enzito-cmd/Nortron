using UnityEngine;
using UnityEngine.UI;

public class LeaveSessionButton : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        NetworkManager.Instance.LeaveSession();
    }
}
