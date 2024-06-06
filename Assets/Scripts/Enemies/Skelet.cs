using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class Skelet : BaseEnemy {
    public override void Init(MazeController controller, int pool_id) {
        base.Init(controller, pool_id);
        MoveToPlayer = true;
    }
}
