using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class Testing : UdonSharpBehaviour {
    [SerializeField] private Transform poolContainer;

    public bool ManageOwners;
    private GameObject[] poolItems;

    public int MaxCount => poolItems.Length;

    private void Start() {
        int count = poolContainer.childCount;
        poolItems = new GameObject[count];
        for (int i = 0; i < count; i++) {
            poolItems[i] = poolContainer.GetChild(i).gameObject;
        }
    }

    public GameObject Take() {
        TryTake(out GameObject obj);
        return obj;
    }

    public bool TryTake(out GameObject obj) {
        obj = null;
        for (int i = 0; i < poolItems.Length; i++) {
            GameObject item = poolItems[i];
            if (!item.activeSelf) {
                if (ManageOwners && !Networking.IsOwner(item))
                    Networking.SetOwner(Networking.LocalPlayer, item);

                obj = item;
                obj.SetActive(true);
                break;
            }
        }
        return obj != null;
    }

    public void Return(GameObject obj) {
        if (ManageOwners && !Networking.IsOwner(obj))
            return;
        obj.SetActive(false);
    }
}
