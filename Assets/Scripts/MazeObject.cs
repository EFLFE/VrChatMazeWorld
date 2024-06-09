using UdonSharp;
using UnityEngine;

/// <summary>
/// Abstract class.
/// </summary>
public class MazeObject : UdonSharpBehaviour {
    public int pool_id { get; private set; }
    protected MazeController Controller { get; private set; }

    public Vector3 RespawnPos;

    public virtual void Init(MazeController controller, int pool_id) {
        Controller = controller;
        this.pool_id = pool_id;
    }

    public virtual void ReturnedToPool() { }

    public virtual void ManualUpdate() { }
}
