﻿using Cysharp.Threading.Tasks.Triggers;
using QvPen.UdonScript;
using System.Diagnostics;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class MazeController : UdonSharpBehaviour {
    [SerializeField] private bool buildOnStart;
    [SerializeField] private PoolObjects chestPool;

    [Space]
    [UdonSynced] private int mazeSize;
    [UdonSynced] private int mazeRoomsAmount;
    [UdonSynced] private int mazeChestsAmount;
    [Space]

    [UdonSynced] private int level = 1;
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

    public PoolObjects GetChestPool { get => chestPool; }

    [UdonSynced] public string network_event = "";
    private bool need_to_build_at_join = true;

    // called only on late-joiners (not master of this object)
    public override void OnDeserialization() {
        MazeUI.Log(
            $"OnDeserialization, " +
            $"\n- network_event = {network_event}" +
            $"\n- seed = {seed}" +
            $"\n- level = {level}" +
            $"\n- chests: {mazeChestsAmountGathered} / {mazeChestsAmount}"
        );
        base.OnDeserialization();
        if (network_event == nameof(NextLevel)) NextLevel();
        if (network_event == nameof(Build) || need_to_build_at_join) Build();
        network_event = ""; // no need, just for clarity
        need_to_build_at_join = false;
    }

    private void Start() {
        genStopwatch = new Stopwatch();

        chestPool.Init(this);
        MazeBuilder.Init(this);
        MazeUI.Init(this);

        seed = Random.Range(0, 9999);
        MazeUI.Log($"First seed generated by owner = {seed}");

        if (Networking.IsOwner(this.gameObject)) {
            network_event = nameof(NextLevel);
            RequestSerialization(); // works only for owner of this
            NextLevel();
        } else {
            // late joiners will build via OnDeserialization + network_event = Build
        }
    }


    private void NextLevel() {
        MazeUI.Log($"NextLevel, respawning all players");

        // respawn all players
        VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        players = VRCPlayerApi.GetPlayers(players);
        foreach (var player in players) {
            player.Respawn();
        }

        mazeChestsAmountGathered = 0;
        mazeSize = 19 + level * 2;
        mazeRoomsAmount = 15 + 5 * level;
        mazeChestsAmount = 1 + level;

        MazeUI.Log(
            $"NextLevel, new level = {level} "
            + $"\n- mazeSize = {mazeSize}"
            + $"\n- mazeRoomsAmount = {mazeRoomsAmount}"
            + $"\n- mazeChestsAmount = {mazeChestsAmount}"
        );

        Build();
    }

    public void Build() {
        MazeUI.Log($"Build, seed: {seed}, level: {level}");
        ClearQVPens();
        genStopwatch.Restart();
        MazeUI.Log($"- MazeGenerator Init, mazeSize: {mazeSize}, mazeRoomsAmount: {mazeRoomsAmount}, mazeChestsAmount: {mazeChestsAmount}");
        MazeGenerator.Init(seed, mazeSize, mazeRoomsAmount, mazeChestsAmount);
        MazeBuilder.Init(this);
        generator_is_ready = false;
    }

    // network event for master only
    public void OnTreasureGathered() {
        mazeChestsAmountGathered++;
        MazeUI.Log($"OnTreasureGathered, mazeChestsAmountGathered: {mazeChestsAmountGathered} / {mazeChestsAmount}");

        network_event = "";
        if (mazeChestsAmountGathered >= mazeChestsAmount) {
            if (Networking.IsOwner(this.gameObject)) {
                MazeUI.Log($"requesting NextLevel by owner, level: {level} => {level + 1}");
                level++;
                seed = Random.Range(0, 9999);
                MazeUI.Log($"- new seed generated by owner = {seed}");
                network_event = nameof(NextLevel);
                NextLevel(); // direct event call for owner of this
            }
        }
        RequestSerialization(); // send event to late-joiners (works only for owner of this)
    }

    public void ClearQVPens() {
        if (level <= 1) return;
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

    public void Update() {
        if (!generator_is_ready) {
            generator_is_ready = MazeGenerator.Generate();

            if (generator_is_ready) {
                genStopwatch.Stop();
                MazeUI.Log($"Build Complete, " +
                    //$"\n seed: {seed}, " +
                    //$"\n mazeSize: {mazeSize}" +
                    //$"\n mazeRoomsAmount: {mazeRoomsAmount}" +
                    //$"\n mazeChestsAmount: {mazeChestsAmount}" +
                    //$"\n MazeBuilder.MazeReady: {MazeBuilder.MazeReady}" +
                    $" gen time sec: {System.Math.Round(genStopwatch.Elapsed.TotalSeconds, 2)}"
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
