using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

/// <summary>
/// Manual sync game object transform props.
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class MazeObjectSync : UdonSharpBehaviour {
    [UdonSynced(UdonSyncMode.None)] private Vector3 pos;

    private float timer;

    /// <summary>
    /// Synced gameObject position.
    /// </summary>
    public Vector3 Position {
        get => pos;
        set {
            pos = value;
            transform.position = pos;
        }
    }

    private void Update() {
        timer += Time.deltaTime;
        if (timer < 1f) return;
        timer = 0f;

        if (Networking.IsOwner(gameObject)) {
            pos = transform.position;
        }
        RequestSerialization();
    }
}
