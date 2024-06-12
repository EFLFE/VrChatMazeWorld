using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

/// <summary>
/// Abstract class.
/// </summary>
public class MazeObject : UdonSharpBehaviour {
    [UdonSynced(UdonSyncMode.None)] private Vector3 pos;

    private float syncTimer;

    public int PoolID { get; private set; }
    protected MazeController Controller { get; private set; }

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

    public Vector3 RespawnPos;

    public virtual void Init(MazeController controller, int pool_id) {
        Controller = controller;
        PoolID = pool_id;
        pos = transform.position;
    }

    public virtual void ReturnedToPool() { }

    public virtual void ManualUpdate() {
        syncTimer += Time.deltaTime;
        if (syncTimer < 0.5f) return;
        syncTimer = 0f;

        if (Networking.IsOwner(gameObject)) {
            pos = transform.position;
        }
        RequestSerialization();
    }

    public override void OnDeserialization() {
        base.OnDeserialization();
        if (!Networking.IsOwner(gameObject))
            transform.position = pos;
    }

}
