using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using static VRC.Udon.Common.Interfaces.NetworkEventTarget;

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
            MazeController.MazeUI.UILog("Treasure found in CentralZone!"); // one time on random amount of clients
            treasure.Drop();
            if (Networking.IsOwner(model.gameObject)) {
                treasure.IsActiveSynced = false;
                MazeController.MazeUI.UILog("We are the owner of the treasure!"); // one time on treasure owner
                MazeController.SendCustomNetworkEvent(Owner, nameof(MazeController.OnTreasureGathered));
                treasure.SendCustomNetworkEvent(All, nameof(Treasure.Despawn));
            }
        }
    }
}
