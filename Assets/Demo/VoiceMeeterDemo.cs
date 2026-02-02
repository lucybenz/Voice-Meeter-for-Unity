using UnityEngine;
using AudioControl;

/// <summary>
/// VoiceMeeter 音频控制插件使用示例
/// 将此脚本挂载到场景中任意 GameObject
/// </summary>
public class VoiceMeeterDemo : MonoBehaviour
{
    [Header("音频设置")]
    [Tooltip("拖入音频文件")]
    [SerializeField] private AudioClip audioClip;

    [Tooltip("启动时自动播放")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("循环播放")]
    [SerializeField] private bool loop = true;

    [Header("快捷键")]
    [SerializeField] private KeyCode modeAKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode modeBKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode modeCKey = KeyCode.Alpha3;

    private AudioSource audioSource;
    private AudioOutputController controller;

    void Start()
    {
        // 获取控制器实例
        controller = AudioOutputController.Instance;

        // 监听事件
        controller.OnModeChanged += HandleModeChanged;
        controller.OnConnectionChanged += HandleConnectionChanged;
        controller.OnReconnected += HandleReconnected;

        // 设置音频
        SetupAudio();
    }

    void SetupAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = audioClip;
        audioSource.loop = loop;
        audioSource.playOnAwake = false;

        if (playOnStart && audioClip != null)
        {
            audioSource.Play();
        }
    }

    void Update()
    {
        // 键盘控制
        if (Input.GetKeyDown(modeAKey))
        {
            controller.SetModeA();
        }
        else if (Input.GetKeyDown(modeBKey))
        {
            controller.SetModeB();
        }
        else if (Input.GetKeyDown(modeCKey))
        {
            controller.SetModeC();
        }
        else if (Input.GetKeyDown(KeyCode.P) && audioSource != null)
        {
            if (audioSource.isPlaying)
                audioSource.Pause();
            else
                audioSource.Play();
        }
    }

    void OnDestroy()
    {
        if (controller != null)
        {
            controller.OnModeChanged -= HandleModeChanged;
            controller.OnConnectionChanged -= HandleConnectionChanged;
            controller.OnReconnected -= HandleReconnected;
        }
    }

    // 事件处理
    void HandleModeChanged(AudioOutputMode mode)
    {
        Debug.Log($"[Demo] 模式切换: {mode}");
    }

    void HandleConnectionChanged(bool connected)
    {
        Debug.Log($"[Demo] 连接状态: {(connected ? "已连接" : "已断开")}");
    }

    void HandleReconnected()
    {
        Debug.Log("[Demo] 重连成功!");
    }

    // UI 显示
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 400, 350));
        GUILayout.BeginVertical("box");

        // 标题
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };
        GUILayout.Label("VoiceMeeter Audio Controller Demo", titleStyle);
        GUILayout.Space(10);

        // 连接状态
        GUIStyle statusStyle = new GUIStyle(GUI.skin.label) { richText = true };
        string status = controller.IsConnected
            ? "<color=green>● 已连接</color>"
            : "<color=red>● 未连接</color>";

        if (controller.IsReconnecting)
        {
            status = $"<color=yellow>● 重连中... ({controller.ReconnectAttempts})</color>";
        }

        GUILayout.Label($"状态: {status}", statusStyle);

        if (controller.IsConnected)
        {
            GUILayout.Label($"VoiceMeeter: {VoiceMeeterAPI.GetVoiceMeeterTypeName()}");
        }

        GUILayout.Space(5);

        // 当前模式
        string modeColor;
        string modeName;
        switch (controller.CurrentMode)
        {
            case AudioOutputMode.A_HeadphoneOnly:
                modeColor = "cyan";
                modeName = "A - 仅耳机";
                break;
            case AudioOutputMode.B_HeadphoneAndSpeaker:
                modeColor = "orange";
                modeName = "B - 耳机+音箱";
                break;
            case AudioOutputMode.C_SpeakerOnly:
                modeColor = "lime";
                modeName = "C - 仅音箱";
                break;
            default:
                modeColor = "white";
                modeName = "未知";
                break;
        }
        GUILayout.Label($"当前模式: <color={modeColor}>{modeName}</color>", statusStyle);

        GUILayout.Space(15);

        // 模式切换按钮
        GUILayout.Label("模式切换:");
        GUILayout.BeginHorizontal();

        GUI.backgroundColor = controller.CurrentMode == AudioOutputMode.A_HeadphoneOnly
            ? Color.cyan : Color.white;
        if (GUILayout.Button("A 模式 (1)\n仅耳机", GUILayout.Height(50)))
        {
            controller.SetModeA();
        }

        GUI.backgroundColor = controller.CurrentMode == AudioOutputMode.B_HeadphoneAndSpeaker
            ? new Color(1f, 0.6f, 0f) : Color.white;
        if (GUILayout.Button("B 模式 (2)\n耳机+音箱", GUILayout.Height(50)))
        {
            controller.SetModeB();
        }

        GUI.backgroundColor = controller.CurrentMode == AudioOutputMode.C_SpeakerOnly
            ? Color.green : Color.white;
        if (GUILayout.Button("C 模式 (3)\n仅音箱", GUILayout.Height(50)))
        {
            controller.SetModeC();
        }

        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 连接控制
        GUILayout.Label("连接控制:");
        GUILayout.BeginHorizontal();
        if (!controller.IsConnected)
        {
            if (GUILayout.Button("连接", GUILayout.Height(30)))
            {
                controller.Connect();
            }
        }
        else
        {
            if (GUILayout.Button("断开", GUILayout.Height(30)))
            {
                controller.Disconnect();
            }
            if (GUILayout.Button("同步状态", GUILayout.Height(30)))
            {
                controller.SyncFromVoiceMeeter();
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 音频控制
        if (audioSource != null && audioClip != null)
        {
            GUILayout.Label("音频控制:");
            string playText = audioSource.isPlaying ? "暂停 (P)" : "播放 (P)";
            if (GUILayout.Button(playText, GUILayout.Height(30)))
            {
                if (audioSource.isPlaying)
                    audioSource.Pause();
                else
                    audioSource.Play();
            }
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();

        // 快捷键提示
        GUILayout.BeginArea(new Rect(10, Screen.height - 60, 400, 50));
        GUILayout.Label("快捷键: 1=A模式  2=B模式  3=C模式  P=播放/暂停");
        GUILayout.EndArea();
    }
}
