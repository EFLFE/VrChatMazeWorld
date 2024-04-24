using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class CustomInteractToggle : UdonSharpBehaviour {
    [SerializeField] private string interactionText;

    public UdonSharpBehaviour TargetNetworkObject;
    public string TargetMethodName;
    public bool for_owner_only = true;

    private void Start() {
        InteractionText = interactionText;
    }

    public override void Interact() {
        TargetNetworkObject.SendCustomNetworkEvent(
            for_owner_only
            ? VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner
            : VRC.Udon.Common.Interfaces.NetworkEventTarget.All,
            TargetMethodName
        );
    }
}