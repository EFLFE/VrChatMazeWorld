using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common;

public class Treasure : MazeObject {
    public int value = 100;

    [SerializeField] public VRC_Pickup pickup;
    public override void Init(MazeController controller, int pool_id) {
        base.Init(controller, pool_id);
        pickup.InteractionText = $"Treasure #{pool_id}";
    }

    // network event
    public void Despawn() {
        if (!Networking.IsMaster) return;
        Controller.GetChestPool.Return(this);
    }

}
