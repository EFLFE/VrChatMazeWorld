using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Map : UdonSharpBehaviour {
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private Transform bgImage;
    [SerializeField] private GameObject facadePrefab;
    [SerializeField] private GameObject imageCellPrefab;
    [SerializeField] private GameObject imageWallUpPrefab;
    [SerializeField] private GameObject imageWallRightPrefab;
    [SerializeField] private GameObject imageWallBottomPrefab;
    [SerializeField] private GameObject imageWallLeftPrefab;
    [Space]
    [SerializeField] private GameObject playerImagePrefab;
    //[SerializeField] private GameObject testPlayer;
    [SerializeField] private float floorScaleTest = 0.5f;

    private MazeController controller;
    private Vector2 cellSize;
    private Transform mapContainer;
    private RectTransform[] playersImageRect;
    private VRCPlayerApi[] allPlayers;

    public void Init(MazeController controller) {
        this.controller = controller;
        playersImageRect = new RectTransform[64];
        allPlayers = new VRCPlayerApi[64];
        enabled = true;
    }

    private void Update() {
        int playersCount = VRCPlayerApi.GetPlayerCount();
        VRCPlayerApi.GetPlayers(allPlayers);
        for (int i = 0; i < playersCount; i++) {
            var item = allPlayers[i];
            UpdatePlayer(item.GetPosition(), i, item.isLocal);
        }
    }

    private void UpdatePlayer(Vector3 playerPos, int index, bool isLocal) {
        if (playersImageRect[index] == null) {
            var obj = Instantiate(playerImagePrefab, canvasRect.transform);
            playersImageRect[index] = obj.GetComponent<RectTransform>();

            var image = obj.GetComponent<Image>();
            Color clr;
            const float D = 255f;
            switch (index % 7) {
                case 0: clr = new Color(225 / D, 155 / D, 155 / D); break;
                case 1: clr = new Color(225 / D, 205 / D, 158 / D); break;
                case 2: clr = new Color(175 / D, 225 / D, 158 / D); break;
                case 3: clr = new Color(158 / D, 223 / D, 225 / D); break;
                case 4: clr = new Color(158 / D, 158 / D, 225 / D); break;
                case 5: clr = new Color(225 / D, 158 / D, 225 / D); break;
                case 6: clr = new Color(158 / D, 158 / D, 158 / D); break;
                default: clr = Color.white; break;
            }
            image.color = clr;
        }

        float mapX = (playerPos.x / floorScaleTest) + (canvasRect.sizeDelta.x / 2f);
        float mapY = (playerPos.z / floorScaleTest) + (canvasRect.sizeDelta.y / 2f);
        playersImageRect[index].anchoredPosition = new Vector2(mapX, -mapY);
    }

    public void Clear() {
        if (mapContainer != null) {
            Destroy(mapContainer.gameObject);
            mapContainer = null;
        }
    }

    public void Render(MazeGenerator maze) {
        Clear();

        mapContainer = Instantiate(facadePrefab, canvasRect.transform).transform;
        mapContainer.SetSiblingIndex(bgImage.GetSiblingIndex() + 1);

        cellSize.x = canvasRect.sizeDelta.x / maze.Size;
        cellSize.y = canvasRect.sizeDelta.y / maze.Size;
        Cell[][] cells = maze.Cells;
        int[][] ids = maze.Ids;

        for (int y = 0; y < maze.Size; y++) {
            for (int x = 0; x < maze.Size; x++) {
                bool safeZone = x != 0 && y != 0 && x < maze.Size - 1 && y < maze.Size - 1;
                Cell cellType = cells[x][y];

                switch (cellType) {
                    case Cell.Passage:
                    case Cell.DoorDeadEnd:
                    case Cell.DoorEnterance:
                    case Cell.DoorExit:
                        CreateImage(x, y, Color.gray, cellType);
                        break;

                    case Cell.Hole:
                        CreateImage(x, y, Color.black, cellType);
                        break;
                }

                if (safeZone) {
                    int curId = ids[x][y];
                    if (curId != ids[x][y - 1] && cells[x][y - 1] != Cell.DoorEnterance && cells[x][y - 1] != Cell.DoorExit)
                        CreateWall(x, y, imageWallUpPrefab);
                    if (curId != ids[x][y + 1] && cells[x][y + 1] != Cell.DoorEnterance && cells[x][y + 1] != Cell.DoorExit)
                        CreateWall(x, y, imageWallBottomPrefab);
                    if (curId != ids[x - 1][y] && cells[x - 1][y] != Cell.DoorEnterance && cells[x - 1][y] != Cell.DoorExit)
                        CreateWall(x, y, imageWallLeftPrefab);
                    if (curId != ids[x + 1][y] && cells[x + 1][y] != Cell.DoorEnterance && cells[x + 1][y] != Cell.DoorExit)
                        CreateWall(x, y, imageWallRightPrefab);
                }
            }
        }
    }

    private void CreateImage(int cellX, int cellY, Color clr, Cell cellType) {
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
