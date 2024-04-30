using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ToggleCeilingDebug : UdonSharpBehaviour {
    [SerializeField] private Transform mazeCeilingContainer;

    [UdonSynced] private bool isVisibleState;

    public void SetCeilingsVisible(bool isVisible) {
        isVisibleState = isVisible;
        mazeCeilingContainer.gameObject.SetActive(isVisible);
    }

    public void SwitchCeilingVisible() {
        isVisibleState = !mazeCeilingContainer.gameObject.activeSelf;
        SetCeilingsVisible(isVisibleState);
        RequestSerialization();
    }

    public override void OnDeserialization() {
        base.OnDeserialization();
        SetCeilingsVisible(isVisibleState);
    }
}
