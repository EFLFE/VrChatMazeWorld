using UdonSharp;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using VRC.SDKBase;
using static VRC.Udon.Common.Interfaces.NetworkEventTarget;

public class CentralZone : UdonSharpBehaviour {
    [SerializeField] public MazeController MazeController;
    [SerializeField] private PoolObjects chestPool;
    [SerializeField] private AudioSource noice;

    private void OnTriggerEnter(Collider collider) {
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

        var treasure = collider.gameObject.GetComponent<Treasure>();
        if (treasure != null) {
            MazeController.MazeUI.UILog($"Treasure found in CentralZone: {collider.gameObject.name}, id = {treasure.PoolID}");

            // дропнуть предмет из руки текущего владельца
            treasure.Drop();
            noice.Play();

            if (Networking.IsMaster) {
                MazeController.MazeUI.UILog($"We are Master: return treasure, call OnTreasureGathered()");

                //treasure.SendCustomNetworkEvent(All, nameof(Treasure.Despawn));
                MazeController.GetChestPool.Return(treasure);

                //MazeController.SendCustomNetworkEvent(Owner, nameof(MazeController.OnTreasureGathered));
                MazeController.OnTreasureGathered();
            }
        }
    }
}
