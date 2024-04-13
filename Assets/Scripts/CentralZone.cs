using UdonSharp;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class CentralZone : UdonSharpBehaviour {
    [SerializeField] private PoolObjects chestPool;
    [SerializeField] public MazeController MazeController;

    private void OnTriggerEnter(Collider model) {
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

        //var component = other.gameObject.GetComponent<Treasure>();
        var treasure = model.gameObject.transform.parent.GetComponent<Treasure>();
        if (treasure != null) {
            MazeController.MazeUI.Log("Treasure found in CentralZone!"); // one time on random amount of clients
            treasure.pickup.Drop();
            if (Networking.IsOwner(model.gameObject)) {
                MazeController.MazeUI.Log("We are the owner of the treasure!"); // one time on treasure owner
                MazeController.SendCustomNetworkEvent(
                    VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner,
                    nameof(MazeController.OnTreasureGathered)
                );
                treasure.SendCustomNetworkEvent(
                    VRC.Udon.Common.Interfaces.NetworkEventTarget.All,
                    nameof(Treasure.Despawn)
                );
            }
        }
    }
}
