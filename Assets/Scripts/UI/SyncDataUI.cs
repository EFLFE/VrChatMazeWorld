using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SyncDataUI : UdonSharpBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI _mainText;

    public void Clear() {
        _mainText.text = "";
    }

    public void AddText(string text) {
        _mainText.text += text + "\n";
    }
}
