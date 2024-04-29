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
    private Vector2 cellSize;
    private Transform mapContainer;
    private VRCPlayerApi[] allPlayers;
    private RectTransform[] circleRects;
    private int circleIndex;

    public void Init(MazeController controller) {
        this.controller = controller;
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
            VRCPlayerApi item = allPlayers[i];
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
            DrawCircle(item.GetPosition(), clr);
        }

        // treasures
        int count = Mathf.Min(controller.GetChestsAmount, poolsOfTreasures.childCount);
        for (int i = 0; i < count; i++) {
            Transform treasureTran = poolsOfTreasures.GetChild(i);
            if (treasureTran.gameObject.activeSelf)
                DrawCircle(treasureTran.position, Color.red);
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

    public void Clear() {
        if (mapContainer != null) {
            Destroy(mapContainer.gameObject);
            mapContainer = null;
        }
    }

    public void Render(MazeGenerator maze) {
        Clear();

        // build map
        mapContainer = Instantiate(facadePrefab, staticContainer).transform;
        mapContainer.SetSiblingIndex(bgImage.GetSiblingIndex() + 1);

        cellSize.x = canvasRect.sizeDelta.x / maze.Size;
        cellSize.y = canvasRect.sizeDelta.y / maze.Size;
        Cell[][] cells = maze.Cells;
        int[][] ids = maze.Ids;

        for (int y = 0; y < maze.Size; y++) {
            for (int x = 0; x < maze.Size; x++) {
                bool safeZone = x != 0 && y != 0 && x < maze.Size - 1 && y < maze.Size - 1;
                Cell cellType = cells[x][y];
                int posX = maze.Size - x - 1;
                int posY = y;

                switch (cellType) {
                    case Cell.Passage:
                    case Cell.DoorDeadEnd:
                    case Cell.DoorEnterance:
                    case Cell.DoorExit:
                        Color clr = controller.Utils.GetFloorColor(ids[x][y]);
                        CreateCell(posX, posY, clr, cellType);
                        break;

                    case Cell.Hole:
                        CreateCell(posX, posY, Color.black, cellType);
                        break;
                }

                int curID = ids[x][y];
                Cell curCell = cells[x][y];
                if (safeZone && curID != 0) {
                    for (int direction = 1; direction <= 4; direction++) {
                        // 1 up, 2 right, 3 down, 4 left
                        int dx = (direction == 2) ? 1 : (direction == 4) ? -1 : 0;
                        int dy = (direction == 1) ? 1 : (direction == 3) ? -1 : 0;
                        int neirID = ids[x + dx][y + dy];
                        Cell neirCell = cells[x + dx][y + dy];

                        if (neirID == 0
                            || neirCell == Cell.Wall
                            || (neirID > 0 && neirID != curID
                                && !(
                                    (curCell == Cell.DoorEnterance && neirCell == Cell.DoorExit)
                                    ||
                                    (curCell == Cell.DoorExit && neirCell == Cell.DoorEnterance)
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
