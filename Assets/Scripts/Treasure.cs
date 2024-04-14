using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common;

public class Treasure : MazeObject {
    public int value = 100;

    [SerializeField] private VRC_Pickup pickup;

    [UdonSynced, HideInInspector] public bool IsActiveSynced;

    public void Drop() {
        pickup.Drop();
    }

    // network event
    public void Despawn() {
        Controller.GetChestPool.Return(this);
        VRCObjectSync a;
    }

}
