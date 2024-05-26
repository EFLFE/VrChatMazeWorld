using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ListManager : UdonSharpBehaviour {
    public const int MIN_CAPACITY_EXPAND_SIZE = 64;

    [SerializeField] private Transform container;
    [SerializeField] private GameObject floatListObject;
    [SerializeField] private GameObject intListObject;
    [SerializeField] private GameObject vector2IntListObject;

    public IntList GetIntList(int initialCapacity = MIN_CAPACITY_EXPAND_SIZE) {
        GameObject obj = Instantiate(intListObject, container);
        var script = obj.GetComponent<IntList>();
        script.Init(initialCapacity);
        return script;
    }

    public FloatList GetFloatList(int initialCapacity = MIN_CAPACITY_EXPAND_SIZE) {
        GameObject obj = Instantiate(floatListObject, container);
        var script = obj.GetComponent<FloatList>();
        script.Init(initialCapacity);
        return script;
    }

    public Vector2IntList GetVector2IntList(int initialCapacity = MIN_CAPACITY_EXPAND_SIZE) {
        GameObject obj = Instantiate(vector2IntListObject, container);
        var script = obj.GetComponent<Vector2IntList>();
        script.Init(initialCapacity);
        return script;
    }
}
