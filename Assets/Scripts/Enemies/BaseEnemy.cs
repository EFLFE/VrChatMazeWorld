using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class BaseEnemy : UdonSharpBehaviour {
    [SerializeField, UdonSynced] private float health = 5f;
    [SerializeField, UdonSynced] private float speed = 1f;
    [SerializeField, UdonSynced] private float rotateSpeed = 4f;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Rigidbody rigidbodyRef;

    private MazeController controller;
    protected bool MoveToPlayer;

    public virtual void Init(MazeController controller) {
        this.controller = controller;
        speed += Random.Range(0f, 0.2f);
        RequestSerialization();
    }

    protected virtual void ManualUpdate() {
        if (MoveToPlayer) {
            Vector3 pos = transform.position;
            if (controller.PlayersManager.TryGetNearPlayer(pos, out PlayerData playerData)) {
                Vector3 playerPos = playerData.GetGlobalPos;
                pos = Vector3.MoveTowards(pos, playerPos, speed * Time.deltaTime);

                // 0.6 = впритык
                float dist = Vector3.Distance(pos, playerPos);
                if (dist < 0.8f) {
                    OnTouchPlayer(playerData.GetPlayerApi);
                }

                transform.position = pos;
                RotateTo(pos, playerPos);
            }
        }
    }

    protected virtual void OnTouchPlayer(VRCPlayerApi player) { }

    private void RotateTo(Vector3 from, Vector3 target) {
        from.y = 0f;
        target.y = 0f;
        Vector3 direction = (target - from).normalized;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotateSpeed * Time.deltaTime);
    }

    protected void SetMaterialColor(Color clr) {
        var prop = new MaterialPropertyBlock();
        prop.SetColor("_Color", clr);
        meshRenderer.SetPropertyBlock(prop);
    }

    private void OnTriggerEnter(Collider other) {
        // TODO: проверить что за other объект
        health--;
        if (health <= 0f) {
            Destroy(gameObject);
        }
    }
}
