using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Map3d : UdonSharpBehaviour {
    [SerializeField] Transform staticContainer;
    [SerializeField] GameObject contentPrefab;
    [SerializeField] GameObject floorPrefab;
    [SerializeField] GameObject wallPrefab;
    [SerializeField] float floorMoveOffset = 0.1f;
    [SerializeField] float floorHeightMoveOffset = 0.1f;
    [SerializeField] float colorAlpha = 0.5f;

    MazeController controller;
    Transform mapContainer;

    public void Init(MazeController controller) {
        this.controller = controller;
        floorPrefab.SetActive(false);
        wallPrefab.SetActive(false);
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
                            clr = controller.Utils.GetFloorColor(id);
                        clr.a = colorAlpha;

                        var prop = new MaterialPropertyBlock();
                        prop.SetColor("_Color", clr);
                        var meshRender = floorObj.GetComponent<MeshRenderer>();
                        meshRender.SetPropertyBlock(prop);

                        // create wall
                        for (int direction = 1; direction <= 4; direction++) {
                            // 1 up, 2 right, 3 down, 4 left
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

}
