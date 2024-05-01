﻿using QvPen.UdonScript;
using System.Diagnostics;
using UdonSharp;
using UnityEngine;
using VRC.Economy;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using static VRC.Udon.Common.Interfaces.NetworkEventTarget;

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
    private int last_level = 0; // используется для контроля запуска NextLevel() на лейт-джойнерах
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

    [SerializeField] private AudioSource[] next_level_variants;

    // called only on late-joiners (not master of this object)
    public override void OnDeserialization() {
        MazeUI.UILog(
            $"MazeController OnDeserialization, " +
            $"\n- seed = {seed}" +
            $"\n- level = {level}" +
            $"\n- chests: {mazeChestsAmountGathered} / {mazeChestsAmount}"
        );
        base.OnDeserialization();

        if (last_level != level) {
            last_level = level;
            NextLevel();
        }

        UpdateProgressText();
    }

    private void Start() {
        genStopwatch = new Stopwatch();

        chestPool.Init(this);
        MazeBuilder.Init(this);
        MazeUI.Init(this);
        for (int i = 0; i < maps.Length; i++)
            maps[i].Init(this);

        if (Networking.IsOwner(gameObject)) {
            seed = startedSeed == 0 ? Random.Range(0, 999999) : startedSeed;
            level = startedLevel;
            MazeUI.UILog($"First seed generated by owner = {seed}");
            RequestSerialization(); // works only for owner of this
            NextLevel();
        } else {
            // late joiners will build via OnDeserialization
        }
        UpdateProgressText();
    }

    private void ReturnAllPlayersToStartRoom() {
        MazeUI.UILog($"ReturnAllPlayersToStartRoom()");
        //ReturnLocalPlayerToStartRoom();
        SendCustomNetworkEvent(All, nameof(ReturnLocalPlayerToStartRoom));
    }

    public void ReturnLocalPlayerToStartRoom() {
        MazeUI.UILog($"ReturnLocalPlayerToStartRoom()");
        MazeUI.UILog($"- my position: {Networking.LocalPlayer.GetPosition()}");
        if (Mathf.Abs(Networking.LocalPlayer.GetPosition().x) < 10.0f && Mathf.Abs(Networking.LocalPlayer.GetPosition().z) < 10.0f)
            return;
        Networking.LocalPlayer.TeleportTo(Vector3.zero, Networking.LocalPlayer.GetRotation());
    }

    private void NextLevel() {

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

        if (level > 1) {
            int next_level_variant_index = level % next_level_variants.Length;
            next_level_variants[next_level_variant_index].Play();
        }
    }

    public void Build() {
        MazeUI.UILog($"Build, seed: {seed}, level: {level}");

        MazeUI.UILog($"CentralZone - deactivating");
        CentralZone.gameObject.SetActive(false);

        ClearQVPens();
        genStopwatch.Restart();
        MazeUI.UILog($"MazeGenerator Init, mazeSize: {mazeSize}, mazeRoomsAmount: {mazeRoomsAmount}, mazeChestsAmount: {mazeChestsAmount}");
        MazeGenerator.Init(seed, mazeSize, mazeRoomsAmount, mazeChestsAmount);
        MazeBuilder.Init(this);
        generator_is_ready = false;

        for (int i = 0; i < maps.Length; i++) {
            maps[i].Clear();
        }
    }

    // local (not-network) event for master only
    public void OnTreasureGathered() {
        mazeChestsAmountGathered++;
        MazeUI.UILog($"OnTreasureGathered, mazeChestsAmountGathered: {mazeChestsAmountGathered} / {mazeChestsAmount}");

        if (mazeChestsAmountGathered >= mazeChestsAmount) {
            if (Networking.IsOwner(gameObject)) {

                MazeUI.UILog($"requesting NextLevel by owner, level: {level} => {level + 1}");
                ReturnAllPlayersToStartRoom();

                level++;
                mazeChestsAmountGathered = 0;
                seed = Random.Range(0, 999999);
                MazeUI.UILog($"- new seed generated by owner = {seed}");

                NextLevel(); // direct event call for owner of this
            }
        }
        UpdateProgressText();
        RequestSerialization(); // send event to late-joiners (works only for owner of this)
    }

    public void ClearQVPens() {
        if (level <= startedLevel) return;
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
                MazeUI.UILog($"CentralZone - activating");
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
        syncDataUI.AddText("Other:");
        syncDataUI.AddText($"- lang = {VRCPlayerApi.GetCurrentLanguage()}");
        //syncDataUI.AddText($"- my position: {Networking.LocalPlayer.GetPosition()}");
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
        progressText.text = $"Bring treasures!" +
            $"\r\n Level: {level}" +
            $"\r\n Treasures: {mazeChestsAmountGathered} / {mazeChestsAmount}";

        if (VRCPlayerApi.GetCurrentLanguage() == "ru") {
            progressText.text = "Принеси сокровища!" +
                $"\r\n Уровень: {level}" +
                $"\r\n Сокровища: {mazeChestsAmountGathered} / {mazeChestsAmount}";
        }
    }
}
