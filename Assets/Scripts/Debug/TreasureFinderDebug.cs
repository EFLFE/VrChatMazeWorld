using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class TreasureFinderDebug : UdonSharpBehaviour {
    [SerializeField] private Transform poolsOfTreasures;
    [SerializeField] private GameObject lightPrefab;

    private GameObject[] lightsArray;
    private bool state = false;

    public override void Interact() {
        if (state) Hide(); else Show();
        state = !state;
    }

    private void Start() {
        lightsArray = new GameObject[0];
    }

    public void Show() {
        if (lightsArray.Length != 0)
            return;

        int count = poolsOfTreasures.childCount;
        lightsArray = new GameObject[count];

        for (int i = 0; i < count; i++) {
            Transform treasureTran = poolsOfTreasures.GetChild(i);
            GameObject light = Instantiate(lightPrefab, treasureTran);
            lightsArray[i] = light;
            light.SetActive(true);
        }
    }

    public void Hide() {
        for (int i = 0; i < lightsArray.Length; i++) {
            GameObject.Destroy(lightsArray[i]);
        }
        lightsArray = new GameObject[0];
    }

    private void Update() {
        for (int i = 0; i < lightsArray.Length; i++) {
            GameObject light = lightsArray[i];
            lightsArray[i].gameObject.transform.rotation = Quaternion.identity;
        }
    }
}
