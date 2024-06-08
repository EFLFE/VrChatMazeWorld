using UdonSharp;
using UnityEngine;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDKBase;
using VRC.Udon;

public enum EnemyAnimState {
    Idle = 0,
    Sleeping = 1,
    Walking = 2,
    Dead = 3,
    Raise = 4,
}

public class BaseEnemy : MazeObject {
    private const float MAX_PLAYER_DISTANCE = 20f;

    [SerializeField, UdonSynced] private float health = 5f;
    [SerializeField, UdonSynced] private float speed = 1f;
    [SerializeField, UdonSynced] private float rotateSpeed = 4f;

    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Rigidbody rigidbodyRef;
    [SerializeField] private Collider baseCollider;
    [SerializeField] private Animator baseAnimator;

    private MazeController controller;

    // пробуждение монстра, когда игрок в поле зрения
    private bool sleeping;
    private float wakeupCheckTimer;
    private RaycastHit[] raycastResult;

    private EnemyAnimState animState;
    private EnemyAnimState AnimState {
        get => animState;
        set {
            if (animState != value) {
                animState = value;
                baseAnimator.SetInteger("State", (int)animState);
            }
        }
    }

    protected bool MoveToPlayer;

    public bool IsDead => health <= 0f;
    public MeshRenderer GetMeshRenderer => meshRenderer;

    public override void Init(MazeController controller, int pool_id) {
        if (raycastResult == null)
            raycastResult = new RaycastHit[2];

        base.Init(controller, pool_id);
        this.controller = controller;
        speed += Random.Range(0f, 0.2f);
        sleeping = true;

        AnimState = EnemyAnimState.Sleeping;
        baseCollider.enabled = true;
        rigidbodyRef.isKinematic = false;
        rigidbodyRef.useGravity = true;
        wakeupCheckTimer = 1f;

        RequestSerialization();
    }

    public override void ManualUpdate() {
        if (IsDead)
            return;

        PlayerData localPlayerData = controller.PlayersManager.GetLocalPlayer();

        if (localPlayerData.TryPunchLeftHand()) {
            Vector3 leftHandPos = localPlayerData.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
            if (baseCollider.bounds.Contains(leftHandPos)) {
                Damage(1, "left hand");
            }
        }
        if (localPlayerData.TryPunchRightHand()) {
            Vector3 rightHandPos = localPlayerData.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
            if (baseCollider.bounds.Contains(rightHandPos)) {
                Damage(1, "right hand");
            }
        }

        if (sleeping) {
            SleepModeUpdate();
        } else if (MoveToPlayer) {
            Vector3 pos = transform.position;
            if (controller.PlayersManager.TryGetNearPlayer(pos, out PlayerData playerData)) {
                Vector3 playerPos = playerData.GetGlobalPos;

                // 0.6 = впритык к игроку
                float dist = Vector3.Distance(pos, playerPos);
                if (dist < 0.8f) {
                    // не двигаться
                    OnTouchPlayer(playerData);
                    RotateTo(pos, playerPos);
                    AnimState = EnemyAnimState.Idle;
                } else if (dist < MAX_PLAYER_DISTANCE) {
                    // move to player
                    transform.position = Vector3.MoveTowards(pos, playerPos, speed * Time.deltaTime);
                    RotateTo(pos, playerPos);
                    AnimState = EnemyAnimState.Walking;
                } else {
                    AnimState = EnemyAnimState.Idle;
                }
            }
        }
    }

    private void SleepModeUpdate() {
        wakeupCheckTimer -= Time.deltaTime;
        if (wakeupCheckTimer > 0f)
            return;

        wakeupCheckTimer = 0.1f;
        Vector3 pos = transform.position;
        pos.y += 0.5f;
        if (!controller.PlayersManager.TryGetNearPlayer(pos, out PlayerData playerData))
            return;

        float dist = Vector3.Distance(pos, playerData.GetGlobalPos);
        if (dist > MAX_PLAYER_DISTANCE)
            return;

        var ray = new Ray(pos, playerData.GetGlobalPos);
        int hitCount = Physics.RaycastNonAlloc(ray, raycastResult, dist, Utils.LAYER_WALL);

        if (hitCount == 0) {
            Wakeup();
        }
    }

    private void Wakeup() {
        if (AnimState == EnemyAnimState.Sleeping) {
            AnimState = EnemyAnimState.Raise;
            sleeping = false;
        }
    }

    protected virtual void OnTouchPlayer(PlayerData player) { }

    private void RotateTo(Vector3 from, Vector3 target) {
        from.y = 0f;
        target.y = 0f;
        Vector3 direction = (target - from).normalized;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(direction),
            rotateSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.layer == Utils.LAYER_WEAPON) {
            var weapontPart = other.gameObject.GetComponent<WeapontPart>();
            if (weapontPart.Weapon.CanDamage)
                Damage(5, $"collider '{other.gameObject.name}', Weapon");
        }
    }

    public void Damage(float value = 1f, string log = null) {
        if (IsDead)
            return;

        if (sleeping) {
            Wakeup();
            return;
        }

        health -= value;
        //controller.MazeUI.UILog($"Enemy got {value} damage (hp {health}) {log}");

        if (health <= 0f) {
            AnimState = EnemyAnimState.Dead;
            baseCollider.enabled = false;
            rigidbodyRef.useGravity = false;
            rigidbodyRef.isKinematic = true;
        } else {
            rigidbodyRef.AddForce(Vector3.back * Mathf.Clamp(value, 1, 3), ForceMode.Impulse);
        }
    }

}
