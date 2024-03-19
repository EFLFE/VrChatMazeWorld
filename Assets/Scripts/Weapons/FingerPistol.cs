using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class FingerPistol : UdonSharpBehaviour {
    [SerializeField] GameObject projectilePrefab;

    private void Update() {
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButton(2)) {
            Shot();
        }
    }

    private void Shot() {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        bool vrMode = localPlayer.IsUserInVR();
        VRCPlayerApi.TrackingData trackData = localPlayer.GetTrackingData(vrMode ? VRCPlayerApi.TrackingDataType.RightHand : VRCPlayerApi.TrackingDataType.Head);
        GameObject projectileObj = Instantiate(projectilePrefab, trackData.position, trackData.rotation);
        var script = projectileObj.GetComponent<SimpleProjectile>();
        script.Init(trackData.rotation, vrMode);
    }
}
