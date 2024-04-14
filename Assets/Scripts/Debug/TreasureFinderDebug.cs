using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TreasureFinderDebug : UdonSharpBehaviour {
    [SerializeField] private Transform poolsOfTreasures;
    [SerializeField] private GameObject lightPrefab;

    private GameObject[] lightsArray;

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

    private void Update() {
        for (int i = 0; i < lightsArray.Length; i++) {
            GameObject light = lightsArray[i];
            if (light.transform.parent.gameObject.activeSelf)
                light.transform.position = poolsOfTreasures.GetChild(i).GetChild(0).position;
        }
    }
}
