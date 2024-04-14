using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

// global script
public class FingerPistol : UdonSharpBehaviour {
    [SerializeField] MazeController controller;
    [SerializeField] GameObject projectilePrefab;

    private VRCPlayerApi localPlayer;
    private bool vrMode;
    private bool reloaded;

    private void Start() {
        localPlayer = Networking.LocalPlayer;
        vrMode = localPlayer.IsUserInVR();
        reloaded = true;
    }

    private void Update() {
        if (vrMode) {
            float secondaryIndexTrigger = Input.GetAxis("Oculus_CrossPlatform_SecondaryIndexTrigger");

            if (reloaded && secondaryIndexTrigger == 1f) {
                Shot();
                //reloaded = controller.MazeUI.IsNoReload;
            } else if (!reloaded && secondaryIndexTrigger < 1f) {
                reloaded = true;
            }
        } else {
            if (Input.GetMouseButtonDown(1) || Input.GetMouseButton(2)) {
                Shot();
            }
        }
    }

    private void Shot() {
        VRCPlayerApi.TrackingData trackData = localPlayer.GetTrackingData(
            vrMode
            ? VRCPlayerApi.TrackingDataType.RightHand
            : VRCPlayerApi.TrackingDataType.Head
        );
        GameObject projectileObj = Instantiate(projectilePrefab, trackData.position, trackData.rotation);
        var script = projectileObj.GetComponent<SimpleProjectile>();
        script.Init(controller, trackData.rotation, vrMode);
    }
}
