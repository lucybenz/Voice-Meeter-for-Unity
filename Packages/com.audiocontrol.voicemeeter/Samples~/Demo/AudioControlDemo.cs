using UnityEngine;

namespace AudioControl.Demo
{
    /// <summary>
    /// 音频控制演示脚本
    /// 提供 UI 和键盘控制用于测试
    /// </summary>
    public class AudioControlDemo : MonoBehaviour
    {
        [Header("音频设置")]
        [Tooltip("拖入音频文件（MP3/WAV）")]
        [SerializeField] private AudioClip _audioClip;

        [Tooltip("启动时自动播放")]
        [SerializeField] private bool _playOnStart = true;

        [Tooltip("循环播放")]
        [SerializeField] private bool _loop = true;

        private AudioSource _audioSource;

        [Header("快捷键设置")]
        [SerializeField] private KeyCode _modeAKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode _modeBKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode _toggleKey = KeyCode.Space;
        [SerializeField] private KeyCode _playPauseKey = KeyCode.P;

        private AudioOutputController _controller;

        private void Start()
        {
            _controller = AudioOutputController.Instance;

            _controller.OnModeChanged += OnModeChanged;
            _controller.OnConnectionChanged += OnConnectionChanged;
            _controller.OnReconnecting += OnReconnecting;
            _controller.OnReconnected += OnReconnected;

            SetupAudioSource();

            AudioLogger.Instance.Info("Demo started. Press 1/2/Space to switch modes, P to play/pause.");
        }

        private void SetupAudioSource()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.clip = _audioClip;
            _audioSource.loop = _loop;
            _audioSource.playOnAwake = false;

            if (_playOnStart && _audioClip != null)
            {
                _audioSource.Play();
            }
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnModeChanged -= OnModeChanged;
                _controller.OnConnectionChanged -= OnConnectionChanged;
                _controller.OnReconnecting -= OnReconnecting;
                _controller.OnReconnected -= OnReconnected;
            }
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(_modeAKey))
            {
                _controller.SetModeA();
            }
            else if (Input.GetKeyDown(_modeBKey))
            {
                _controller.SetModeB();
            }
            else if (Input.GetKeyDown(_toggleKey))
            {
                _controller.ToggleMode();
            }
            else if (Input.GetKeyDown(_playPauseKey) && _audioSource != null)
            {
                if (_audioSource.isPlaying)
                    _audioSource.Pause();
                else
                    _audioSource.Play();
            }
        }

        private void OnModeChanged(AudioOutputMode newMode)
        {
            AudioLogger.Instance.Debug($"Demo received mode change: {newMode}");
        }

        private void OnConnectionChanged(bool connected)
        {
            AudioLogger.Instance.Debug($"Demo received connection change: {connected}");
        }

        private void OnReconnecting()
        {
            AudioLogger.Instance.Debug("Demo: Reconnecting...");
        }

        private void OnReconnected()
        {
            AudioLogger.Instance.Debug("Demo: Reconnected!");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 320, 280));
            GUILayout.BeginVertical("box");

            // 标题
            var titleStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 };
            GUILayout.Label("<b>VoiceMeeter Audio Controller</b>", titleStyle);
            GUILayout.Space(5);

            // 连接状态
            var richStyle = new GUIStyle(GUI.skin.label) { richText = true };
            string connectionStatus;
            if (_controller.IsReconnecting)
            {
                connectionStatus = $"<color=yellow>Reconnecting... ({_controller.ReconnectAttempts})</color>";
            }
            else if (_controller.IsConnected)
            {
                connectionStatus = "<color=green>Connected</color>";
            }
            else
            {
                connectionStatus = "<color=red>Disconnected</color>";
            }
            GUILayout.Label($"Status: {connectionStatus}", richStyle);

            // VoiceMeeter 类型
            if (_controller.IsConnected)
            {
                GUILayout.Label($"Type: {VoiceMeeterAPI.GetVoiceMeeterTypeName()}");
            }

            // 当前模式
            string modeText = _controller.CurrentMode == AudioOutputMode.A_HeadphoneOnly
                ? "<color=cyan>A - Headphone Only</color>"
                : "<color=orange>B - Headphone + Speaker</color>";
            GUILayout.Label($"Mode: {modeText}", richStyle);

            GUILayout.Space(10);

            // 模式按钮
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Mode A (1)", GUILayout.Height(35)))
            {
                _controller.SetModeA();
            }
            if (GUILayout.Button("Mode B (2)", GUILayout.Height(35)))
            {
                _controller.SetModeB();
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Toggle Mode (Space)", GUILayout.Height(30)))
            {
                _controller.ToggleMode();
            }

            GUILayout.Space(5);

            // 连接控制
            GUILayout.BeginHorizontal();
            if (!_controller.IsConnected)
            {
                if (GUILayout.Button("Connect"))
                {
                    _controller.Connect();
                }
            }
            else
            {
                if (GUILayout.Button("Disconnect"))
                {
                    _controller.Disconnect();
                }
                if (GUILayout.Button("Sync"))
                {
                    _controller.SyncFromVoiceMeeter();
                }
            }
            GUILayout.EndHorizontal();

            // 音频控制
            if (_audioSource != null && _audioClip != null)
            {
                GUILayout.Space(5);
                string playStatus = _audioSource.isPlaying ? "Playing" : "Paused";
                if (GUILayout.Button($"Audio: {playStatus} (P)", GUILayout.Height(25)))
                {
                    if (_audioSource.isPlaying)
                        _audioSource.Pause();
                    else
                        _audioSource.Play();
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
