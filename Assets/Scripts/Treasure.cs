using UnityEngine;
using VRC.SDKBase;

public class Treasure : MazeObject {
    public int value = 100;

    [SerializeField] private VRC_Pickup pickup;

    public void Drop() {
        pickup.Drop();
    }

    // network event
    public void Despawn() {
        Controller.GetChestPool.Return(this);
    }
}
