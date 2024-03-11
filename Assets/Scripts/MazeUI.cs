using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class MazeUI : UdonSharpBehaviour {
    [Header("Progress bar")]
    [SerializeField] private GameObject loadProgressBarContent;
    [SerializeField] private Image loadProgressBar;

    private MazeController controller;

    public void Init(MazeController controller) {
        this.controller = controller;
        loadProgressBarContent.SetActive(false);
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
