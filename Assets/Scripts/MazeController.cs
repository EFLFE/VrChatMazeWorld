using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class MazeController : UdonSharpBehaviour {
    [SerializeField] private bool buildOnStart;
    [SerializeField] private int maxRooms = 200; // test

    public MazeBuilder Builder;
    public MazeV2 GeneratorV2;
    public Utils Utils;
    public MazeUI UI;

    public TMPro.TextMeshProUGUI debugText;

    [UdonSynced] private int seed;

    private void Start() {
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

    private bool generator_is_ready = false;

    public void Build() {
        debugText.text = $"Build(), seed: {seed}";
        Builder.Init(this);
        GeneratorV2.Init(maxRooms, seed);
        generator_is_ready = false;
    }

    public void Update() {
        //if (Input.GetKeyDown(KeyCode.O)) {
        //    //debugText.text += $"Pressed!!!\n";
        //    seed++;
        //    GeneratorV2.Init(maxRooms, seed);
        //    GeneratorV2.Generate();
        //    PrintRooms();
        //}

        if (!generator_is_ready) {
            generator_is_ready = GeneratorV2.Generate();
            UI.SetProgressValue((float) GeneratorV2.current_id / maxRooms);
        }

        // if (Input.GetKeyDown(KeyCode.O))
        if (!Builder.MazeReady && generator_is_ready)
            if (Builder.BuildRoomsIter())
                UI.HideProgress();
    }

    public void PrintRooms() {
        debugText.text = "";
        var maze = GeneratorV2.GetCells;
        for (int x = 0; x < maze.Length; x++) {
            for (int y = 0; y < maze[x].Length; y++) {
                if (maze[x][y] == Cell.DoorEnterance)
                    debugText.text += "D";

                else if (maze[x][y] == Cell.DoorExit)
                    debugText.text += "d";

                else if (maze[x][y] == Cell.Passage)
                    debugText.text += ".";

                else if (maze[x][y] == Cell.Wall)
                    debugText.text += "#";

                else
                    debugText.text += ".";
            }
            debugText.text += "\n";
        }
        bool isOwner = Networking.LocalPlayer.IsOwner(gameObject);
        debugText.text += $"\nBuild seed: {seed}\n{(isOwner ? "Owner" : "")}";
    }

}
