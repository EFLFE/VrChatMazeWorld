
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MazeController : UdonSharpBehaviour {
    public MazeBuilder Builder;
    public MazeGenerator Generator;
    public Utils Utils;

    public TMPro.TextMeshProUGUI debugText;

    [UdonSynced] private int seed;

    private RoomTypeEnum[][] maze;

    private void Start() {
        Generator.Init(this);

        bool isOwner = Networking.LocalPlayer.IsOwner(gameObject);
        if (isOwner) {
            debugText.text += "Owner\n";
            seed = Random.Range(0, 9999);
            Build();
        }
        RequestSerialization();
    }

    public override void OnDeserialization() {
        base.OnDeserialization();
        Build();
    }

    public void SendRebuild()
    {
        seed = Random.Range(0, 9999);
        if (Networking.LocalPlayer.IsOwner(gameObject))
            Build();
        RequestSerialization();
    }

    public void Build() {
        debugText.text += $"Build seed: {seed}\n";

        maze = Generator.Generate(seed);
        PrintRooms();
        Builder.BuildRooms(maze);

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
    }

    public void PrintRooms() {
        debugText.text = "";
        for (int x = 0; x < maze.Length; x++) {
            for (int y = 0; y < maze[x].Length; y++) {
                debugText.text += (maze[x][y] == RoomTypeEnum.Nothing) ? " " : "#";
            }
            debugText.text += "\n";
        }
    }

}
