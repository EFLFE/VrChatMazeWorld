﻿using UdonSharp;
using VRC.SDKBase;

public class Treasure : MazeObject {
    public int value = 100;

    private VRC_Pickup _pickup;
    public VRC_Pickup pickup => _pickup ? _pickup : (_pickup = (VRC_Pickup) GetComponent(typeof(VRC_Pickup)));

    public void Despawn() {
        Controller.OnTreasureGathered();
        Controller.GetChestPool.Return(this);
    }
}
