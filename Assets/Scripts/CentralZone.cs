
using System.ComponentModel;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CentralZone : UdonSharpBehaviour {

    public MazeController MazeController;

    public void Init(MazeController MazeController) {
        this.MazeController = MazeController;
    }

    void Start() {

    }

    private void OnTriggerEnter(Collider other) {
        var type1 = other.GetType(); // MeshCollider
        var type2 = other.gameObject.GetType(); // GameObject
        var layer = other.gameObject.layer; // 13
        var name = other.gameObject.name; // Treasure (test)

/*        MazeController.MazeUI.Log(
            $"new object in Central Zone: "
            + $"\n type1 = {type1}"
            + $"\n type2 = {type2}"
            + $"\n layer = {layer}"
            + $"\n name = {name}"
        );*/

        var component = other.gameObject.GetComponent<Treasure>();
        if (component != null) {
            Destroy(other.gameObject);
            MazeController.OnTreasureGathered();
        }
    }
}
