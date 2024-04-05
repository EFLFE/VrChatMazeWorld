using Cysharp.Threading.Tasks.Triggers;
using QvPen.UdonScript;
using System.Diagnostics;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class MazeController : UdonSharpBehaviour {
    [SerializeField] private bool buildOnStart;

    [Space]
    [UdonSynced] private int mazeSize;
    [UdonSynced] private int mazeRoomsAmount;
    [UdonSynced] private int mazeChestsAmount;
    [Space]

    [UdonSynced] private int level = 0;
    [UdonSynced] private int mazeChestsAmountGathered = 0;

    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform enemySpawn;

    public MazeBuilder MazeBuilder;
    public MazeGenerator MazeGenerator;
    public MazeUI MazeUI;

    public TMPro.TextMeshProUGUI debugText;

    public CentralZone CentralZone;

    private bool generator_is_ready = false;
    private Stopwatch genStopwatch;

    [UdonSynced] private int seed;
    [SerializeField] private QvPen_Settings QV_PEN_Settings;


    private void Start() {
        genStopwatch = new Stopwatch();

        MazeBuilder.Init(this);
        MazeUI.Init(this);
        CentralZone.Init(this);

        GenerateNewLevel();
    }

    private void GenerateNewLevel() {
        level++;
        mazeChestsAmountGathered = 0;

        mazeSize = 49;
        mazeRoomsAmount = 15 + 5 * level;
        mazeChestsAmount = 4 + level;

        MazeUI.Log(
            $"GenerateNewLevel, level = {level} "
            + $"mazeSize = {mazeSize}"
            + $"mazeRoomsAmount = {mazeRoomsAmount}"
            + $"mazeChestsAmount = {mazeChestsAmount}"
        );

        SendRebuild();
    }

    // event
    public void SendRebuild() {
        seed = Random.Range(0, 9999);
        if (Networking.LocalPlayer.IsOwner(gameObject))
            Build();
        RequestSerialization();
    }

    public override void OnDeserialization() {
        base.OnDeserialization();
        Build();
    }

    public void Build() {
        ClearQVPens();

        genStopwatch.Restart();
        MazeUI.Log($"Build Start, seed: {seed}");
        MazeGenerator.Init(seed, mazeSize, mazeRoomsAmount, mazeChestsAmount);
        MazeBuilder.Init(this);
        generator_is_ready = false;
    }

    public void ClearQVPens() {
        if (level > 1) {
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


    public void Update() {
        if (!generator_is_ready) {
            generator_is_ready = MazeGenerator.Generate();

            if (generator_is_ready) {
                genStopwatch.Stop();
                MazeUI.Log($"Build Complete, " +
                    $"\n seed: {seed}, " +
                    $"\n mazeSize: {mazeSize}" +
                    $"\n mazeRoomsAmount: {mazeRoomsAmount}" +
                    $"\n mazeChestsAmount: {mazeChestsAmount}" +
                    $"\n MazeBuilder.MazeReady: {MazeBuilder.MazeReady}" +
                    $"\n Gen time sec: {System.Math.Round(genStopwatch.Elapsed.TotalSeconds, 2)}"
                );
            }

            MazeUI.SetProgressValue((float) MazeGenerator.CurrentId / mazeRoomsAmount);
        }

        // if (Input.GetKeyDown(KeyCode.O))
        if (!MazeBuilder.MazeReady && generator_is_ready)
            if (MazeBuilder.BuildRoomsIter())
                MazeUI.HideProgress();
    }

    public void PrintRooms() {
        debugText.text = "";
        Cell[][] maze = MazeGenerator.Cells;
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

    // event (test)
    public void SpawnEnemy() {
        if (Networking.LocalPlayer.IsOwner(gameObject)) {
            var obj = Instantiate(enemyPrefab, enemySpawn);
            obj.transform.localPosition = Vector3.zero;
            var script = obj.GetComponent<BaseEnemy>();
            script.Init();
        }
    }
}
