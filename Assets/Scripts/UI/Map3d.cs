﻿using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Map3d : UdonSharpBehaviour {
    [SerializeField] private Transform staticContainer;
    [SerializeField] private GameObject contentPrefab;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private float floorMoveOffset = 0.1f;
    [SerializeField] private float floorHeightMoveOffset = 0.1f;
    [SerializeField] private float colorAlpha = 0.5f;

    private MazeController controller;
    private Transform mapContainer;
    private GameObject[] playersDots;

    public void Init(MazeController controller) {
        this.controller = controller;
        floorPrefab.SetActive(false);
        wallPrefab.SetActive(false);
        playerPrefab.SetActive(false);
        playersDots = new GameObject[64];
        enabled = true;
    }

    public void Clear() {
        if (mapContainer != null) {
            Destroy(mapContainer.gameObject);
            mapContainer = null;
        }

        mapContainer = Instantiate(contentPrefab, staticContainer).transform;
    }

    public void GenerateMap() {
        Clear();
        // 0.0 = center
        // 0.5 = edge

        var maze = controller.MazeGenerator;
        for (int z = 0; z < maze.Height; z++) {
            for (int x = 0; x < maze.Size; x++) {
                for (int y = 0; y < maze.Size; y++) {
                    int id = maze.GetId(x, y, z);
                    if (id == 0) continue;

                    var cell = maze.GetCell(x, y, z);
                    bool isFloor = cell != Cell.Wall && id != maze.GetId(x, y, z - 1);

                    if (isFloor) {
                        var localPos = new Vector3(x * floorMoveOffset, z * floorHeightMoveOffset, y * floorMoveOffset);

                        // create floor
                        var floorObj = Instantiate(floorPrefab, mapContainer);
                        floorObj.name = $"Floor {x}:{y}:{z}";
                        floorObj.transform.localPosition = localPos;
                        floorObj.SetActive(true);

                        // floor color
                        Color clr = Color.white;
                        if (id > 1)
                            clr = Utils.GetFloorColor(id);
                        clr.a = colorAlpha;

                        var prop = new MaterialPropertyBlock();
                        prop.SetColor("_Color", clr);
                        var meshRender = floorObj.GetComponent<MeshRenderer>();
                        meshRender.SetPropertyBlock(prop);

                        // create wall
                        for (int direction = 1; direction <= 4; direction++) {
                            // 1 up, 2 right, 3 down, 4 left.
                            int dx = (direction == 2) ? 1 : (direction == 4) ? -1 : 0;
                            int dy = (direction == 1) ? 1 : (direction == 3) ? -1 : 0;
                            int neirID = maze.GetId(x + dx, y + dy, z);
                            Cell neirCell = maze.GetCell(x + dx, y + dy, z);

                            if (neirID == 0
                                || neirCell == Cell.Wall
                                || (neirID > 0 && neirID != id
                                    && !(
                                        (cell == Cell.DoorEnterance && neirCell == Cell.DoorExit)
                                        ||
                                        (cell == Cell.DoorExit && neirCell == Cell.DoorEnterance)
                                        )
                                    )
                                ) {
                                var wallObj = Instantiate(wallPrefab, mapContainer);
                                wallObj.name = $"Wall {x}:{y}:{z}";
                                wallObj.transform.localPosition = localPos;
                                float rotate = (direction - 2) * 90;
                                wallObj.transform.eulerAngles = new Vector3(0, rotate, 0);
                                wallObj.SetActive(true);
                            }
                        }
                    }
                }
            }
        }

    }

    public void ManualUpdate() {
        // reset dots
        for (int i = 0; i < playersDots.Length; i++) {
            if (playersDots[i] == null)
                break;
            playersDots[i].transform.position = new Vector3(999999, 99999, 0);
        }

        // set players dots
        var players = controller.PlayersManager.GetPlayers;
        for (int i = 0; i < players.Length; i++) {
            PlayerData player = players[i];
            if (!player.IsValid())
                continue;

            Vector3 playerPos = player.GetGlobalPos;
            var mazeGen = controller.MazeGenerator;

            playerPos.x = (playerPos.x / MazeBuilder.ROOMS_OFFSET + (mazeGen.Size / 2f)) * floorMoveOffset;
            playerPos.z = (playerPos.z / MazeBuilder.ROOMS_OFFSET + (mazeGen.Size / 2f)) * floorMoveOffset;
            playerPos.y = (playerPos.y / MazeBuilder.ROOMS_OFFSET + (mazeGen.Height - mazeGen.StartRoomHeight)) * floorHeightMoveOffset;

            playerPos.x -= floorMoveOffset / 2f;
            playerPos.z -= floorMoveOffset / 2f;

            var dot = GetPlayerDot(i);
            dot.transform.localPosition = playerPos;
        }

    }

    private GameObject GetPlayerDot(int index) {
        if (playersDots[index] == null) {
            playersDots[index] = Instantiate(playerPrefab, staticContainer);
            playersDots[index].SetActive(true);
        }
        return playersDots[index];
    }

}
