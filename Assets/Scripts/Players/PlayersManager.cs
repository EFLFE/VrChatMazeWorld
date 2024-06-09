using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

/// <summary>
/// Хранит список активных игроков и их данные (PlayerData).
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class PlayersManager : UdonSharpBehaviour {
    [SerializeField] private Transform playersContent;

    private MazeController controller;
    private PlayerData[] playersData;
    private float refreshPlayersTimer;
    private VRCPlayerApi[] vrcPlayersBuffer;

    public PlayerData[] GetPlayers => playersData;

    public void Init(MazeController controller) {
        this.controller = controller;
        playersData = new PlayerData[playersContent.childCount];
        vrcPlayersBuffer = new VRCPlayerApi[playersContent.childCount];

        for (int i = 0; i < playersData.Length; i++) {
            PlayerData data = playersContent.GetChild(i).GetComponent<PlayerData>();
            data.Init(controller);
            playersData[i] = data;
        }

        // add all players
        RefreshAllPlayers();
        FullSerialization();
    }

    private void RefreshAllPlayers() {
        int playersCount = VRCPlayerApi.GetPlayerCount();
        VRCPlayerApi.GetPlayers(vrcPlayersBuffer);
        //Debug.Log($"Found {vrcPlayersBuffer.Length} players");
        for (int i = 0; i < playersCount; i++) {
            AddPlayer(vrcPlayersBuffer[i]);
        }
    }

    private void AddPlayer(VRCPlayerApi player) {
        if (HasPlayer(player.playerId))
            return;

        for (int i = 0; i < playersData.Length; i++) {
            PlayerData data = playersData[i];
            if (!data.IsValid()) {
                data.BindPlayer(player);
                break;
            }
        }
    }

    public void ManualUpdate() {
        refreshPlayersTimer += Time.deltaTime;
        if (refreshPlayersTimer >= 5f) {
            refreshPlayersTimer = 0f;
            Clean();
            RefreshAllPlayers();
            FullSerialization();
        }

        for (int i = 0; i < playersData.Length; i++) {
            playersData[i].ManualUpdate();
        }
    }

    public bool HasPlayer(int playerID) {
        for (int i = 0; i < playersData.Length; i++) {
            if (playersData[i].GetPlayerID == playerID)
                return true;
        }
        return false;
    }

    private void FullSerialization() {
        RequestSerialization();
        for (int i = 0; i < playersData.Length; i++) {
            playersData[i].RequestSerialization();
        }
    }

    private void Clean() {
        for (int i = 0; i < playersData.Length; i++) {
            PlayerData data = playersData[i];
            if (data.IsLostPlayer()) {
                data.Unbind();
            }
        }
    }

    public bool TryGetNearPlayer(Vector3 fromPos, out PlayerData playerData) {
        playerData = null;
        float minDist = float.MaxValue;

        for (int i = 0; i < playersData.Length; i++) {
            PlayerData data = playersData[i];
            if (data.IsValid()) {
                float dist = Vector3.Distance(data.GetGlobalPos, fromPos);
                if (dist < minDist) {
                    minDist = dist;
                    playerData = data;
                }
            }
        }

        return playerData != null;
    }

    public PlayerData GetLocalPlayer() {
        int localPlayerID = Networking.LocalPlayer.playerId;
        for (int i = 0; i < playersData.Length; i++) {
            PlayerData data = playersData[i];
            if (data.GetPlayerID == localPlayerID)
                return data;
        }
        Debug.LogError($"Local player not found!");
        return null;
    }

}
