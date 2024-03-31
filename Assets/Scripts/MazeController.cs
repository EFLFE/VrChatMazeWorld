using QvPen.UdonScript;
using System.Diagnostics;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class MazeController : UdonSharpBehaviour {
    [SerializeField] private bool buildOnStart;
    [SerializeField] private int maxRooms = 200; // test
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform enemySpawn;

    public MazeBuilder MazeBuilder;
    public MazeGenerator MazeGenerator;
    public MazeUI MazeUI;

    public TMPro.TextMeshProUGUI debugText;

    private bool generator_is_ready = false;
    private Stopwatch genStopwatch;

    [UdonSynced] private int seed;
    [SerializeField]
    private QvPen_Settings QV_PEN_Settings;

    private void Start() {
        genStopwatch = new Stopwatch();

        MazeBuilder.Init(this);
        MazeUI.Init(this);

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

    // event
    public void SendRebuild() {
        seed = Random.Range(0, 9999);
        if (Networking.LocalPlayer.IsOwner(gameObject))
            Build();
        RequestSerialization();

        // clear all pens
        if (QV_PEN_Settings) {

            foreach (var penManager in QV_PEN_Settings.penManagers)
                if (penManager)
                    penManager.Clear();
            foreach (var penManager in QV_PEN_Settings.penManagers)
                if (penManager)
                    penManager.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(QvPen_PenManager.ResetPen));
            foreach (var eraserManager in QV_PEN_Settings.eraserManagers)
                if (eraserManager)
                    eraserManager.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(QvPen_EraserManager.ResetEraser));
        }
    }

    // event (test)
    public void SpawnEnemy() {
        if (Networking.LocalPlayer.IsOwner(gameObject)) {
            var obj = Instantiate(enemyPrefab, enemySpawn);
            obj.transform.localPosition = Vector3.zero;
            var script = obj.GetComponent<BaseEnemy>();
            script.Init();
        }
    }

    public void Build() {
        genStopwatch.Restart();
        debugText.text = $"Build(), seed: {seed}";
        MazeBuilder.Init(this);
        MazeGenerator.Init(maxRooms, seed);
        generator_is_ready = false;
    }

    public void Update() {
        if (!generator_is_ready) {
            generator_is_ready = MazeGenerator.Generate();

            if (generator_is_ready) {
                genStopwatch.Stop();
                debugText.text = $"Build(), seed: {seed}, max_room_id: {MazeGenerator.current_id}" +
                    $"\nGen time sec: {System.Math.Round(genStopwatch.Elapsed.TotalSeconds, 2)}";
            }

            MazeUI.SetProgressValue((float) MazeGenerator.current_id / maxRooms);
        }

        // if (Input.GetKeyDown(KeyCode.O))
        if (!MazeBuilder.MazeReady && generator_is_ready)
            if (MazeBuilder.BuildRoomsIter())
                MazeUI.HideProgress();
    }

    public void PrintRooms() {
        debugText.text = "";
        Cell[][] maze = MazeGenerator.GetCells;
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
