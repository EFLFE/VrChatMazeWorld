using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class MazeUI : UdonSharpBehaviour {
    [Header("Player location")]
    [SerializeField] private RectTransform _playerLocationImage;
    [SerializeField] private Vector2 playerLocationImageScale;
    [SerializeField] private Vector2 playerLocationImageOffset;
    [Header("Progress bar")]
    [SerializeField] private GameObject loadProgressBarContent;
    [SerializeField] private Image loadProgressBar;

    private MazeController controller;
    private VRCPlayerApi localPlayer;

    public void Init(MazeController controller) {
        this.controller = controller;
        loadProgressBarContent.SetActive(false);
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

        posOnMap.x =   playerPos.x * playerLocationImageScale.x + playerLocationImageOffset.x;
        posOnMap.y = -(playerPos.z * playerLocationImageScale.y + playerLocationImageOffset.y);

        //posOnMap.x += controller.Builder.MazeSize * MazeBuilder.ROOMS_OFFSET / 2f + playerLocationImageOffset.x;
        //posOnMap.y -= controller.Builder.MazeSize * MazeBuilder.ROOMS_OFFSET / 2f + playerLocationImageOffset.y;

        _playerLocationImage.anchoredPosition = posOnMap;
    }

    public void HideProgress() {
        loadProgressBarContent.SetActive(false);
    }

    public void SetProgressValue(float perc) {
        if (!loadProgressBarContent.activeSelf)
            loadProgressBarContent.SetActive(true);
        loadProgressBar.fillAmount = perc;
    }
}
