using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class Sword : BaseWeapon {
    [SerializeField] private Transform swordTip;
    [SerializeField] private TrailRenderer tipTrail;
    [SerializeField] private VRCPickup vrcPickup;

    private bool tipTrailEmitting;
    private Vector3 lastSwordTipPos;
    private Vector3 startedPos;

    public override void Init(MazeController controller, int pool_id) {
        base.Init(controller, pool_id);
        lastSwordTipPos = swordTip.position;
        startedPos = transform.localPosition;
    }

    public override void ManualUpdate() {
        base.ManualUpdate();

        var swordTipPos = swordTip.position;
        float dist = Vector3.Distance(swordTipPos, lastSwordTipPos);
        lastSwordTipPos = swordTipPos;
        const float minDamageForce = 0.060f;
        CanDamage = dist >= minDamageForce;

        if (tipTrailEmitting != CanDamage) {
            tipTrailEmitting = CanDamage;
            tipTrail.emitting = tipTrailEmitting;
        }
    }

    public void ReturnToSpawn() {
        if (!vrcPickup.IsHeld)
            transform.SetLocalPositionAndRotation(startedPos, Quaternion.identity);
    }

}
