using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class Sword : BaseWeapon {
    [SerializeField] private Transform swordTip;
    [SerializeField] private TrailRenderer tipTrail;

    private bool tipTrailEmitting;
    private Vector3 lastSwordTipPos;

    public override void Init(MazeController controller, int pool_id) {
        base.Init(controller, pool_id);
        lastSwordTipPos = swordTip.position;
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

}
