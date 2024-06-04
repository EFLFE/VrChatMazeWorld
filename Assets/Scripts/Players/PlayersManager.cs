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

    public void Init(MazeController controller) {
        this.controller = controller;
        playersData = new PlayerData[playersContent.childCount];
        for (int i = 0; i < playersData.Length; i++) {
            PlayerData data = playersContent.GetChild(i).GetComponent<PlayerData>();
            data.Init(controller);
            playersData[i] = data;
        }

        // add all players
        var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);
        Debug.Log($"Found {players.Length} players");
        for (int i = 0; i < players.Length; i++) {
            AddPlayer(players[i]);
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
