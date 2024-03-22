using System.Collections.Generic;
using System.Diagnostics;
using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class MazeBuilder : UdonSharpBehaviour {
    public const float ROOMS_OFFSET = 4f;
    public const float ROOM_SCALE = 1; // 0,1875

    [Header("Rooms")]
    [SerializeField] private Transform mazeContainer;
    [SerializeField] private GameObject[] wallPrefabs;
    [SerializeField] private GameObject doorPrefab;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject[] cornerPrefabs;
    [SerializeField] private GameObject[] deadendPrefabs;

    public bool MazeReady { get; private set; }
    public int MazeSize { get; private set; }

    private MazeController controller;
    private int w, h, iterX, iterY, iter;
    private const int BUILD_COUNT = 3;
    private int buildLeft;

    public static void Spiral(int size, int step, out int x, out int y) {
        x = 0;
        y = 0;
        int dx = 0, dy = -1;
        int maxI = size * size;

        for (int i = 0; i < maxI && i < step; i++) {
            if ((x == y) || ((x < 0) && (x == -y)) || ((x > 0) && (x == 1 - y))) {
                int t = dx;
                dx = -dy;
                dy = t;
            }
            x += dx;
            y += dy;
        }

        x += size / 2;
        y += size / 2;
    }

    public void Init(MazeController controller) {
        this.controller = controller;
        MazeReady = false;
        controller.Utils.RemoveAllChildGameObjects(mazeContainer, 0.1f);
        iterX = -1;
        iterY = 0;
        iter = 0;
        buildLeft = 0;
        MazeSize = controller.GeneratorV2.Size;
        h = MazeSize;
        w = MazeSize;
        controller.UI.SetProgressValue(0f);
    }

    /// <summary>
    /// Run BuildRoomsBegin before. Iteration building. true = completed.
    /// </summary>
    public bool BuildRoomsIter() {
        Cell[][] cells = controller.GeneratorV2.GetCells;
        int[][] ids = controller.GeneratorV2.GetIds;

        // epic костыль
        if (cells == null) {
            //controller.debugText.text += "\n OH SH~! NO CELLS!!! : ";
            return MazeReady;
        }

        buildLeft = BUILD_COUNT;
        while (buildLeft > 0 && !MazeReady) {
            iter++;
            Spiral(MazeSize, iter - 1, out int x, out int y);
            //Debug.Log($"iter, x, y, MazeSize: {iter},, {x}, {y},, {MazeSize}");
            if (cells[x][y] != Cell.Wall && ids[x][y] != 0) {
                SpawnFloor(x, y, cells);
                buildLeft--;
            }
            if (iter >= MazeSize * MazeSize) {
                MazeReady = true;
                break;
            }
        }


        controller.UI.SetProgressValue((float) iter / (MazeSize * MazeSize));
        return MazeReady;
    }

    private void SpawnFloor(int x, int y, Cell[][] cells) {
        int[][] ids = controller.GeneratorV2.GetIds;
        Room[] rooms = controller.GeneratorV2.GetRooms;
        int floorId = ids[x][y];

        Cell current_cell = cells[x][y];
        int current_id = ids[x][y];

        if (current_cell != Cell.Hole) {
            GameObject obj_floor = Instantiate(floorPrefab, mazeContainer);
            Vector3 floorPos = obj_floor.transform.position;
            floorPos.x = (x - w / 2) * ROOMS_OFFSET;
            floorPos.z = (y - w / 2) * ROOMS_OFFSET;
            floorPos.y = 0;
            obj_floor.transform.position = floorPos;

            // colorize floor (demo)
            ColorizeFloor(obj_floor, floorId);

            obj_floor.transform.localScale = new Vector3(ROOM_SCALE, ROOM_SCALE, ROOM_SCALE);

            obj_floor.name = $"floor {floorId}";
        }



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
            string debug = $"out of bounds: x + dx, y + dy, dx, dy: {x + dx}, {y + dy}, {dx}, {dy}";
            if (y + dy >= 0 && y + dy < MazeSize && x + dx >= 0 && x + dx < MazeSize) {
                neighbor = cells[x + dx][y + dy];
                nearId = ids[x + dx][y + dy];
                debug = "in bounds";
            }


            GameObject obj = null;
            if (current_cell == Cell.DoorDeadEnd && neighbor == Cell.Wall) {
                int deadend_variant = ids[x][y] % deadendPrefabs.Length;
                obj = Instantiate(deadendPrefabs[deadend_variant], mazeContainer);
                obj.name = $"id={ids[x][y]}, type deadend, variant {deadend_variant}, {debug}";
            } else if (neighbor == Cell.Wall || nearId == 0) {
                // spawn wall
                int wall_variant = ids[x][y] % wallPrefabs.Length;
                obj = Instantiate(wallPrefabs[wall_variant], mazeContainer);
                obj.name = $"id={ids[x][y]}, type 1, {debug}";
            } else if (nearId > 0 && nearId != ids[x][y]) {
                // wall or door?
                if (
                    (cells[x][y] == Cell.DoorEnterance && neighbor == Cell.DoorExit)
                    ||
                    (cells[x][y] == Cell.DoorExit && neighbor == Cell.DoorEnterance)
                    ) {
                    obj = Instantiate(doorPrefab, mazeContainer);
                    obj.name = $"id={ids[x][y]}, type 2A, {debug}";
                } else {
                    int wall_variant = ids[x][y] % wallPrefabs.Length;
                    obj = Instantiate(wallPrefabs[wall_variant], mazeContainer);
                    obj.name = $"id={ids[x][y]}, type 2B, {debug}";
                }
            } else {
                // nothing to spawn - clear passage
                continue;
            }

            Vector3 pos = obj.transform.position;
            pos.x = (x - w / 2) * ROOMS_OFFSET;
            pos.z = (y - w / 2) * ROOMS_OFFSET;
            pos.y = 0;

            obj.transform.SetPositionAndRotation(pos, Quaternion.Euler(-90, rotation, 0));
            obj.transform.localScale = new Vector3(ROOM_SCALE, ROOM_SCALE, ROOM_SCALE);
        }

        // спавн уголков только поверх чистых проходов без дверей и только для пещер
        if (current_cell == Cell.Passage && rooms[current_id] == Room.Cave) {
            for (int direction1 = 1; direction1 <= 4; direction1++) {
                int dx1 = (direction1 == 1) ? 1 : (direction1 == 3) ? -1 : 0;
                int dy1 = (direction1 == 2) ? 1 : (direction1 == 4) ? -1 : 0;

                int rotation = 0;
                // не ругайте меня пж
                if (direction1 == 1) rotation = 90;
                if (direction1 == 2) rotation = 0;
                if (direction1 == 3) rotation = 270;
                if (direction1 == 4) rotation = 180;

                Cell near1_cell = Cell.Wall;
                int near1_id = 0;
                if (y + dy1 >= 0 && y + dy1 < MazeSize && x + dx1 >= 0 && x + dx1 < MazeSize) {
                    near1_cell = cells[x + dx1][y + dy1];
                    near1_id = ids[x + dx1][y + dy1];
                }

                int direction2 = GetClockwiseDirection(direction1);
                int dx2 = (direction2 == 1) ? 1 : (direction2 == 3) ? -1 : 0;
                int dy2 = (direction2 == 2) ? 1 : (direction2 == 4) ? -1 : 0;

                //UnityEngine.Debug.Log($"dir: {direction1} -> {direction2}");

                Cell near2_cell = Cell.Wall;
                int near2_id = 0;
                if (y + dy2 >= 0 && y + dy2 < MazeSize && x + dx2 >= 0 && x + dx2 < MazeSize) {
                    near2_cell = cells[x + dx2][y + dy2];
                    near2_id = ids[x + dx2][y + dy2];
                }

                if (current_id != near1_id && current_id != near2_id) {
                    int corner_variant = ids[x][y] % cornerPrefabs.Length;
                    GameObject obj333 = Instantiate(cornerPrefabs[corner_variant], mazeContainer);
                    obj333.name = $"corner={ids[x][y]}";

                    Vector3 pos = obj333.transform.position;
                    pos.x = (x - w / 2) * ROOMS_OFFSET;
                    pos.z = (y - w / 2) * ROOMS_OFFSET;
                    pos.y = 0;

                    obj333.transform.SetPositionAndRotation(pos, Quaternion.Euler(-90, rotation, 0));
                    obj333.transform.localScale = new Vector3(ROOM_SCALE, ROOM_SCALE, ROOM_SCALE);
                }
            }
        }
    }

    private int GetClockwiseDirection(int dir) {
        return (dir - 1 + 1) % 4 + 1;
    }

    private void ColorizeFloor(GameObject floor, int id) {
        var floorMesh = (MeshRenderer) floor.GetComponent(typeof(MeshRenderer));
        var matProp = new MaterialPropertyBlock();

        Color clr;
        switch (id % 8) {
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

        matProp.SetColor("_Color", clr);
        floorMesh.SetPropertyBlock(matProp);
    }
}

