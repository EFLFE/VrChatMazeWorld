using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class NextLevelDebug : UdonSharpBehaviour {
    [SerializeField] private MazeController controller;    

    public override void Interact() {
        base.Interact();
        controller.NextLevelMaster();
    }
}
