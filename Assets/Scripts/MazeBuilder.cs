using UdonSharp;
using UnityEngine;

public class MazeBuilder : UdonSharpBehaviour {
    [SerializeField] private Transform mazeContainer;
    [Header("Rooms")]
    [SerializeField] private float roomsOffset = 20f;
    [SerializeField] private GameObject baseRoomPrefab;
    [SerializeField] private GameObject corridorPrefab;

    public void BuildRooms(RoomTypeEnum[][] rooms) {
        int h = rooms.Length;
        for (int y = 0; y < h; y++) {
            int w = rooms[y].Length;
            for (int x = 0; x < w; x++) {
                GameObject roomObj = CreateRoom(rooms[y][x]);
                if (roomObj == null)
                    continue;

                // open near doors
                //if (y > 0 && rooms[y - 1][x] != RoomTypeEnum.Nothing)
                //    roomObj.OpenBottom();
                //if (y < h - 1 && rooms[y + 1][x] != RoomTypeEnum.Nothing)
                //    roomObj.OpenTop();
                //if (x > 0 && rooms[y][x - 1] != RoomTypeEnum.Nothing)
                //    roomObj.OpenLeft();
                //if (x < w - 1 && rooms[y][x + 1] != RoomTypeEnum.Nothing)
                //    roomObj.OpenRight();

                var pos = roomObj.transform.position;
                //pos.x -= w / 2;
                //pos.z -= w / 2;
                pos.x = (x - w / 2) * roomsOffset;
                pos.z = (y - w / 2) * roomsOffset;
                pos.y = 0;
                roomObj.transform.position = pos;
            }
        }
    }

    private GameObject CreateRoom(RoomTypeEnum roomType) {
        GameObject prefab = GetRoomTypePrefab(roomType);
        if (prefab == null)
            return null;

        GameObject obj = Instantiate(prefab, mazeContainer);
        return obj;
    }

    private GameObject GetRoomTypePrefab(RoomTypeEnum roomType) {
        switch (roomType) {
            case RoomTypeEnum.Nothing: return null;
            case RoomTypeEnum.Room: return baseRoomPrefab;
            case RoomTypeEnum.Corridor: return corridorPrefab;
            default:
                Debug.LogError($"RoomType '{roomType}' not defined!");
                return null;
        }
    }

}
