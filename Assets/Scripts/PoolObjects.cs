using UdonSharp;
using UnityEngine;
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
        poolContainer = gameObject.transform;
        int count = poolContainer.childCount;
        poolItems = new MazeObject[count];
        states = new bool[count];
        for (int i = 0; i < count; i++) {
            poolItems[i] = poolContainer.GetChild(i).GetComponent<MazeObject>();
            poolItems[i].Init(controller, i);
            states[i] = false;
        }
    }


    public override void OnDeserialization() {
        base.OnDeserialization();
        MazeController.MazeUI.UILog($"PoolObjects OnDeserialization:");
        string log = "Active ids: ";
        for (int i = 0; i < poolContainer.childCount; i++) {

            var obj = poolItems[i];
            if (obj.gameObject.activeSelf != states[i]) {
                if (states[i]) {
                    // предположение: телепортация в -10 перед активацией уберет случайные коллайды
                    // obj.transform.SetPositionAndRotation(new Vector3(0, -10, 0), Quaternion.Euler(0, 0, 0));
                    // не сработало
                }
                MazeController.MazeUI.UILog($"- {i} - " + (states[i] ? "activate" : "deactivate"));
                var vrc_sync = obj.gameObject.GetComponent<VRCObjectSync>();
                if (vrc_sync != null) {
                    vrc_sync.FlagDiscontinuity();
                }
                obj.gameObject.SetActive(states[i]);
                // предположение: после активации (глобальная позиция пикапа) засинхронится через VRC_Sync
            }

            if (states[i]) {
                log += $"{i}, ";
            }
        }
        MazeController.MazeUI.UILog(log);
    }


    public bool TryTake(out MazeObject obj, Vector3 position, Quaternion rotation) {
        obj = null;
        for (int i = 0; i < poolItems.Length; i++) {
            MazeObject item = poolItems[i];
            if (!item.gameObject.activeSelf) {
                obj = item;

                obj.gameObject.SetActive(true);
                var vrc_sync = obj.gameObject.GetComponent<VRCObjectSync>();
                if (vrc_sync != null) {
                    vrc_sync.FlagDiscontinuity();
                }
                obj.transform.SetPositionAndRotation(position, rotation);

                states[i] = true;
                RequestSerialization();
                break;
            }
        }
        return obj != null;
    }


    // вызывать строго только мастером
    public void Return(MazeObject obj) {
        if (!obj.gameObject.activeSelf) return;

        // вернуть предмет во владение мастера
        Networking.SetOwner(Networking.LocalPlayer, obj.gameObject);

        states[obj.pool_id] = false;
        var vrc_sync = obj.gameObject.GetComponent<VRCObjectSync>();
        if (vrc_sync != null) {
            vrc_sync.FlagDiscontinuity();
        }
        obj.transform.SetPositionAndRotation(new Vector3(0, -10, 0), Quaternion.Euler(0, 0, 0));
        obj.gameObject.SetActive(false);
        RequestSerialization();
    }

    public void ReturnAll() {
        for (int i = 0; i < poolItems.Length; i++) {
            Return(poolItems[i]);
        }
    }
}
