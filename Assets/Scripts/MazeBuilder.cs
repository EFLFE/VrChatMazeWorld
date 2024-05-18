﻿using System.Collections.Generic;
using UdonSharp;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.XR;
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
    private const int walls_simple_count = 3;
    [SerializeField] private Mesh[] walls;
    [SerializeField] private Mesh[] doors;
    [SerializeField] private Mesh[] floors;
    [SerializeField] private Mesh[] deadends;
    [SerializeField] private Mesh[] ceilings;
    [SerializeField] private Mesh[] corners;
    [SerializeField] private Mesh[] stairs;
    [Header("Decor")]
    [SerializeField] private Mesh[] deco1;
    [SerializeField] private Mesh[] deco2;
    [SerializeField] private Mesh[] deco3;

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
        for (int i = mazeCeilingContainer.childCount - 1; i >= 0; --i) {
            GameObject child = mazeCeilingContainer.GetChild(i).gameObject;
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

                if (iter <= maze.Size * maze.Size) {
                    controller.Utils.NextSpiral(out int x, out int y);
                    for (int z = 0; z < maze.Height; z++) {
                        SpawnCell(x, y, z);
                    }
                    controller.MazeUI.SetProgressValue((float) iter / (maze.Size * maze.Size));
                } else {
                    buildIterType = BuildIterType.Chest;
                    iter = 0;
                }
            } else if (buildIterType == BuildIterType.Chest) {
                if (iter < maze.ChestsAmount) {
                    SpawnChest(maze.ChestsX[iter], maze.ChestsY[iter], maze.ChestsZ[iter]);
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

    private bool SpawnCell(int x, int y, int z) {
        //controller.MazeUI.UILog($"SpawnCell XY = {x}, {y}");

        MazeGenerator maze = controller.MazeGenerator;
        Cell[][][] cells = maze.Cells;
        int[][][] ids = maze.Ids;
        Room[] rooms = maze.Rooms;

        Cell current_cell = cells[x][y][z];
        int current_id = ids[x][y][z];
        Room current_room = rooms[current_id];

        bool must_spawn_floor = (current_id > 0);
        bool must_spawn_stair = false;
        if (z > 0 && ids[x][y][z - 1] == ids[x][y][z]) {
            must_spawn_floor = false;
            if (current_room == Room.Stairs) {
                must_spawn_stair = true;
            }
        }

        if (must_spawn_floor) {

            bool can_spawn_decorative_floor = false;
            if (z <= 0 || ids[x][y][z - 1] == 0) {
                can_spawn_decorative_floor = true;
            }
            if (current_id == 1) {
                can_spawn_decorative_floor = false;
            }
            int floor_variant = can_spawn_decorative_floor ? GetRandomIndex(floors.Length, 0.75f) : 0;

            GameObject obj_floor = Spawn(
                floors[floor_variant],
                x, y, z, 0,
                $"floor {current_id}, cell type: {current_cell}, xyz: {x} {y} {z}"
            );

            ColorizeFloor(obj_floor, current_id);
        }

        if (must_spawn_stair) {

            int rotation = -1;
            for (int dir = 0; dir <= 3; dir++) {
                maze.GetDirectionsVector(dir, out int dx, out int dy);
                var diff_cell1 = maze.GetCell(x + dx, y + dy, z);
                var diff_cell2 = maze.GetCell(x - dx, y - dy, z - 1);
                if (
                    (diff_cell1 == Cell.DoorEnterance && diff_cell2 == Cell.DoorExit)
                    ||
                    (diff_cell1 == Cell.DoorExit && diff_cell2 == Cell.DoorEnterance)
                ) {
                    rotation = dir;
                    break;
                }
            }

            // не ругайте меня пж
            if (rotation == 3) rotation = 1;
            else if (rotation == 1) rotation = 3;

            GameObject obj_stair = Spawn(
                stairs[0],
                x, y, z - 1, rotation * 90,
                $"stair {current_id}, cell type: {current_cell}, xyz: {x} {y} {z}, rotation: {rotation}"
            );
        }


        // spawn ceiling everywhere where id > 0 and no same id at the level higher
        bool must_spawn_ceiling = (current_id > 0);
        if (z < maze.Height - 1) {
            if (current_id == ids[x][y][z + 1]) {
                must_spawn_ceiling = false;
            }
        }

        if (must_spawn_ceiling) {

            bool can_spawn_decorative_ceiling = false;
            if (z >= maze.Height - 1 || ids[x][y][z + 1] == 0) {
                can_spawn_decorative_ceiling = true;
            }
            int ceiling_variant = can_spawn_decorative_ceiling ? GetRandomIndex(ceilings.Length, 0.75f) : 0;

            GameObject GO = Spawn(
                ceilings[ceiling_variant],
                x, y, z, maze.RandomInclusive(0, 3) * 90,
                "ceiling",
                mazeCeilingContainer
            );

            // поворот для потолочков <(^_^)> =(^_^)=
            GO.transform.SetPositionAndRotation(
                GO.transform.position + new Vector3(0, 4.0f - 0.2f, 0),
                Quaternion.Euler(180, 0, 0)
            );
        }


        // spawn 2, 3 or 4 walls
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
            if (y + dy >= 0 && y + dy < maze.Size && x + dx >= 0 && x + dx < maze.Size) {
                neighbor = cells[x + dx][y + dy][z];
                nearId = ids[x + dx][y + dy][z];
                debug = "in bounds";
            }

            /*
            // спавн стен для первой комнаты на втором этаже
            if ((current_id == 1 && nearId != 1)) {
                // spawn wall in air
                GameObject wall2;
                wall2 = SpawnWithOffsetAndRecolor(walls[GetRandomIndexOfWall()], x, y, rotation);
                wall2.name = $"id={ids[x][y]}, type 2NDFLOOR, {debug}";
                wall2.transform.SetPositionAndRotation(
                    wall2.transform.position + new Vector3(0, 4, 0),
                    wall2.transform.rotation
                );
            }
            */


            if (direction == 4 && y > 0) continue;
            if (direction == 3 && x > 0) continue;

            if (current_id == 0 && nearId == 0) continue;

            GameObject obj;
            if (current_cell == Cell.DoorDeadEnd && neighbor == Cell.Wall) {
                int deadend_variant = ids[x][y][z] % deadends.Length;
                obj = SpawnWithOffsetAndRecolor(deadends[deadend_variant], x, y, z, rotation);
                obj.name = $"id={ids[x][y][z]}, type deadend, variant {deadend_variant}, {debug}";
            } else if (neighbor == Cell.Wall || nearId == 0) {
                // spawn wall
                obj = SpawnWithOffsetAndRecolor(walls[GetRandomIndexOfWall()], x, y, z, rotation);
                obj.name = $"id={ids[x][y][z]}, type 1, {debug}";
            } else if (nearId > 0 && nearId != ids[x][y][z]) {
                // wall or door?
                if (
                    (cells[x][y][z] == Cell.DoorEnterance && neighbor == Cell.DoorExit)
                    ||
                    (cells[x][y][z] == Cell.DoorExit && neighbor == Cell.DoorEnterance)
                    ) {
                    obj = SpawnWithOffsetAndRecolor(doors[controller.MazeGenerator.RandomInclusive(0, doors.Length - 1)], x, y, z, rotation, "door");
                    obj.name = $"id={ids[x][y][z]}, type 2A, {debug}";
                } else {
                    obj = SpawnWithOffsetAndRecolor(walls[GetRandomIndexOfWall()], x, y, z, rotation);
                    obj.name = $"id={ids[x][y][z]}, type 2B, {debug}";
                }
            } else {
                // nothing to spawn - clear passage
                continue;
            }
        }

        // спавн декораций
        if (x > 0 && x < controller.MazeGenerator.Size - 1 && y > 0 && y < controller.MazeGenerator.Size - 1) {
            if (current_id > 1 && current_room == Room.Square) {
                bool in_middle = true;
                for (int d = 0; d <= 3; d++) {
                    controller.MazeGenerator.GetDirectionsVector(d, out int dx, out int dy);
                    if (ids[x + dx][y + dy][z] != current_id) {
                        in_middle = false;
                        break;
                    }
                }
                if (!in_middle && current_cell == Cell.Passage) {
                    // мы находимся на краю квадратной комнаты + текущая ячейка не проход: можно спавнить декорации
                    int deco_type = RandomInclusive(1, 3);
                    if (deco_type == 1) {
                        Spawn(deco1[RandomInclusive(0, deco1.Length - 1)], x, y, z, RandomInclusive(0, 360 - 1), "deco1");
                    }
                    if (deco_type == 2) {
                        Spawn(deco2[RandomInclusive(0, deco2.Length - 1)], x, y, z, RandomInclusive(0, 3) * 90, "deco2");
                    }
                    if (deco_type == 3) {
                        Spawn(deco3[RandomInclusive(0, deco3.Length - 1)], x, y, z, RandomInclusive(0, 7) * 45, "deco3");
                    }
                }
            }
        }

        // спавн уголков только поверх чистых проходов без дверей и только для пещер
        if (current_cell == Cell.Passage && current_room == Room.Cave) {
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
                if (y + dy1 >= 0 && y + dy1 < maze.Size && x + dx1 >= 0 && x + dx1 < maze.Size) {
                    near1_cell = cells[x + dx1][y + dy1][z];
                    near1_id = ids[x + dx1][y + dy1][z];
                }

                int direction2 = GetClockwiseDirection(direction1);
                int dx2 = (direction2 == 1) ? 1 : (direction2 == 3) ? -1 : 0;
                int dy2 = (direction2 == 2) ? 1 : (direction2 == 4) ? -1 : 0;

                //UnityEngine.Debug.Log($"dir: {direction1} -> {direction2}");

                Cell near2_cell = Cell.Wall;
                int near2_id = 0;
                if (y + dy2 >= 0 && y + dy2 < maze.Size && x + dx2 >= 0 && x + dx2 < maze.Size) {
                    near2_cell = cells[x + dx2][y + dy2][z];
                    near2_id = ids[x + dx2][y + dy2][z];
                }

                if (current_id != near1_id && current_id != near2_id) {
                    int corner_variant = ids[x][y][z] % corners.Length;
                    GameObject corner = SpawnWithRecolor(corners[corner_variant], x, y, z, rotation);
                    corner.name = $"corner={ids[x][y][z]}";
                }
            }
        }

        return true;
    }

    private int GetClockwiseDirection(int dir) {
        return (dir - 1 + 1) % 4 + 1;
    }

    private int RandomInclusive(int min, int max) {
        return controller.MazeGenerator.RandomInclusive(min, max);
    }

    private int GetRandomIndex(int length, float probability_of_zero_index = 0.5f) {
        if (RandomInclusive(0, 100) / 100.0f < probability_of_zero_index) return 0;
        return RandomInclusive(1, length - 1);
    }

    private int GetRandomIndexOfWall() {
        if (RandomInclusive(0, 100) / 100.0f < 0.5f) {
            return RandomInclusive(0, walls_simple_count - 1);
        } else {
            return RandomInclusive(walls_simple_count, walls.Length - 1);
        }
    }

    private void ColorizeFloor(GameObject floor, int id) {
        var floorMesh = (MeshRenderer) floor.GetComponent(typeof(MeshRenderer));
        var matProp = new MaterialPropertyBlock();
        Color clr = controller.Utils.GetFloorColor(id);
        matProp.SetColor("_Color", clr);
        floorMesh.SetPropertyBlock(matProp);
    }

    private GameObject Spawn(
            Mesh mesh,
            int x,
            int y,
            int z,
            int rotation,
            string name = "",
            Transform cutstomContainer = null
        ) {

        GameObject GO = Instantiate(universalPrefab, cutstomContainer != null ? cutstomContainer : mazeContainer);

        GO.GetComponent<MeshFilter>().sharedMesh = mesh;
        GO.GetComponent<MeshCollider>().sharedMesh = mesh;

        Vector3 position = GO.transform.position;
        position.x = (x - controller.MazeGenerator.Size / 2) * ROOMS_OFFSET;
        position.z = (y - controller.MazeGenerator.Size / 2) * ROOMS_OFFSET;
        position.y = (z - controller.MazeGenerator.Height / 2) * ROOMS_OFFSET;
        GO.transform.SetPositionAndRotation(position, Quaternion.Euler(0, rotation, 0));
        GO.transform.localScale = new Vector3(ROOM_SCALE, ROOM_SCALE, ROOM_SCALE);

        GO.name = name;
        buildLeft--;

        return GO;
    }

    private GameObject SpawnWithRecolor(
        Mesh mesh,
        int x,
        int y,
        int z,
        int rotation,
        string name = ""
    ) {
        GameObject GO = Spawn(mesh, x, y, z, rotation, name);

        // покраска для стеночек для каждого нового уровня
        Material material = GO.GetComponent<MeshRenderer>().materials[0];
        material.SetFloat("_Hue", (controller.level * 0.31473248f) % 1f);
        material.SetFloat("_Contrast", (controller.level * 374262944 % 5 + 8) / 10f);

        return GO;
    }

    private GameObject SpawnWithOffsetAndRecolor(
        Mesh mesh,
        int x,
        int y,
        int z,
        int rotation,
        string name = ""
    ) {
        GameObject GO = SpawnWithRecolor(mesh, x, y, z, rotation, name);

        // смещение для стеночек вдоль поворота
        Vector3 position = GO.transform.position;
        if (rotation == 0) position.z += 2;
        if (rotation == 90) position.x += 2;
        if (rotation == 180) position.z -= 2;
        if (rotation == 270) position.x -= 2;
        GO.transform.SetPositionAndRotation(position, Quaternion.Euler(0, rotation, 0));

        return GO;
    }

    private MazeObject SpawnChest(int x, int y, int z) {
        // lets spawn treasures only on master
        if (!Networking.IsOwner(gameObject)) return null;

        Vector3 position;
        position.x = (x - controller.MazeGenerator.Size / 2) * ROOMS_OFFSET;
        position.z = (y - controller.MazeGenerator.Size / 2) * ROOMS_OFFSET;
        position.y = (z - controller.MazeGenerator.Height / 2) * ROOMS_OFFSET + 1;

        if (!chestPool.TryTake(out MazeObject GO, position, Quaternion.Euler(0, 0, 0))) {
            controller.MazeUI.UILog("No more chest in pool!");
            return null;
        }

        var treasure = GO.GetComponent<Treasure>();
        controller.MazeUI.UILog(
            $"Spawn {GO.name}, id = {treasure.pool_id} " +
            $"\n- x, y = {x}, {y}, {z} => {position.x}, {position.z}, {position.y}"
        );

        buildLeft = 0;
        return GO;
    }
}
