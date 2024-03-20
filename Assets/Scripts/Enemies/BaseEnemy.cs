
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class BaseEnemy : UdonSharpBehaviour {
    [SerializeField] float health = 5f;
    [SerializeField] float speed = 0.1f;
    [SerializeField] MeshRenderer meshRenderer;

    private VRCPlayerApi localPlayer;
    protected bool MoveToPlayer;

    public virtual void Init() {
        speed += Random.Range(0f, 0.2f);
    }

    protected void ManualUpdate() {
        if (MoveToPlayer) {
            if (localPlayer == null)
                localPlayer = Networking.LocalPlayer;

            var playerPos = localPlayer.GetPosition();
            transform.position = Vector3.Lerp(transform.position, playerPos, speed * Time.deltaTime);
        }
    }

    protected void SetMaterialColor(Color clr) {
        var prop = new MaterialPropertyBlock();
        prop.SetColor("_Color", clr);
        meshRenderer.SetPropertyBlock(prop);
    }

    private void OnTriggerEnter(Collider other) {
        // TODO: проверить что за объект
        health--;
        if (health <= 0f) {
            Destroy(gameObject);
        }
    }
}
