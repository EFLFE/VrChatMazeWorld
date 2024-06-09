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

    [SerializeField, UdonSynced] private float startedHealth = 5f;
    [SerializeField, UdonSynced] private float startedSpeed = 1f;
    [SerializeField, UdonSynced] private float rotateSpeed = 4f;

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

        // go to default state
        baseAnimator.Play("Default");
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
        if (Input.GetKey(KeyCode.R)) {
            if (controller.PlayersManager.TryGetNearPlayer(transform.position, out PlayerData playerDataDebug)) {
                Debug.DrawLine(transform.position, playerDataDebug.GetGlobalPos, Color.red);
            }
        }

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

        int gridID = GetRoomID(pos);
        int playerGridID = playerData.GridID;
        if (gridID == playerGridID) {
            Wakeup();
        }
    }

    private void Wakeup() {
        if (AnimState == EnemyAnimState.Sleeping) {
            AnimState = EnemyAnimState.Raise;
            sleeping = false;
            audioSource.PlayOneShot(wakeupClip);
        }
    }

    private int GetRoomID(Vector3 pos) {
        var maze = controller.MazeGenerator;
        int x = (int)(maze.Size / 2f + pos.x / MazeBuilder.ROOMS_OFFSET);
        int y = (int)(maze.Size / 2f + pos.z / MazeBuilder.ROOMS_OFFSET);
        int z = (int)(maze.Height - maze.StartRoomHeight + pos.y / MazeBuilder.ROOMS_OFFSET);
        return maze.GetId(x, y, z);
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
            audioSource.PlayOneShot(deadClip);
            AnimState = EnemyAnimState.Dead;
            baseCollider.enabled = false;
            rigidbodyRef.useGravity = false;
            rigidbodyRef.isKinematic = true;
        } else {
            audioSource.PlayOneShot(impactClip);
            rigidbodyRef.AddForce(new Vector3(0f, Mathf.Clamp(value, 1f, 2f), 0f), ForceMode.Impulse);
        }
    }

}
