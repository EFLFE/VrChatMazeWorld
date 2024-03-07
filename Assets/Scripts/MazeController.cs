using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class MazeController : UdonSharpBehaviour {
    [SerializeField] private bool buildOnStart;

    public MazeBuilder Builder;
    public MazeGenerator Generator;
    public Utils Utils;
    public MazeUI UI;

    public TMPro.TextMeshProUGUI debugText;

    [UdonSynced] private int seed;

    private RoomTypeEnum[][] maze;

    private void Start() {
        Generator.Init(this);
        Builder.Init(this);
        UI.Init(this);

        if (buildOnStart) {
            bool isOwner = Networking.LocalPlayer.IsOwner(gameObject);
            if (isOwner) {
                seed = Random.Range(0, 9999);
                Build();
            }
            RequestSerialization();
        }
    }

    public override void OnDeserialization() {
        base.OnDeserialization();
        Build();
    }

    public void SendRebuild() {
        seed = Random.Range(0, 9999);
        if (Networking.LocalPlayer.IsOwner(gameObject))
            Build();
        RequestSerialization();
    }

    public void Build() {
        maze = Generator.Generate(seed);
        PrintRooms();
        Builder.BuildRoomsBegin(this, maze);

        //Vector2 pos = Builder.GetMainRoomPos(rooms);
        //Networking.LocalPlayer.TeleportTo(new Vector3(pos.x, 1, pos.y), Quaternion.identity);
    }

    public void Update() {
        if (Input.GetKeyDown(KeyCode.O)) {
            //debugText.text += $"Pressed!!!\n";
            seed++;
            maze = Generator.Generate(seed);
            PrintRooms();
        }

        if (maze != null && !Builder.MazeReady)
            if (Builder.BuildRoomsIter(maze))
                UI.HideProgress();

        UI.ManualUpdate();
    }

    public void PrintRooms() {
        debugText.text = "";
        for (int x = 0; x < maze.Length; x++) {
            for (int y = 0; y < maze[x].Length; y++) {
                debugText.text += (maze[x][y] == RoomTypeEnum.Nothing) ? " " : "#";
            }
            debugText.text += "\n";
        }
        bool isOwner = Networking.LocalPlayer.IsOwner(gameObject);
        debugText.text += $"\nBuild seed: {seed}\n{(isOwner ? "Owner" : "")}";
    }

}
