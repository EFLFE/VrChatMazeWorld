using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ToggleCeilingDebug : UdonSharpBehaviour {
    [SerializeField] private Transform mazeCeilingContainer;
    private bool isVisibleState;

    public override void Interact() {
        base.Interact();
        SwitchCeilingVisible();
    }

    public void SetCeilingsVisible(bool isVisible) {
        isVisibleState = isVisible;
        mazeCeilingContainer.gameObject.SetActive(isVisible);
    }

    public void SwitchCeilingVisible() {
        isVisibleState = !mazeCeilingContainer.gameObject.activeSelf;
        SetCeilingsVisible(isVisibleState);
        RequestSerialization();
    }
}
