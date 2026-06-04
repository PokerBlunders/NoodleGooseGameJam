using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

public class SoundRecerd : MonoBehaviour
{
    [Header("UI 文字")]
    public Text statusText;
    public Text resultText;

    private string[] actionNames = { "前进", "后退", "上升", "下降" };
    private string[] saveKeys = { "Voice_Forward", "Voice_Back", "Voice_Up", "Voice_Down" };
    private int step = 0;

#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
    private DictationRecognizer recognizer;
#endif

    void Start()
    {
#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
        StartCoroutine(RecordingLoop());
#else
        statusText.text = "当前平台不支持语音设置";
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
    IEnumerator RecordingLoop()
    {
        while (step < actionNames.Length)
        {
            statusText.text = $"请说出“{actionNames[step]}”的指令词";
            resultText.text = "正在聆听...";

            recognizer = new DictationRecognizer();
            string recognized = "";

            recognizer.DictationResult += (text, confidence) =>
                recognized = text;
            recognizer.DictationHypothesis += (text) =>
                resultText.text = $"听到: {text}";

            recognizer.Start();
            yield return new WaitForSeconds(3f);    // 录制3秒
            recognizer.Stop();
            recognizer.Dispose();

            if (!string.IsNullOrEmpty(recognized))
            {
                string command = recognized.Trim().ToLower();
                resultText.text = $"已保存: {command}";
                PlayerPrefs.SetString(saveKeys[step], command);
            }
            else
            {
                resultText.text = "未识别，使用默认词";
                PlayerPrefs.SetString(saveKeys[step], actionNames[step]);
            }

            PlayerPrefs.Save();
            yield return new WaitForSeconds(1.5f);
            step++;
        }
        statusText.text = "设置完成！现在可以进入游戏。";
    }
#endif

    public void ResetToDefaults()
    {
        PlayerPrefs.SetString("Voice_Forward", "forward");
        PlayerPrefs.SetString("Voice_Back", "back");
        PlayerPrefs.SetString("Voice_Up", "up");
        PlayerPrefs.SetString("Voice_Down", "down");
        PlayerPrefs.Save();
        statusText.text = "已重置为英文指令";
    }
}