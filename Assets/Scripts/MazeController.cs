﻿using QvPen.UdonScript;
using System.Diagnostics;
using UdonSharp;
using UnityEngine;
using VRC.Economy;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class MazeController : UdonSharpBehaviour {
    [SerializeField] private bool buildOnStart;
    [SerializeField] private PoolObjects chestPool;
    [SerializeField] private SyncDataUI syncDataUI;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform enemySpawn;
    [SerializeField] private Map[] maps;
    [Header("Debug")]
    [SerializeField] private int startedLevel = 1;
    [SerializeField] private int startedSeed = 0;

    [UdonSynced] private int mazeSize;
    [UdonSynced] private int mazeRoomsAmount;
    [UdonSynced] private int mazeChestsAmount;
    [UdonSynced] private int level = 1;
    [UdonSynced] private int mazeChestsAmountGathered = 0;

    [Space]
    public MazeBuilder MazeBuilder;
    public MazeGenerator MazeGenerator;
    public MazeUI MazeUI;
    public Utils Utils;

    public CentralZone CentralZone;

    private bool generator_is_ready = false;
    private Stopwatch genStopwatch;

    [UdonSynced] private int seed;
    [SerializeField] private QvPen_Settings QV_PEN_Settings;

    public TMPro.TextMeshProUGUI progressText;

    public PoolObjects GetChestPool { get => chestPool; }
    public int GetChestsAmount => mazeChestsAmount;

    [UdonSynced] public string network_event = "";
    private bool need_to_build_at_join = true;

    // called only on late-joiners (not master of this object)
    public override void OnDeserialization() {
        MazeUI.UILog(
            $"MazeController OnDeserialization, " +
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
        UpdateProgressText();
    }

    private void Start() {
        genStopwatch = new Stopwatch();

        chestPool.Init(this);
        MazeBuilder.Init(this);
        MazeUI.Init(this);
        for (int i = 0; i < maps.Length; i++)
            maps[i].Init(this);

        seed = startedSeed == 0 ? Random.Range(0, 9999) : startedSeed;
        level = startedLevel;

        MazeUI.UILog($"First seed generated by owner = {seed}");

        if (IsOwner()) {
            network_event = nameof(NextLevel);
            RequestSerialization(); // works only for owner of this
            NextLevel();
        } else {
            // late joiners will build via OnDeserialization + network_event = Build
        }
        UpdateProgressText();
    }

    public bool IsOwner() {
        return Networking.IsOwner(gameObject);
    }

    private void NextLevel() {
        MazeUI.UILog($"NextLevel, respawning all players");

        // respawn all players
        VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        players = VRCPlayerApi.GetPlayers(players);
        foreach (VRCPlayerApi player in players) {
            player.TeleportTo(Vector3.zero, player.GetRotation());
        }

        CentralZone.gameObject.SetActive(false);

        mazeChestsAmountGathered = 0;
        mazeSize = 19 + level * 2;
        mazeRoomsAmount = 15 + 5 * level;
        mazeChestsAmount = 1 + level;

        MazeUI.UILog(
            $"NextLevel, new level = {level} "
            + $"\n- mazeSize = {mazeSize}"
            + $"\n- mazeRoomsAmount = {mazeRoomsAmount}"
            + $"\n- mazeChestsAmount = {mazeChestsAmount}"
        );

        Build();
    }

    public void Build() {
        MazeUI.UILog($"Build, seed: {seed}, level: {level}");
        ClearQVPens();
        genStopwatch.Restart();
        MazeUI.UILog($"- MazeGenerator Init, mazeSize: {mazeSize}, mazeRoomsAmount: {mazeRoomsAmount}, mazeChestsAmount: {mazeChestsAmount}");
        MazeGenerator.Init(seed, mazeSize, mazeRoomsAmount, mazeChestsAmount);
        MazeBuilder.Init(this);
        generator_is_ready = false;

        for (int i = 0; i < maps.Length; i++) {
            maps[i].Clear();
        }
    }

    // network event for master only
    public void OnTreasureGathered() {
        mazeChestsAmountGathered++;
        MazeUI.UILog($"OnTreasureGathered, mazeChestsAmountGathered: {mazeChestsAmountGathered} / {mazeChestsAmount}");

        network_event = "";
        if (mazeChestsAmountGathered >= mazeChestsAmount) {
            if (Networking.IsOwner(gameObject)) {
                MazeUI.UILog($"requesting NextLevel by owner, level: {level} => {level + 1}");
                level++;
                seed = Random.Range(0, 9999);
                MazeUI.UILog($"- new seed generated by owner = {seed}");
                network_event = nameof(NextLevel);
                NextLevel(); // direct event call for owner of this
            }
        }
        UpdateProgressText();
        RequestSerialization(); // send event to late-joiners (works only for owner of this)
    }

    public void ClearQVPens() {
        if (level <= 1) return;
        foreach (QvPen_PenManager penManager in QV_PEN_Settings.penManagers)
            if (penManager)
                penManager.Clear();
        foreach (QvPen_PenManager penManager in QV_PEN_Settings.penManagers)
            if (penManager)
                penManager.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(QvPen_PenManager.ResetPen));
        foreach (QvPen_EraserManager eraserManager in QV_PEN_Settings.eraserManagers)
            if (eraserManager)
                eraserManager.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(QvPen_EraserManager.ResetEraser));
    }

    public void Update() {
        syncDataUI.Clear();

        if (!generator_is_ready) {
            generator_is_ready = MazeGenerator.Generate();

            if (generator_is_ready) {
                genStopwatch.Stop();
                MazeUI.UILog($"Build Complete, " +
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
        if (!MazeBuilder.MazeReady && generator_is_ready) {
            if (MazeBuilder.BuildRoomsIter()) {
                // completed
                MazeUI.HideProgress();
                CentralZone.gameObject.SetActive(true);
                for (int i = 0; i < maps.Length; i++)
                    maps[i].Render(MazeGenerator);
            }
        }

        // send synd data to ui
        syncDataUI.AddText("Maze controler:");
        syncDataUI.AddText($"- {(Networking.IsOwner(gameObject) ? "Is owner!" : "Is secondary")}");
        syncDataUI.AddText($"- {nameof(mazeSize)} = {mazeSize}");
        syncDataUI.AddText($"- {nameof(mazeRoomsAmount)} = {mazeRoomsAmount}");
        syncDataUI.AddText($"- {nameof(mazeChestsAmount)} = {mazeChestsAmount}");
        syncDataUI.AddText($"- {nameof(level)} = {level}");
        syncDataUI.AddText($"- {nameof(mazeChestsAmountGathered)} = {mazeChestsAmountGathered}");
        syncDataUI.AddText($"- {nameof(seed)} = {seed}");
        syncDataUI.AddText($"- {nameof(network_event)} = {network_event}");
    }

    // event (test)
    public void SpawnEnemy() {
        if (Networking.LocalPlayer.IsOwner(gameObject)) {
            GameObject obj = Instantiate(enemyPrefab, enemySpawn);
            obj.transform.localPosition = Vector3.zero;
            BaseEnemy script = obj.GetComponent<BaseEnemy>();
            script.Init();
        }
    }

    public void UpdateProgressText() {
        progressText.text = "Bring treasures!" +
            $"\r\nLevel: {level}" +
            $"\r\nTreasures: {mazeChestsAmountGathered} / {mazeChestsAmount}";
    }
}
