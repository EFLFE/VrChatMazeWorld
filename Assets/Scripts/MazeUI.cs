using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class MazeUI : UdonSharpBehaviour {
    [Header("Player location")]
    [SerializeField] private RectTransform _playerLocationImage;
    [SerializeField] private Vector2 _playerLocationImageScale;
    [SerializeField] private Vector2 _playerLocationImageOffset;

    private MazeController controller;
    private VRCPlayerApi localPlayer;

    public void Init(MazeController controller) {
        this.controller = controller;
    }

    public void ManualUpdate() {
        if (controller == null)
            return;

        if (localPlayer != null && localPlayer.IsValid()) {
            UpdateLocation();
        } else {
            localPlayer = Networking.LocalPlayer;
        }
    }

    private void UpdateLocation() {
        // точка с центра? заменить текст на renderTraget/images?

        Vector3 playerPos = localPlayer.GetPosition();
        Vector2 posOnMap = Vector2.zero;

        posOnMap.x = playerPos.x / MazeBuilder.ROOM_SCALE * _playerLocationImageScale.x;
        posOnMap.y = -(playerPos.z / MazeBuilder.ROOM_SCALE * _playerLocationImageScale.y);

        posOnMap.x += controller.Builder.MazeSize * MazeBuilder.ROOM_SCALE / 2f + _playerLocationImageOffset.x;
        posOnMap.y -= controller.Builder.MazeSize * MazeBuilder.ROOM_SCALE / 2f + _playerLocationImageOffset.y;

        _playerLocationImage.anchoredPosition = posOnMap;
    }
}
