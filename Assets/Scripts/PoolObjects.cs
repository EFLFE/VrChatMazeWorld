using UdonSharp;
using UnityEngine;
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
            poolItems[i].gameObject.SetActive(states[i]);
            if (states[i]) {
                log += $"{i}, ";
            }
        }
        MazeController.MazeUI.UILog(log);
    }

    public MazeObject Take() {
        TryTake(out MazeObject obj);
        return obj;
    }

    public bool TryTake(out MazeObject obj) {
        obj = null;
        for (int i = 0; i < poolItems.Length; i++) {
            MazeObject item = poolItems[i];
            if (!item.gameObject.activeSelf) {
                if (!Networking.IsOwner(item.gameObject)) {
                    Networking.SetOwner(Networking.LocalPlayer, item.gameObject);
                }
                obj = item;
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
        obj.gameObject.SetActive(false);
        RequestSerialization();
    }

    public void ReturnAll() {
        for (int i = 0; i < poolItems.Length; i++) {
            Return(poolItems[i]);
        }
    }
}
