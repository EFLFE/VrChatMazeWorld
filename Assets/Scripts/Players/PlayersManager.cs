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
    private bool localPlayerAdded;

    public override void OnDeserialization() {
        base.OnDeserialization();
        AddLocalPlayer();
    }

    public void Init(MazeController controller) {
        this.controller = controller;
        playersData = new PlayerData[playersContent.childCount];
        for (int i = 0; i < playersData.Length; i++) {
            PlayerData data = playersContent.GetChild(i).GetComponent<PlayerData>();
            data.Unbind();
            playersData[i] = data;
        }

        RequestSerialization();
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

    private void Clean(bool doFullSerialization) {
        for (int i = 0; i < playersData.Length; i++) {
            PlayerData data = playersData[i];
            if (data.IsLostPlayer()) {
                data.Unbind();
            }
        }

        if (doFullSerialization)
            FullSerialization();
    }

    private void AddLocalPlayer() {
        if (localPlayerAdded)
            return;

        Clean(true);
        int localPlayerID = Networking.LocalPlayer.playerId;

        if (HasPlayer(localPlayerID))
            return;

        for (int i = 0; i < playersData.Length; i++) {
            PlayerData data = playersData[i];
            if (!data.IsValid()) {
                data.BindPlayer(localPlayerID);
                localPlayerAdded = true;
                break;
            }
        }

        FullSerialization();
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

}
