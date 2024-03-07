using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class MazeBuilder : UdonSharpBehaviour {
    public const float ROOMS_OFFSET = 5f;
    public const float ROOM_SCALE = 0.75f / 4;

    [Header("Rooms")]
    [SerializeField] private Transform mazeContainer;
    [SerializeField] private GameObject baseRoomPrefab;
    [SerializeField] private GameObject corridorPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject doorPrefab;
    [SerializeField] private GameObject floorPrefab;

    public int MazeSize { get; private set; }

    public void BuildRooms(MazeController controller, RoomTypeEnum[][] rooms) {
        controller.Utils.RemoveAllChildGameObjects(mazeContainer);

        MazeSize = rooms.Length;
        int h = MazeSize;
        for (int y = 0; y < h; y++) {
            int w = rooms[y].Length;
            for (int x = 0; x < w; x++) {

                if (rooms[y][x] == RoomTypeEnum.Room || rooms[y][x] == RoomTypeEnum.Corridor) {

                    // spawn floor
                    GameObject obj_floor = Instantiate(floorPrefab, mazeContainer);
                    var pos2 = obj_floor.transform.position;
                    pos2.x = (x - w / 2) * ROOMS_OFFSET;
                    pos2.z = (y - w / 2) * ROOMS_OFFSET;
                    pos2.y = 0;
                    obj_floor.transform.position = pos2;

                    obj_floor.transform.localScale = new Vector3(ROOM_SCALE, ROOM_SCALE, ROOM_SCALE);


                    for (int direction = 1; direction <= 4; direction++) {
                        int dx = (direction == 1) ? 1 : (direction == 3) ? -1 : 0;
                        int dy = (direction == 2) ? 1 : (direction == 4) ? -1 : 0;

                        int rotation = 0;
                        // не ругайте меня пж
                        if (direction == 1) rotation = 90;
                        if (direction == 2) rotation = 0;
                        if (direction == 4) rotation = 180;
                        if (direction == 3) rotation = 270;

                        RoomTypeEnum neighbor = rooms[y + dy][x + dx];
                        GameObject obj = null;
                        if (neighbor == RoomTypeEnum.Nothing) {
                            // spawn wall
                            obj = Instantiate(wallPrefab, mazeContainer);
                        } else if (neighbor != rooms[y][x]) {
                            // spawn door
                            obj = Instantiate(doorPrefab, mazeContainer);
                        } else {
                            // nothing to spawn - clear passage
                            continue;
                        }

                        obj.name = direction.ToString();

                        var pos = obj.transform.position;
                        pos.x = (x - w / 2) * ROOMS_OFFSET;
                        pos.z = (y - w / 2) * ROOMS_OFFSET;
                        pos.y = 0;
                        obj.transform.position = pos;

                        obj.transform.rotation = Quaternion.Euler(-90, rotation, 0);
                        obj.transform.localScale = new Vector3(ROOM_SCALE, ROOM_SCALE, ROOM_SCALE);
                    }
                }

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
