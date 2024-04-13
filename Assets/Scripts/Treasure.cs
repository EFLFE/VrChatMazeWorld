﻿using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class Treasure : MazeObject {
    public int value = 100;

    //private VRC_Pickup _pickup;
    //public VRC_Pickup pickup => _pickup ? _pickup : (_pickup = (VRC_Pickup) GetComponent(typeof(VRC_Pickup)));

    [SerializeField] public VRC_Pickup pickup;

    // network event
    public void Despawn() {
        Controller.GetChestPool.Return(this);
    }
}
