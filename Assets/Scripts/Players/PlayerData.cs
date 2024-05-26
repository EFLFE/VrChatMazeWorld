using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class PlayerData : UdonSharpBehaviour {
    [UdonSynced] private int playerID;

    private MazeController controller;
    private VRCPlayerApi playerApi;
    private Vector3 globalPos;
    private Vector3Int gridPos;

    /// <summary>
    /// Get player or null (depend on saved player ID).
    /// </summary>
    public VRCPlayerApi GetPlayerApi => playerApi;
    public int GetPlayerID => playerID;
    public Vector3 GetGlobalPos => globalPos;
    public Vector3Int GetGridPos => gridPos;

    public void Init(MazeController controller) {
        this.controller = controller;
        playerID = -1;
    }

    public bool IsValid() {
        if (playerID == -1)
            return false;
        return playerApi != null || playerApi.IsValid();
    }

    /// <summary>
    /// Has player id but player not valid.
    /// </summary>
    public bool IsLostPlayer() {
        if (playerID == -1)
            return false;
        return playerApi == null || !playerApi.IsValid();
    }

    /// <summary>
    /// Bind player by <see cref="VRCPlayerApi.playerId"/>.
    /// </summary>
    public void BindPlayer(int playerID) {
        this.playerID = playerID;
        playerApi = VRCPlayerApi.GetPlayerById(playerID);
    }

    public void Unbind() {
        playerID = -1;
        playerApi = null;
    }

    public void ManualUpdate() {
        if (playerApi == null || !playerApi.IsValid())
            return;

        Vector3 pos = playerApi.GetPosition();
        globalPos = pos;
        float halfSize = controller.MazeGenerator.Size / 2f;
        float halfHeight = controller.MazeGenerator.Height / 2f;
        gridPos.x = (int)(halfSize + pos.x / MazeBuilder.ROOMS_OFFSET);
        gridPos.y = (int)(halfSize + pos.z / MazeBuilder.ROOMS_OFFSET);
        gridPos.z = (int)(halfHeight + pos.y / MazeBuilder.ROOMS_OFFSET + 1f);
    }

}
