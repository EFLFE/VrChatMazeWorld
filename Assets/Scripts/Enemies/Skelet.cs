using UnityEngine;

public class Skelet : BaseEnemy {
    [SerializeField] private MazeController controllerRef;

    private void Start() {
        Init(controllerRef);
        MoveToPlayer = true;
    }

    private void Update() {
        if (controllerRef.MazeBuilder.MazeReady) // remove in ManualUpdate
            ManualUpdate();
    }
}
