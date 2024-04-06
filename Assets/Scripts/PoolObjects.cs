using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class PoolObjects : UdonSharpBehaviour {
    private Transform poolContainer;
    private MazeObject[] poolItems;

    public bool ManageOwners;

    public int MaxCount => poolItems.Length;

    public void Init(MazeController controller) {
        poolContainer = gameObject.transform;
        int count = poolContainer.childCount;
        poolItems = new MazeObject[count];
        for (int i = 0; i < count; i++) {
            poolItems[i] = poolContainer.GetChild(i).GetComponent<MazeObject>();
            poolItems[i].Init(controller);
        }
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
                if (ManageOwners && !Networking.IsOwner(item.gameObject))
                    Networking.SetOwner(Networking.LocalPlayer, item.gameObject);

                obj = item;
                obj.gameObject.SetActive(true);
                break;
            }
        }
        return obj != null;
    }

    public void Return(MazeObject obj) {
        if (!obj.gameObject.activeSelf)
            return;
        if (ManageOwners && !Networking.IsOwner(obj.gameObject))
            return;
        obj.gameObject.SetActive(false);
        obj.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public void ReturnAll() {
        for (int i = 0; i < poolItems.Length; i++) {
            Return(poolItems[i]);
        }
    }
}
