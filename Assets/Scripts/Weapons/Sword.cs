using UdonSharp;
using UnityEngine;

public class Sword : UdonSharpBehaviour {
    [SerializeField] private GameObject swordTip;

    private MazeController controller;

    public void Init(MazeController mazeController) {
        controller = mazeController;
    }

    public void ManualUpdate() {

    }

}
