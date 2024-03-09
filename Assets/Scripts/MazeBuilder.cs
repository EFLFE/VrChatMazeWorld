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

    public void BuildRoomsBegin() {
        MazeReady = false;
        controller.Utils.RemoveAllChildGameObjects(mazeContainer, 0.1f);
        iterX = -1;
        iterY = 0;
        buildLeft = 0;
        MazeSize = controller.GeneratorV2.Size;
        h = MazeSize;
        controller.UI.SetProgressValue(0f);
    }

    /// <summary>
    /// Run BuildRoomsBegin before. Iteration building. true = completed.
    /// </summary>
    public bool BuildRoomsIter() {
        Cell[][] cells = controller.GeneratorV2.GetCells;
        //int[][] ids = controller.GeneratorV2.GetIds;

        while (buildLeft > 0 && !MazeReady) {
            // step
            iterX++;
            if (iterX >= cells[iterY].Length) {
                iterX = 0;
                iterY++;
                if (iterY >= h) {
                    // end
                    MazeReady = true;
                    break;
                }
                w = cells[iterY].Length;
            }

            // build
            Cell roomType = cells[iterX][iterY];
            if (roomType != Cell.Wall) {
                SpawnFloor(iterX, iterY, cells);
                buildLeft--;
            }
        }

        buildLeft = BUILD_COUNT;
        controller.UI.SetProgressValue((float) iterY / h);
        return MazeReady;
    }

    private void SpawnFloor(int x, int y, Cell[][] cells) {
        GameObject obj_floor = Instantiate(floorPrefab, mazeContainer);
        Vector3 floorPos = obj_floor.transform.position;
        floorPos.x = (x - w / 2) * ROOMS_OFFSET;
        floorPos.z = (y - w / 2) * ROOMS_OFFSET;
        floorPos.y = 0;
        obj_floor.transform.position = floorPos;

        obj_floor.transform.localScale = new Vector3(ROOM_SCALE, ROOM_SCALE, ROOM_SCALE);
        int[][] ids = controller.GeneratorV2.GetIds;

        obj_floor.name = $"floor {ids[x][y]}";

        for (int direction = 1; direction <= 4; direction++) {
            int dx = (direction == 1) ? 1 : (direction == 3) ? -1 : 0;
            int dy = (direction == 2) ? 1 : (direction == 4) ? -1 : 0;

            int rotation = 0;
            // не ругайте меня пж
            if (direction == 1) rotation = 90;
            if (direction == 2) rotation = 0;
            if (direction == 3) rotation = 270;
            if (direction == 4) rotation = 180;

            Cell neighbor = Cell.Wall;
            int nearId = 0;
            if (y + dy >= 0 && y + dy < MazeSize && x + dx > 0 && x + dx < MazeSize) {
                neighbor = cells[x + dx][y + dy];
                nearId = ids[x + dx][y + dy];
            }

            GameObject obj = null;
            if (neighbor == Cell.Wall) {
                // spawn wall
                obj = Instantiate(wallPrefab, mazeContainer);
            } else if (nearId > 0 && nearId != ids[x][y]) {
                // wall or door?
                if ((cells[x][y] == Cell.DoorEnterance || cells[x][y] == Cell.DoorExit)
                    && (neighbor == Cell.DoorEnterance || neighbor == Cell.DoorExit)) {
                    obj = Instantiate(doorPrefab, mazeContainer);
                } else {
                    obj = Instantiate(wallPrefab, mazeContainer);
                }
            } else {
                // nothing to spawn - clear passage
                continue;
            }

            obj.name = $"id={ids[x][y]}";

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
