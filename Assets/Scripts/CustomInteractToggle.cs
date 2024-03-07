using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class CustomInteractToggle : UdonSharpBehaviour {
    [SerializeField] private string interactionText;

    public UdonSharpBehaviour TargetNetworkObject;
    public string TargetMethodName;

    private void Start() {
        InteractionText = interactionText;
    }

    public override void Interact() {
        TargetNetworkObject.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, TargetMethodName);
    }
}