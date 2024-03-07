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

    public bool MazeReady { get; private set; }
    public int MazeSize { get; private set; }

    private MazeController controller;
    private int w, h, iterX, iterY;
    private const int BUILD_COUNT = 3;
    private int buildLeft;

    public void Init(MazeController controller) {
        this.controller = controller;
    }

    public void BuildRooms(RoomTypeEnum[][] rooms) {
        MazeReady = false;
        controller.Utils.RemoveAllChildGameObjects(mazeContainer);

        MazeSize = rooms.Length;
        h = MazeSize;

        for (int y = 0; y < h; y++) {
            w = rooms[y].Length;

            for (int x = 0; x < w; x++) {
                if (rooms[y][x] == RoomTypeEnum.Room || rooms[y][x] == RoomTypeEnum.Corridor) {
                    SpawnFloor(x, y, rooms);
                }
            }
        }
        MazeReady = true;
    }

    public void BuildRoomsBegin(MazeController controller, RoomTypeEnum[][] rooms) {
        MazeReady = false;
        controller.Utils.RemoveAllChildGameObjects(mazeContainer);
        iterX = -1;
        iterY = 0;
        buildLeft = 0;
        MazeSize = rooms.Length;
        h = MazeSize;
        controller.UI.SetProgressValue(0f);
    }

    /// <summary>
    /// Run BuildRoomsBegin before. Iteration building. true = completed.
    /// </summary>
    public bool BuildRoomsIter(RoomTypeEnum[][] rooms) {
        while (buildLeft > 0 && !MazeReady) {
            // step
            iterX++;
            if (iterX >= rooms[iterY].Length) {
                iterX = 0;
                iterY++;
                if (iterY >= h) {
                    // end
                    MazeReady = true;
                    break;
                }
                w = rooms[iterY].Length;
            }

            // build
            RoomTypeEnum roomType = rooms[iterY][iterX];
            if (roomType == RoomTypeEnum.Room || roomType == RoomTypeEnum.Corridor) {
                SpawnFloor(iterX, iterY, rooms);
                buildLeft--;
            }
        }

        buildLeft = BUILD_COUNT;
        controller.UI.SetProgressValue((float) iterY / h);
        return MazeReady;
    }

    private void SpawnFloor(int x, int y, RoomTypeEnum[][] rooms) {
        GameObject obj_floor = Instantiate(floorPrefab, mazeContainer);
        Vector3 floorPos = obj_floor.transform.position;
        floorPos.x = (x - w / 2) * ROOMS_OFFSET;
        floorPos.z = (y - w / 2) * ROOMS_OFFSET;
        floorPos.y = 0;
        obj_floor.transform.position = floorPos;

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

            Vector3 pos = obj.transform.position;
            pos.x = (x - w / 2) * ROOMS_OFFSET;
            pos.z = (y - w / 2) * ROOMS_OFFSET;
            pos.y = 0;

            obj.transform.SetPositionAndRotation(pos, Quaternion.Euler(-90, rotation, 0));
            obj.transform.localScale = new Vector3(ROOM_SCALE, ROOM_SCALE, ROOM_SCALE);
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
