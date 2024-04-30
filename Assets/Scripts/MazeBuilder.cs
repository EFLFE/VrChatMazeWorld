using System.Collections.Generic;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

internal enum BuildIterType {
    BaseLevel,
    Chest,
    None,
}

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class MazeBuilder : UdonSharpBehaviour {
    public const float ROOMS_OFFSET = 4f;
    public const float ROOM_SCALE = 1;

    [Header("Stuff")]
    [SerializeField] private PoolObjects chestPool;
    [SerializeField] private GameObject universalPrefab;
    [SerializeField] private Transform mazeContainer;
    [SerializeField] private Transform mazeCeilingContainer;
    [Header("Rooms")]
    [SerializeField] private Mesh[] walls;
    [SerializeField] private Mesh[] doors;
    [SerializeField] private Mesh[] floors;
    [SerializeField] private Mesh[] deadends;
    [SerializeField] private Mesh[] ceilings;

    public bool MazeReady { get; private set; }

    private MazeController controller;
    private int iter;
    private const int BUILD_COUNT = 4;
    private int buildLeft;

    private BuildIterType buildIterType;

    public void Init(MazeController controller) {
        this.controller = controller;
        MazeReady = false;
        //controller.Utils.RemoveAllChildGameObjects(mazeContainer, 0.1f);
        for (int i = mazeContainer.childCount - 1; i >= 0; --i) {
            GameObject child = mazeContainer.GetChild(i).gameObject;
            GameObject.Destroy(child, 0.1f);
        }
        chestPool.ReturnAll();
        iter = 0;
        buildLeft = 0;
        controller.MazeUI.SetProgressValue(0f);
        buildIterType = default(BuildIterType);
    }

    /// <summary>
    /// Run BuildRoomsBegin before. Iteration building. true = completed.
    /// </summary>
    public bool BuildRoomsIter() {
        if (MazeReady)
            return true;

        MazeGenerator maze = controller.MazeGenerator;

        // epic костыль
        if (maze.Cells == null) {
            controller.MazeUI.UILog("BuildRoomsIter: OH SH~! NO CELLS!!!");
            return MazeReady;
        }

        if (iter == 0) {
            controller.Utils.ResetSpiral(maze.Size);
        }

        buildLeft = BUILD_COUNT;
        while (buildLeft > 0 && buildIterType != BuildIterType.None) {
            if (buildIterType == BuildIterType.BaseLevel) {
                iter++;

                if (iter < maze.Size * maze.Size) {
                    controller.Utils.NextSpiral(out int x, out int y);
                    SpawnCell(x, y);
                    controller.MazeUI.SetProgressValue((float) iter / (maze.Size * maze.Size));
                } else {
                    buildIterType = BuildIterType.Chest;
                    iter = 0;
                }
            } else if (buildIterType == BuildIterType.Chest) {
                if (iter < maze.ChestsAmount) {
                    SpawnChest(maze.ChestsX[iter], maze.ChestsY[iter]);
                    controller.MazeUI.SetProgressValue((float) iter / maze.ChestsAmount);
                } else {
                    buildIterType = BuildIterType.None;
                    iter = 0;
                }

                iter++;
            }
        }

        MazeReady = buildIterType == BuildIterType.None;
        return MazeReady;
    }

    private Cell GetCell(int x, int y) {
        if (x < 0 || y < 0 || x >= controller.MazeGenerator.Size || y >= controller.MazeGenerator.Size) {
            return Cell.Wall;
        } else {
            return controller.MazeGenerator.Cells[x][y];
        }
    }

    private bool SpawnCell(int x, int y) {
        MazeGenerator maze = controller.MazeGenerator;
        Cell[][] cells = maze.Cells;
        int[][] ids = maze.Ids;
        Room[] rooms = maze.Rooms;

        Cell current_cell = cells[x][y];
        int current_id = ids[x][y];
        Room current_room = rooms[current_id];

        if (current_cell != Cell.Hole) {
            bool need_to_spawn_floor = false;
            if (current_id != 0) {
                need_to_spawn_floor = true;
            }

            if (!need_to_spawn_floor) {
                for (int dir = 0; dir < 4; dir++) {
                    controller.MazeGenerator.GetDirectionsVector(dir, out int dx, out int dy);
                    if (GetCell(x + dx, y + dy) == Cell.Hole) {
                        need_to_spawn_floor = true;
                        break;
                    }
                }
            }

            if (need_to_spawn_floor) {
                GameObject obj_floor = Spawn(
                    floors[current_id == 1 ? 0 : GetRandomIndex(floors.Length, 0.9f)],
                    x, y, 0,
                    $"floor {current_id}, cell type: {current_cell}",
                    true
                );
                ColorizeFloor(obj_floor, current_id);
            }
        }

        // ------------- next, spawn walls and corners
        if (current_id == 0) {
            //return false; // spawn floor only
        }
        // ------------- next, spawn walls and corners


        // spawn ceiling
        if (current_id != 1) {
            // TODO make admin button to remove all ceilings
            int rotation = controller.MazeGenerator.RandomInclusive(0, 3) * 90;
            GameObject GO = Spawn(
                ceilings[GetRandomIndex(ceilings.Length, 0.75f)],
                x, y, rotation,
                "ceiling",
                true,
                mazeCeilingContainer);
            // поворот для потолочков <(^_^)> =(^_^)=
            GO.transform.SetPositionAndRotation(GO.transform.position + new Vector3(0, 4, 0), Quaternion.Euler(180, 0, 0));
        }

        // spawn 2, 3 or 4 walls
        for (int direction = 1; direction <= 4; direction++) {

            if (direction == 3 && x > 0) continue;
            if (direction == 4 && y > 0) continue;

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
            if (y + dy >= 0 && y + dy < maze.Size && x + dx >= 0 && x + dx < maze.Size) {
                neighbor = cells[x + dx][y + dy];
                nearId = ids[x + dx][y + dy];
                debug = "in bounds";
            }

            if (current_id == 0 && nearId == 0) continue;

            GameObject obj;
            if (current_cell == Cell.DoorDeadEnd && neighbor == Cell.Wall) {
                int deadend_variant = ids[x][y] % deadends.Length;
                obj = Spawn(deadends[deadend_variant], x, y, rotation);
                obj.name = $"id={ids[x][y]}, type deadend, variant {deadend_variant}, {debug}";
            } else if (neighbor == Cell.Wall || nearId == 0) {
                // spawn wall
                obj = Spawn(walls[GetRandomIndex(walls.Length, 0.5f)], x, y, rotation);
                obj.name = $"id={ids[x][y]}, type 1, {debug}";
            } else if (nearId > 0 && nearId != ids[x][y]) {
                // wall or door?
                if (
                    (cells[x][y] == Cell.DoorEnterance && neighbor == Cell.DoorExit)
                    ||
                    (cells[x][y] == Cell.DoorExit && neighbor == Cell.DoorEnterance)
                    ) {
                    obj = Spawn(doors[controller.MazeGenerator.RandomInclusive(0, doors.Length - 1)], x, y, rotation,
                        "door", false, null, false);
                    obj.name = $"id={ids[x][y]}, type 2A, {debug}";
                } else {
                    obj = Spawn(walls[GetRandomIndex(walls.Length, 0.5f)], x, y, rotation);
                    obj.name = $"id={ids[x][y]}, type 2B, {debug}";
                }
            } else {
                // nothing to spawn - clear passage
                continue;
            }
        }

        return true;
    }

    private int GetRandomIndex(int length, float probability_of_zero_index = 0.5f) {
        if (controller.MazeGenerator.RandomInclusive(0, 100) / 100.0f < probability_of_zero_index) return 0;
        return controller.MazeGenerator.RandomInclusive(1, length - 1);
    }

    private void ColorizeFloor(GameObject floor, int id) {
        var floorMesh = (MeshRenderer) floor.GetComponent(typeof(MeshRenderer));
        var matProp = new MaterialPropertyBlock();
        Color clr = controller.Utils.GetFloorColor(id);
        matProp.SetColor("_Color", clr);
        floorMesh.SetPropertyBlock(matProp);
    }

    private GameObject Spawn(
            Mesh mesh, int x, int y, int rotation, string name = "", bool do_not_offset = false, Transform cutstomContainer = null,
            bool useBoxCollider = true) {

        GameObject GO = Instantiate(universalPrefab, cutstomContainer != null ? cutstomContainer : mazeContainer);
        GO.GetComponent<MeshFilter>().sharedMesh = mesh;

        var boxCollider = GO.GetComponent<BoxCollider>();
        var meshCollider = GO.GetComponent<MeshCollider>();
        boxCollider.enabled = useBoxCollider;
        meshCollider.enabled = !useBoxCollider;

        if (useBoxCollider) {
            boxCollider.center = mesh.bounds.center;
            boxCollider.size = mesh.bounds.size;
        } else {
            meshCollider.sharedMesh = mesh;
        }

        Vector3 position = GO.transform.position;
        position.x = (x - controller.MazeGenerator.Size / 2) * ROOMS_OFFSET;
        position.z = (y - controller.MazeGenerator.Size / 2) * ROOMS_OFFSET;
        position.y = 0;

        if (!do_not_offset) {
            if (rotation == 0) position.z += 2;
            if (rotation == 90) position.x += 2;
            if (rotation == 180) position.z -= 2;
            if (rotation == 270) position.x -= 2;
        }

        GO.transform.SetPositionAndRotation(position, Quaternion.Euler(0, rotation, 0));
        GO.transform.localScale = new Vector3(ROOM_SCALE, ROOM_SCALE, ROOM_SCALE);
        GO.name = name;
        buildLeft--;
        return GO;
    }

    private MazeObject SpawnChest(int x, int y) {
        // lets spawn treasures only on master
        if (!Networking.IsOwner(gameObject)) return null;

        Vector3 position;
        position.x = (x - controller.MazeGenerator.Size / 2) * ROOMS_OFFSET;
        position.z = (y - controller.MazeGenerator.Size / 2) * ROOMS_OFFSET;
        position.y = 1;

        if (!chestPool.TryTake(out MazeObject GO, position, Quaternion.Euler(0, 0, 0))) {
            controller.MazeUI.UILog("No more chest in pool!");
            return null;
        }

        var treasure = GO.GetComponent<Treasure>();
        controller.MazeUI.UILog(
            $"Spawn {GO.name}, id = {treasure.pool_id} " +
            $"\n- x, y = {x}, {y} => {position.x}, {position.z}"
        );

        buildLeft = 0;
        return GO;
    }
}
