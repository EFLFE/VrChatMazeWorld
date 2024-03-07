
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CustomInteractToggle : UdonSharpBehaviour
{
    public UdonSharpBehaviour TargetNetworkObject;
    public string TargetMethodName;

    public override void Interact()
    {
        TargetNetworkObject.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, TargetMethodName);
    }
}