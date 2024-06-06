using UnityEngine;
using VRC.SDKBase;

public class Slime : BaseEnemy {
    [SerializeField] float forceX = 100;
    [SerializeField] float forceY = 350;
    [SerializeField] float maxDistanceDelta = 1f;

    Rigidbody rigidbodyField;
    VRCPlayerApi localPlayer1;
    float jumpTimer;

    public override void Init(MazeController controller, int poolId) {
        base.Init(controller, poolId);
        rigidbodyField = GetComponent<Rigidbody>();
        Color clr = GetRandomColor();
        clr.a = .7f;
        Utils.SetMaterialColor(GetMeshRenderer, clr);
        jumpTimer = 3f;
    }

    public static Color GetRandomColor() {
        Color clr;
        switch (Random.Range(0, 8)) {
            case 0: clr = Color.yellow; break;
            case 1: clr = Color.red; break;
            case 2: clr = Color.magenta; break;
            case 3: clr = Color.grey; break;
            case 4: clr = Color.green; break;
            case 5: clr = Color.cyan; break;
            case 6: clr = Color.blue; break;
            case 7: clr = Color.black; break;
            default: clr = Color.white; break;
        }
        return clr;
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
