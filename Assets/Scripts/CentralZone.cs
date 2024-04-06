using UdonSharp;
using UnityEngine;

public class CentralZone : UdonSharpBehaviour {
    [SerializeField] private PoolObjects chestPool;

    private void OnTriggerEnter(Collider other) {
/*        var type1 = other.GetType(); // MeshCollider
        var type2 = other.gameObject.GetType(); // GameObject
        var layer = other.gameObject.layer; // 13
        var name = other.gameObject.name; // Treasure (test)

        MazeController.MazeUI.Log(
            $"new object in Central Zone: "
            + $"\n type1 = {type1}"
            + $"\n type2 = {type2}"
            + $"\n layer = {layer}"
            + $"\n name = {name}"
        );*/

        var component = other.gameObject.GetComponent<Treasure>();
        if (component != null) {
            component.pickup.Drop();
            component.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Treasure.Despawn));
        }
    }
}
