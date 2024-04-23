using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common;

public class Treasure : MazeObject {
    public int value = 100;

    [SerializeField] public VRC_Pickup pickup;

    // network event
    public void Despawn() {
        Controller.GetChestPool.Return(this);
    }

}
