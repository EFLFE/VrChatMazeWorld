using UdonSharp;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.ClientSim;
using VRC.SDK3.Components;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class PoolObjects : UdonSharpBehaviour {
    public MazeController MazeController;
    private Transform poolContainer;
    private MazeObject[] poolItems;
    [UdonSynced] bool[] states;

    public int MaxCount => poolItems.Length;

    public void Init(MazeController controller) {
        MazeController = controller;
        poolContainer = gameObject.transform;
        int count = poolContainer.childCount;
        poolItems = new MazeObject[count];
        states = new bool[count];
        for (int i = 0; i < count; i++) {
            poolItems[i] = poolContainer.GetChild(i).GetComponent<MazeObject>();
            states[i] = false;
        }
    }

    public override void OnDeserialization() {
        base.OnDeserialization();
        MazeController.MazeUI.UILog($"PoolObjects OnDeserialization:");
        for (int i = 0; i < poolContainer.childCount; i++) {

            var obj = poolItems[i];
            if (obj.gameObject.activeSelf != states[i]) {
                MazeController.MazeUI.UILog(
                    $"- {i} - "
                    + (states[i] ? "activate" : "deactivate")
                    + ", owner player: "
                    + Networking.GetOwner(obj.gameObject).playerId.ToString()
                    + " " + Networking.GetOwner(obj.gameObject).displayName
                );

                var vrc_sync = obj.gameObject.GetComponent<VRCObjectSync>();
                if (vrc_sync != null) vrc_sync.FlagDiscontinuity();

                obj.gameObject.SetActive(states[i]);

                // предположение: после активации (глобальная позиция пикапа) засинхронится через VRC_Sync
                // оно синхронится, но на лейт джойнерах с задержкой
            }
        }
    }

    private void TeleportObject(MazeObject obj, Vector3 position, Quaternion rotation) {
        obj.transform.SetPositionAndRotation(position, rotation);
        VRCObjectSync vrc_sync = obj.gameObject.GetComponent<VRCObjectSync>();
        if (vrc_sync != null) {
            vrc_sync.FlagDiscontinuity();
            vrc_sync.TeleportTo(obj.transform);
        }
    }

    public void ManualUpdate() {
        for (int i = 0; i < poolItems.Length; i++) {
            MazeObject item = poolItems[i];
            if (item.gameObject.activeSelf) {
                item.ManualUpdate();
            }
        }
    }

    public bool TryTake(out MazeObject obj, Vector3 position, Quaternion rotation) {
        obj = null;
        for (int i = 0; i < poolItems.Length; i++) {
            MazeObject item = poolItems[i];
            if (!item.gameObject.activeSelf) {
                obj = item;

                obj.gameObject.SetActive(true);
                TeleportObject(obj, position, rotation);

                states[i] = true;
                obj.Init(MazeController, i);
                RequestSerialization();
                break;
            }
        }
        return obj != null;
    }

    // вызывается только у мастера
    public void Return(MazeObject obj) {
        if (!Networking.IsMaster) return;
        if (!obj.gameObject.activeSelf) return;

        string log = $"Return pool object, id: {obj.pool_id}, owner: ";
        log += Networking.GetOwner(obj.gameObject).playerId.ToString() + " " + Networking.GetOwner(obj.gameObject).displayName;
        // вернуть предмет во владение мастера
        Networking.SetOwner(Networking.GetOwner(MazeController.gameObject), obj.gameObject);
        log += " -> ";
        log += Networking.GetOwner(obj.gameObject).playerId.ToString() + " " + Networking.GetOwner(obj.gameObject).displayName;
        MazeController.MazeUI.UILog(log);

        states[obj.pool_id] = false;

        TeleportObject(obj, new Vector3(0, -10, 0), Quaternion.Euler(0, 0, 0));
        obj.gameObject.SetActive(false);

        RequestSerialization();
    }

    // вызывается только у мастера
    public void ReturnAll() {
        if (!Networking.IsMaster) return;
        for (int i = 0; i < poolItems.Length; i++) {
            Return(poolItems[i]);
        }
    }
}
