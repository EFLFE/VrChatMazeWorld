﻿using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class PlayerData : UdonSharpBehaviour {
    [UdonSynced] private int playerID;

    private MazeController controller;
    private VRCPlayerApi playerApi;
    private Vector3 globalPos;
    private Vector3Int gridPos;

    /// <summary>
    /// Get player or null (depend on saved player ID).
    /// </summary>
    public VRCPlayerApi GetPlayerApi => playerApi;
    public int GetPlayerID => playerID;
    public Vector3 GetGlobalPos => globalPos;
    public Vector3Int GetGridPos => gridPos;

    // VR data
    private const float minPunchForce = 0.03f;
    private const float handResistanceTime = 0.35f;

    private bool isUserInVR;
    private Vector3 lastRightHandPos;
    private Vector3 lastLeftHandPos;
    private float resistanceLeftHandTime;
    private float resistanceRightHandTime;
    private Vector3 lastOriginPos;

    public bool IsUserInVR => isUserInVR;
    private float rightHandForce;
    private float leftHandForce;

    public bool LeftHandPunch { get; private set; }
    public bool RightHandPunch { get; private set; }

    public void Init(MazeController controller) {
        this.controller = controller;
        playerID = -1;
    }

    public bool IsValid() {
        if (playerID == -1)
            return false;
        return playerApi != null && playerApi.IsValid();
    }

    /// <summary>
    /// Has player id but player not valid.
    /// </summary>
    public bool IsLostPlayer() {
        if (playerID == -1)
            return false;
        return playerApi == null || !playerApi.IsValid();
    }

    public void BindPlayer(VRCPlayerApi player) {
        playerID = player.playerId;
        playerApi = player;
        isUserInVR = playerApi.IsUserInVR();
        controller.MazeUI.UILog($"IsUserInVR: {isUserInVR}");
    }

    public void Unbind() {
        playerID = -1;
        playerApi = null;
    }

    public void ManualUpdate() {
        if (playerApi == null || !playerApi.IsValid())
            return;

        float deltaTime = Time.deltaTime;

        Vector3 pos = playerApi.GetPosition();
        globalPos = pos;
        float halfSize = controller.MazeGenerator.Size / 2f;
        float halfHeight = controller.MazeGenerator.Height / 2f;
        gridPos.x = (int)(halfSize + pos.x / MazeBuilder.ROOMS_OFFSET);
        gridPos.y = (int)(halfSize + pos.z / MazeBuilder.ROOMS_OFFSET);
        gridPos.z = (int)(halfHeight + pos.y / MazeBuilder.ROOMS_OFFSET + 1f);

        // VR data
        isUserInVR = playerApi.IsUserInVR();
        if (isUserInVR) {
            // calc hands force
            VRCPlayerApi.TrackingData rightHand = playerApi.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
            VRCPlayerApi.TrackingData leftHand = playerApi.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
            VRCPlayerApi.TrackingData origin = playerApi.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);

            Vector3 originPos = origin.position;
            float originDist = Vector3.Distance(lastOriginPos, originPos);
            lastOriginPos = originPos;

            if (lastRightHandPos == Vector3.zero) {
                lastRightHandPos = rightHand.position;
                rightHandForce = 0f;
            } else {
                float dist = Vector3.Distance(lastRightHandPos, rightHand.position);
                lastRightHandPos = rightHand.position;
                rightHandForce = dist - originDist;
            }

            if (lastLeftHandPos == Vector3.zero) {
                lastLeftHandPos = leftHand.position;
                leftHandForce = 0f;
            } else {
                float dist = Vector3.Distance(lastLeftHandPos, leftHand.position);
                lastLeftHandPos = leftHand.position;
                leftHandForce = dist - originDist;
            }

            if (resistanceLeftHandTime > 0f) {
                resistanceLeftHandTime -= deltaTime;
                LeftHandPunch = false;
            } else {
                LeftHandPunch = leftHandForce > minPunchForce;
            }

            if (resistanceRightHandTime > 0f) {
                resistanceRightHandTime -= deltaTime;
                RightHandPunch = false;
            } else {
                RightHandPunch = rightHandForce > minPunchForce;
            }
        }
    }

    public bool TryPunchLeftHand() {
        if (LeftHandPunch) {
            resistanceLeftHandTime = handResistanceTime;
            return true;
        }
        return false;
    }

    public bool TryPunchRightHand() {
        if (RightHandPunch) {
            resistanceRightHandTime = handResistanceTime;
            return true;
        }
        return false;
    }

    public VRCPlayerApi.TrackingData GetTrackingData(VRCPlayerApi.TrackingDataType trackingData) {
        return playerApi.GetTrackingData(trackingData);
    }

}