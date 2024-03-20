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

    public static Color GetRandomColor() {
        Color clr;
        switch (Random.Range(0,8)) {
            case 0: clr = Color.yellow; break;
            case 1: clr = Color.red; break;
            case 2: clr = Color.magenta; break;
            case 3: clr = Color.grey; break;
            case 4: clr = Color.green; break;
            case 5: clr = Color.cyan; break;
            case 6: clr = Color.blue; break;
            case 7: clr = Color.black; break;
            default: clr = Color.white; break;
        }
        return clr;
    }

}
