using UdonSharp;
using UnityEngine;
using UnityEngine.UIElements;
using VRC;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class PoolObjects : UdonSharpBehaviour {
    public MazeController MazeController;
    private Transform poolContainer;
    private MazeObject[] poolItems;
    [UdonSynced] bool[] states;

    public bool ManageOwners;

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
            //poolItems[i].gameObject.SetActive(states[i]); // temp test
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
                //if (!Networking.IsOwner(item.gameObject)) {
                //    Networking.SetOwner(Networking.LocalPlayer, item.gameObject);
                //}
                obj = item;
                obj.transform.SetPositionAndRotation(position, rotation);
                for (int j = 0; j < obj.transform.childCount; j++) {
                    obj.transform.GetChild(j).SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 0, 0));
                }

                obj.gameObject.SetActive(true);
                states[i] = true;
                RequestSerialization();
                break;
            }
        }
        return obj != null;
    }


    public void Return(MazeObject obj) {
        states[obj.pool_id] = false;
        obj.transform.SetPositionAndRotation(new Vector3(0, -100, 0), Quaternion.Euler(0, 0, 0));
        for (int j = 0; j < obj.transform.childCount; j++) {
            obj.transform.GetChild(j).SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 0, 0));
        }
        obj.gameObject.SetActive(false);
        RequestSerialization();
    }

    public void ReturnAll() {
        for (int i = 0; i < poolItems.Length; i++) {
            Return(poolItems[i]);
        }
    }
}
