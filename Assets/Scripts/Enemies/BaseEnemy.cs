using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using static VRC.Udon.Common.Interfaces.NetworkEventTarget;

public enum EnemyAnimState {
    Sleeping = 0, // to default anim state
    Idle = 1,
    Walking = 2,
    Dead = 3,
    Raise = 4,
}

public enum EnemyNetState {
    None = 0,
    Wakeup = 1,
    Dead = 2,
}

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class BaseEnemy : MazeObject {
    private const float MAX_PLAYER_DISTANCE = 20f;

    [SerializeField] private float startedHealth = 5f;
    [SerializeField] private float startedSpeed = 1f;
    [SerializeField] private float rotateSpeed = 4f;

    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Rigidbody rigidbodyRef;
    [SerializeField] private Collider baseCollider;
    [SerializeField] private Animator baseAnimator;

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip wakeupClip;
    [SerializeField] private AudioClip deadClip;
    [SerializeField] private AudioClip impactClip;

    private MazeController controller;
    private float health;
    private float speed;

    // пробуждение монстра, когда игрок в поле зрения
    private bool sleeping;
    private bool wasDead;
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
        health = startedHealth;
        speed = startedSpeed + Random.Range(0f, 0.25f);
        sleeping = true;
        wasDead = false;
        netState = EnemyNetState.None;

        // go to default state
        AnimState = EnemyAnimState.Sleeping;
        baseAnimator.Play("Default");
        baseCollider.enabled = true;
        rigidbodyRef.isKinematic = false;
        rigidbodyRef.useGravity = true;
        wakeupCheckTimer = 1f;

        RequestSerialization();
    }

    public override void ManualUpdate() {
        if (IsDead)
            return;

        if (sleeping) {
            SleepModeUpdate();
            return;
        }

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

        if (MoveToPlayer) {
            Vector3 pos = Position;
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
                    Position = Vector3.MoveTowards(pos, playerPos, speed * Time.deltaTime);
                    RotateTo(pos, playerPos);
                    AnimState = EnemyAnimState.Walking;
                } else {
                    AnimState = EnemyAnimState.Idle;
                }
            }
        }

        base.ManualUpdate();
    }

    private void SleepModeUpdate() {
        if (Input.GetKey(KeyCode.R)) {
            if (controller.PlayersManager.TryGetNearPlayer(Position, out PlayerData playerDataDebug)) {
                Debug.DrawLine(Position, playerDataDebug.GetGlobalPos, Color.red);
            }
        }

        wakeupCheckTimer -= Time.deltaTime;
        if (wakeupCheckTimer > 0f)
            return;

        wakeupCheckTimer = 0.1f;
        Vector3 pos = Position;
        pos.y += 0.5f;
        if (!controller.PlayersManager.TryGetNearPlayer(pos, out PlayerData playerData))
            return;

        float dist = Vector3.Distance(pos, playerData.GetGlobalPos);
        if (dist > MAX_PLAYER_DISTANCE)
            return;

        int gridID = GetRoomID(pos);
        int playerGridID = playerData.GridID;
        if (gridID == playerGridID) {
            SetNetState(EnemyNetState.Wakeup);
        }
    }

    private int GetRoomID(Vector3 pos) {
        var maze = controller.MazeGenerator;
        int x = (int)(maze.Size / 2f + pos.x / MazeBuilder.ROOMS_OFFSET);
        int y = (int)(maze.Size / 2f + pos.z / MazeBuilder.ROOMS_OFFSET);
        int z = (int)(maze.Height - maze.StartRoomHeight + pos.y / MazeBuilder.ROOMS_OFFSET);
        return maze.GetId(x, y, z);
    }

    protected virtual void OnTouchPlayer(PlayerData player) {
        var leftHand = player.GetPlayerApi.GetPickupInHand(VRC_Pickup.PickupHand.Left);
        var rightHand = player.GetPlayerApi.GetPickupInHand(VRC_Pickup.PickupHand.Right);
        if (leftHand != null)
            leftHand.Drop();
        if (rightHand != null)
            rightHand.Drop();

        player.GetPlayerApi.TeleportTo(Vector3.zero, player.GetPlayerApi.GetRotation());
    }

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
        //controller.MazeUI.UILog($"Enemy got {value} damage (hp {health}) {log}");

        if (IsDead)
            return;

        if (sleeping) {
            SetNetState(EnemyNetState.Wakeup);
            return;
        }

        health -= value;

        if (health <= 0f) {
            SetNetState(EnemyNetState.Dead);
        } else {
            audioSource.PlayOneShot(impactClip);
            rigidbodyRef.AddForce(Vector3.up * Mathf.Clamp(value, 1f, 2f), ForceMode.Impulse);
        }
    }

    // ==== Network ====
    private EnemyNetState netState;

    private void SetNetState(EnemyNetState enemyNetState) {
        if (netState != enemyNetState) {
            netState = enemyNetState;
            switch (netState) {
                case EnemyNetState.Wakeup:
                    SendCustomNetworkEvent(All, "NetWakeup");
                    break;

                case EnemyNetState.Dead:
                    SendCustomNetworkEvent(All, "NetDead");
                    break;
            }
        }
    }

    public void NetWakeup() {
        netState = EnemyNetState.None;
        if (sleeping) {
            sleeping = false;
            AnimState = EnemyAnimState.Raise;
            audioSource.PlayOneShot(wakeupClip);
        }
    }

    public void NetDead() {
        netState = EnemyNetState.None;
        if (wasDead) return;

        wasDead = true;
        audioSource.PlayOneShot(deadClip);
        AnimState = EnemyAnimState.Dead;
        baseCollider.enabled = false;
        rigidbodyRef.useGravity = false;
        rigidbodyRef.isKinematic = true;
    }

}
