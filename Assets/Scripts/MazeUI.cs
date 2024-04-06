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
    [Space]
    [SerializeField] private InputField _handXOffset;
    [SerializeField] private Toggle _noReload;

    private int logLines;
    private string logText;

    public float GetHandXOffset() {
        string a = _handXOffset.text;
        float.TryParse(a, out float x);
        return x;
    }

    public bool IsNoReload => _noReload.isOn;

    public void Init(MazeController controller) {
        loadProgressBarContent.SetActive(false);

        if (Networking.LocalPlayer.IsOwner(gameObject))
            Log("Is Owner");
    }

    public void HideProgress() {
        loadProgressBarContent.SetActive(false);
    }

    public void SetProgressValue(float perc) {
        if (!loadProgressBarContent.activeSelf)
            loadProgressBarContent.SetActive(true);
        loadProgressBar.fillAmount = perc;
    }

    public void Log(string text) {
        string[] textArr = text.Split('\n');

        for (int i = 0; i < textArr.Length; i++) {
            string item = textArr[i];
            Debug.Log(item);
            logText += $"[{DateTime.Now.ToString("HH:mm:ss")}] {item}\n";
            logLines++;
            if (logLines > 15) {
                logLines--;
                logText = logText.Remove(0, logText.IndexOf('\n') + 1);
            }
            _debugText.text = logText;
        }
    }
}
