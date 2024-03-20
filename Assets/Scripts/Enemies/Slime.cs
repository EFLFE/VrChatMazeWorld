using UnityEngine;
using VRC.SDKBase;

public class Slime : BaseEnemy {
    [SerializeField] float forceX = 100;
    [SerializeField] float forceY = 350;
    [SerializeField] float maxDistanceDelta = 1f;

    Rigidbody rigidbodyField;
    VRCPlayerApi localPlayer1;
    float jumpTimer;

    public override void Init() {
        base.Init();
        rigidbodyField = GetComponent<Rigidbody>();
        Color clr = Utils.GetRandomColor();
        clr.a = .7f;
        SetMaterialColor(clr);
        jumpTimer = 3f;
    }

    private void Update() {
        if (jumpTimer > 0) {
            jumpTimer -= Time.deltaTime;
            return;
        }

        if (localPlayer1 == null)
            localPlayer1 = Networking.LocalPlayer;

        // должен прыгать в состоянии покоя
        if (rigidbodyField.velocity.y != 0f)
            return;

        Vector3 playerPos = localPlayer1.GetPosition();
        Vector3 newPos = Vector3.MoveTowards(transform.position, playerPos, maxDistanceDelta);
        Vector3 disPos = (newPos - transform.position).normalized;
        rigidbodyField.AddForce(forceX * disPos.x, forceY, forceX * disPos.z);
        jumpTimer = Random.Range(0.5f, 1.5f);
    }
}
