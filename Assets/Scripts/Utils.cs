using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Utils : UdonSharpBehaviour {
    /// <summary> Создать зазубренный массив массивов ([][]). </summary>
    /// <typeparam name="T"> Тип пассива (без скобок). </typeparam>
    /// <param name="array"> Массив. </param>
    /// <param name="height"> Размер первой ячейки массива. </param>
    /// <param name="width"> Размер второй ячейки массива. </param>
    public void CreateJaggedArrayOfarrays(out int[][] array, int height, int width) {
        array = new int[height][];
        for (int a = 0; a < height; a++) {
            array[a] = new int[width];
        }
    }

    public void CreateJaggedArrayOfarrays(out RoomTypeEnum[][] array, int height, int width) {
        array = new RoomTypeEnum[height][];
        for (int a = 0; a < height; a++) {
            array[a] = new RoomTypeEnum[width];
        }
    }

    public void RemoveAllChildGameObjects(Transform transform, float time = 0f) {
        for (int i = transform.childCount - 1; i >= 0; --i) {
            GameObject child = transform.GetChild(i).gameObject;
            GameObject.Destroy(child, time);
        }
    }

}
