using System;
using System.Linq;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class MazeUI : UdonSharpBehaviour {
    [Header("Progress bar")]
    [SerializeField] private GameObject loadProgressBarContent;
    [SerializeField] private Image loadProgressBar;
    [SerializeField] private TextMeshProUGUI _debugText;
    [SerializeField] private int maxLogLines = 30;

    private int logLines;
    private string logText;
    private int curProg;
    private float maxProg;

    public void Init(MazeController controller) {
        loadProgressBarContent.SetActive(false);

        if (Networking.LocalPlayer.IsOwner(gameObject))
            UILog("Is Owner");
    }

    public void HideProgress() {
        loadProgressBarContent.SetActive(false);
    }

    public void SetProgressValue(float perc) {
        if (!loadProgressBarContent.activeSelf)
            loadProgressBarContent.SetActive(true);
        loadProgressBar.fillAmount = perc;
    }

    public void UILog(string text) {
        string[] textArr = text.Split('\n');

        for (int i = 0; i < textArr.Length; i++) {
            string item = textArr[i];
            Debug.Log(item);
            logText += $"[{DateTime.Now.ToString("HH:mm:ss")}] {item}\n";
            logLines++;
            if (logLines > maxLogLines) {
                logLines--;
                logText = logText.Remove(0, logText.IndexOf('\n') + 1);
            }
            _debugText.text = logText;
        }
    }
}
