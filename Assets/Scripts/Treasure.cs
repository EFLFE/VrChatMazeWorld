using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class Treasure : MazeObject {
    public int value = 100;

    [SerializeField] private VRC_Pickup pickup;

    [UdonSynced, HideInInspector] public bool IsActiveSynced;

    public void Drop() {
        pickup.Drop();
    }

    // network event
    public void Despawn() {
        IsActiveSynced = false;
        Controller.GetChestPool.Return(this);
        RequestSerialization();
    }

    public override void OnDeserialization() {
        base.OnDeserialization();
        gameObject.SetActive(IsActiveSynced);
    }

}
