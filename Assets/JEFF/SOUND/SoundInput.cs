using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

public class SoundInput : MonoBehaviour
{
    [Header("语音指令持续时间")]
    [SerializeField] private float commandDuration = 0.5f;  // 一次指令产生的移动脉冲时长

    // 公共属性，PlayerController 每帧读取
    public Vector3 voiceDirection { get; private set; }

#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
    private KeywordRecognizer keywordRecognizer;
    private Dictionary<string, Action> actionMap;
#endif
    private PlayerController playerController;
    private float voiceTimer;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        string forward = PlayerPrefs.GetString("Voice_Forward", "forward").ToLower();
        string back = PlayerPrefs.GetString("Voice_Back", "back").ToLower();
        string up = PlayerPrefs.GetString("Voice_Up", "up").ToLower();
        string down = PlayerPrefs.GetString("Voice_Down", "down").ToLower();

#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
        actionMap = new Dictionary<string, Action>
        {
            { forward, () => SetVoiceInput(Vector3.forward) },
            { back,    () => SetVoiceInput(Vector3.back) },
            { up,      () => SetVoiceInput(Vector3.up) },
            { down,    () => SetVoiceInput(Vector3.down) }
        };

        keywordRecognizer = new KeywordRecognizer(actionMap.Keys.ToArray());
        keywordRecognizer.OnPhraseRecognized += OnRecognized;
        keywordRecognizer.Start();
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
    private void OnRecognized(PhraseRecognizedEventArgs args)
    {
        if (actionMap.TryGetValue(args.text, out var action))
            action.Invoke();
    }
#endif

    private void SetVoiceInput(Vector3 direction)
    {
        voiceDirection = direction;
        voiceTimer = commandDuration;
    }

    void Update()
    {
        if (voiceTimer > 0)
        {
            voiceTimer -= Time.deltaTime;
            if (voiceTimer <= 0)
                voiceDirection = Vector3.zero;
        }
    }

    void OnDestroy()
    {
#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
        if (keywordRecognizer != null)
        {
            keywordRecognizer.Stop();
            keywordRecognizer.Dispose();
        }
#endif
    }
}