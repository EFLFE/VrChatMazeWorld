using UnityEngine;
using VRC.SDKBase;

public class Treasure : MazeObject {
    [SerializeField] private VRC_Pickup pickup;
    [SerializeField] private AudioSource[] collisionAudios;

    public int value = 100;

    public override void Init(MazeController controller, int pool_id) {
        base.Init(controller, pool_id);
        pickup.InteractionText = $"Treasure #{pool_id}";
    }

    public void Drop() {
        pickup.Drop();
    }

    // network event
    public void Despawn() {
        if (!Networking.IsMaster) return;
        Controller.GetChestPool.Return(this);
    }

    private void OnCollisionEnter(Collision collision) {
        var audioIndex = Random.Range(0, collisionAudios.Length);
        var audioSource = collisionAudios[audioIndex];
        audioSource.pitch = Random.Range(0.75f, 1.25f);
        audioSource.Play();
    }

}
