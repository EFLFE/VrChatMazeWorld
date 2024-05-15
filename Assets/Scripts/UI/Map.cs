using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Map : UdonSharpBehaviour {
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private Transform staticContainer;
    [SerializeField] private Transform dynamicContainer;
    [SerializeField] private Transform bgImage;
    [SerializeField] private GameObject facadePrefab;
    [SerializeField] private GameObject imageCellPrefab;
    [SerializeField] private GameObject imageWallUpPrefab;
    [SerializeField] private GameObject imageWallRightPrefab;
    [SerializeField] private GameObject imageWallBottomPrefab;
    [SerializeField] private GameObject imageWallLeftPrefab;
    [Space]
    [SerializeField] private GameObject playerImagePrefab;
    [SerializeField] private Transform poolsOfTreasures;

    private MazeController controller;
    private MazeGenerator maze;
    private Vector2 cellSize;
    private Transform mapContainer;
    private VRCPlayerApi[] allPlayers;
    private RectTransform[] circleRects;
    private int circleIndex;

    private bool[] rooms_explored;
    private bool[][] coords_explored;

    public void Init(MazeController controller) {
        this.controller = controller;
        maze = controller.MazeGenerator;
        circleRects = new RectTransform[256];
        allPlayers = new VRCPlayerApi[64];
        circleIndex = -1;
        enabled = true;
    }

    private void Update() {
        // clear old circles
        for (int i = 0; i <= circleIndex; i++) {
            circleRects[i].anchoredPosition = new Vector2(-999, 999);
        }

        // draw new
        circleIndex = -1;

        int playersCount = VRCPlayerApi.GetPlayerCount();
        VRCPlayerApi.GetPlayers(allPlayers);
        for (int i = 0; i < playersCount; i++) {
            VRCPlayerApi player = allPlayers[i];
            Color clr;
            const float D = 255f;
            switch (i % 7) {
                case 0: clr = new Color(225 / D, 155 / D, 155 / D); break;
                case 1: clr = new Color(225 / D, 205 / D, 158 / D); break;
                case 2: clr = new Color(175 / D, 225 / D, 158 / D); break;
                case 3: clr = new Color(158 / D, 223 / D, 225 / D); break;
                case 4: clr = new Color(158 / D, 158 / D, 225 / D); break;
                case 5: clr = new Color(225 / D, 158 / D, 225 / D); break;
                case 6: clr = new Color(158 / D, 158 / D, 158 / D); break;
                default: clr = Color.white; break;
            }
            DrawCircle(player.GetPosition(), clr);

            int size = controller.MazeGenerator.Size;
            int x = (int) (size / 2f + player.GetPosition().x / MazeBuilder.ROOMS_OFFSET);
            int y = (int) (size / 2f + player.GetPosition().z / MazeBuilder.ROOMS_OFFSET);
            if (x >= 0 && x < size && y >= 0 && y < size) {
                PlayerIsMoving(x, y, controller.MazeGenerator.GetId(x, y));
            }
        }

        // treasures
        /*
        int count = Mathf.Min(controller.GetChestsAmount, poolsOfTreasures.childCount);
        for (int i = 0; i < count; i++) {
            Transform treasureTran = poolsOfTreasures.GetChild(i);
            if (treasureTran.gameObject.activeSelf)
                DrawCircle(treasureTran.position, Color.red);
        }
        */
    }

    private void PlayerIsMoving(int x, int y, int room_id) {
        if (rooms_explored == null) return;
        if (coords_explored == null) return;

        int radius = 2;

        for (int dx = -radius; dx <= radius; dx++) {
            for (int dy = -radius; dy <= radius; dy++) {
                if (!coords_explored[x + dx][y + dy] && maze.GetId(x + dx, y + dy) == room_id) {
                    coords_explored[x + dx][y + dy] = true;
                    DrawBlock(x + dx, y + dy);
                }
            }
        }
    }

    private void DrawCircle(Vector3 pos, Color clr) {
        DrawCircle(new Vector2(pos.x, pos.z), clr);
    }

    private void DrawCircle(Vector2 pos, Color clr) {
        circleIndex++;
        pos.x = -pos.x;
        if (circleRects[circleIndex] == null) {
            var obj = Instantiate(playerImagePrefab, dynamicContainer.transform);
            circleRects[circleIndex] = obj.GetComponent<RectTransform>();
            var image = obj.GetComponent<Image>();
            image.color = clr;
        }

        float mapX = canvasRect.sizeDelta.x / 2f + pos.x * (cellSize.x / MazeBuilder.ROOMS_OFFSET);
        float mapY = canvasRect.sizeDelta.y / 2f + pos.y * (cellSize.y / MazeBuilder.ROOMS_OFFSET);
        circleRects[circleIndex].anchoredPosition = new Vector2(mapX, -mapY);
    }

    public void NewLevel() {
        // former Clear()
        if (mapContainer != null) {
            Destroy(mapContainer.gameObject);
            mapContainer = null;
        }

        // former NewLevel()
        rooms_explored = new bool[maze.RoomsAmount];
        rooms_explored[1] = true;

        coords_explored = new bool[maze.Size][];
        for (int i = 0; i < maze.Size; i++) {
            coords_explored[i] = new bool[maze.Size];
        }

        // former Render()
        mapContainer = Instantiate(facadePrefab, staticContainer).transform;
        mapContainer.SetSiblingIndex(bgImage.GetSiblingIndex() + 1);

        cellSize.x = canvasRect.sizeDelta.x / maze.Size;
        cellSize.y = canvasRect.sizeDelta.y / maze.Size;
    }

    private void DrawBlock(int x, int y) {

        Cell cellType = maze.GetCell(x, y);
        int posX = maze.Size - x - 1;
        int posY = y;

        switch (cellType) {
            case Cell.Passage:
            case Cell.DoorDeadEnd:
            case Cell.DoorEnterance:
            case Cell.DoorExit:
                Color clr = controller.Utils.GetFloorColor(maze.Ids[x][y]);
                CreateCell(posX, posY, clr, cellType);
                break;

            case Cell.Hole:
                CreateCell(posX, posY, Color.black, cellType);
                break;
        }

        int curID = maze.GetId(x, y);
        if (curID != 0) {
            for (int direction = 1; direction <= 4; direction++) {
                // 1 up, 2 right, 3 down, 4 left
                int dx = (direction == 2) ? 1 : (direction == 4) ? -1 : 0;
                int dy = (direction == 1) ? 1 : (direction == 3) ? -1 : 0;
                int neirID = maze.GetId(x + dx, y + dy);
                Cell neirCell = maze.GetCell(x + dx, y + dy);

                if (neirID == 0
                    || neirCell == Cell.Wall
                    || (neirID > 0 && neirID != curID
                        && !(
                            (cellType == Cell.DoorEnterance && neirCell == Cell.DoorExit)
                            ||
                            (cellType == Cell.DoorExit && neirCell == Cell.DoorEnterance)
                            )
                        )
                    ) {
                    GameObject wallObj = null;
                    if (direction == 1) wallObj = imageWallBottomPrefab;
                    else if (direction == 2) wallObj = imageWallLeftPrefab;
                    else if (direction == 3) wallObj = imageWallUpPrefab;
                    else if (direction == 4) wallObj = imageWallRightPrefab;
                    CreateWall(posX, posY, wallObj);
                }
            }
        }
    }

    private void CreateCell(int cellX, int cellY, Color clr, Cell cellType) {
        var obj = Instantiate(imageCellPrefab, mapContainer);
        var image = obj.GetComponent<Image>();
        var rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = cellSize;
        rect.anchoredPosition = new Vector2(cellX * cellSize.x, -(cellY * cellSize.y));
        image.color = clr;
        obj.name = $"Img:{cellType}";
    }

    private void CreateWall(int cellX, int cellY, GameObject prefab) {
        var obj = Instantiate(prefab, mapContainer);
        var rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = cellSize;
        rect.anchoredPosition = new Vector2(cellX * cellSize.x, -(cellY * cellSize.y));
    }

}
