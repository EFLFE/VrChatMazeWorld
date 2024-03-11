﻿using System.Collections.Generic;
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
            string debug = $"out of bounds: x + dx, y + dy, dx, dy: {x + dx}, {y + dy}, {dx}, {dy}";
            if (y + dy >= 0 && y + dy < MazeSize && x + dx >= 0 && x + dx < MazeSize) {
                neighbor = cells[x + dx][y + dy];
                nearId = ids[x + dx][y + dy];
                debug = "in bounds";
            }

            GameObject obj = null;
            if (neighbor == Cell.Wall || nearId == 0) {
                // spawn wall
                obj = Instantiate(wallPrefab, mazeContainer);
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
                    obj = Instantiate(wallPrefab, mazeContainer);
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
