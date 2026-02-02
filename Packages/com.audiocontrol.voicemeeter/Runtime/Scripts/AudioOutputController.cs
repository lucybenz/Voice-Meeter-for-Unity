using System;
using UnityEngine;

namespace AudioControl
{
    /// <summary>
    /// 音频输出控制器
    /// 管理 A/B 模式切换，支持自动重连
    /// </summary>
    public class AudioOutputController : MonoBehaviour
    {
        #region Singleton

        private static AudioOutputController _instance;
        public static AudioOutputController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AudioOutputController>();
                    if (_instance == null)
                    {
                        var go = new GameObject("AudioOutputController");
                        _instance = go.AddComponent<AudioOutputController>();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Events

        public event Action<AudioOutputMode> OnModeChanged;
        public event Action<bool> OnConnectionChanged;
        public event Action OnReconnecting;
        public event Action OnReconnected;

        #endregion

        #region Inspector Fields

        [Header("VoiceMeeter 配置")]
        [Tooltip("VoiceMeeter Input (VAIO) 对应的 Strip 索引，Banana 中为 3")]
        [SerializeField] private int _stripIndex = 3;

        [Tooltip("启动时自动连接 VoiceMeeter")]
        [SerializeField] private bool _autoConnectOnStart = true;

        [Tooltip("启动时的默认模式")]
        [SerializeField] private AudioOutputMode _defaultMode = AudioOutputMode.A_HeadphoneOnly;

        [Header("自动重连")]
        [Tooltip("启用自动重连")]
        [SerializeField] private bool _enableAutoReconnect = true;

        [Tooltip("连接检查间隔（秒）")]
        [SerializeField] private float _connectionCheckInterval = 5f;

        [Tooltip("重连尝试间隔（秒）")]
        [SerializeField] private float _reconnectInterval = 3f;

        [Tooltip("最大重连尝试次数（0=无限）")]
        [SerializeField] private int _maxReconnectAttempts = 0;

        [Header("状态（只读）")]
        [SerializeField] private AudioOutputMode _currentMode;
        [SerializeField] private bool _isConnected;
        [SerializeField] private int _reconnectAttempts;

        #endregion

        #region Private Fields

        private float _lastConnectionCheck;
        private float _lastReconnectAttempt;
        private bool _isReconnecting;

        #endregion

        #region Properties

        public AudioOutputMode CurrentMode => _currentMode;
        public bool IsConnected => _isConnected;
        public int StripIndex
        {
            get => _stripIndex;
            set => _stripIndex = value;
        }
        public bool EnableAutoReconnect
        {
            get => _enableAutoReconnect;
            set => _enableAutoReconnect = value;
        }
        public bool IsReconnecting => _isReconnecting;
        public int ReconnectAttempts => _reconnectAttempts;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_autoConnectOnStart)
            {
                Connect();
                if (_isConnected)
                {
                    SetMode(_defaultMode);
                }
            }
        }

        private void Update()
        {
            if (!_enableAutoReconnect) return;

            // 定期检查连接状态
            if (_isConnected && Time.time - _lastConnectionCheck > _connectionCheckInterval)
            {
                _lastConnectionCheck = Time.time;
                CheckConnectionStatus();
            }

            // 尝试重连
            if (_isReconnecting && Time.time - _lastReconnectAttempt > _reconnectInterval)
            {
                _lastReconnectAttempt = Time.time;
                TryReconnect();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                Disconnect();
                _instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            _enableAutoReconnect = false; // 防止退出时重连
            Disconnect();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 连接到 VoiceMeeter
        /// </summary>
        public bool Connect()
        {
            if (_isConnected)
            {
                AudioLogger.Instance.Warning("Already connected.");
                return true;
            }

            _isConnected = VoiceMeeterAPI.Login();

            if (_isConnected)
            {
                _isReconnecting = false;
                _reconnectAttempts = 0;
                string typeName = VoiceMeeterAPI.GetVoiceMeeterTypeName();
                AudioLogger.Instance.Info($"Connected to {typeName}");
                AudioLogger.Instance.LogConnectionChange(true);
            }

            OnConnectionChanged?.Invoke(_isConnected);
            return _isConnected;
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (!_isConnected) return;

            VoiceMeeterAPI.Logout();
            _isConnected = false;
            _isReconnecting = false;
            AudioLogger.Instance.LogConnectionChange(false);
            OnConnectionChanged?.Invoke(false);
        }

        /// <summary>
        /// 设置输出模式
        /// </summary>
        public bool SetMode(AudioOutputMode mode)
        {
            if (!_isConnected)
            {
                AudioLogger.Instance.Error("Not connected to VoiceMeeter.");
                return false;
            }

            bool success = ApplyModeToVoiceMeeter(mode);

            if (success)
            {
                var previousMode = _currentMode;
                _currentMode = mode;

                if (previousMode != mode)
                {
                    AudioLogger.Instance.LogModeChange(previousMode, mode);
                    OnModeChanged?.Invoke(mode);
                }
            }

            return success;
        }

        /// <summary>
        /// 切换到 A 模式（仅耳机）
        /// </summary>
        public bool SetModeA() => SetMode(AudioOutputMode.A_HeadphoneOnly);

        /// <summary>
        /// 切换到 B 模式（耳机 + 音箱）
        /// </summary>
        public bool SetModeB() => SetMode(AudioOutputMode.B_HeadphoneAndSpeaker);

        /// <summary>
        /// 切换到 C 模式（仅音箱）
        /// </summary>
        public bool SetModeC() => SetMode(AudioOutputMode.C_SpeakerOnly);

        /// <summary>
        /// 切换模式
        /// </summary>
        public bool ToggleMode()
        {
            var newMode = _currentMode == AudioOutputMode.A_HeadphoneOnly
                ? AudioOutputMode.B_HeadphoneAndSpeaker
                : AudioOutputMode.A_HeadphoneOnly;

            return SetMode(newMode);
        }

        /// <summary>
        /// 从 VoiceMeeter 同步当前状态
        /// </summary>
        public void SyncFromVoiceMeeter()
        {
            if (!_isConnected) return;

            bool a1 = VoiceMeeterAPI.GetStripA1(_stripIndex);
            bool a2 = VoiceMeeterAPI.GetStripA2(_stripIndex);

            if (a1 && !a2)
            {
                _currentMode = AudioOutputMode.A_HeadphoneOnly;
            }
            else if (a1 && a2)
            {
                _currentMode = AudioOutputMode.B_HeadphoneAndSpeaker;
            }
            else if (!a1 && a2)
            {
                _currentMode = AudioOutputMode.C_SpeakerOnly;
            }

            AudioLogger.Instance.Debug($"Synced from VoiceMeeter: A1={a1}, A2={a2}, Mode={_currentMode}");
        }

        /// <summary>
        /// 手动触发重连
        /// </summary>
        public void ForceReconnect()
        {
            _isConnected = false;
            _isReconnecting = true;
            _reconnectAttempts = 0;
            OnReconnecting?.Invoke();
            TryReconnect();
        }

        #endregion

        #region Private Methods

        private bool ApplyModeToVoiceMeeter(AudioOutputMode mode)
        {
            bool success = true;

            switch (mode)
            {
                case AudioOutputMode.A_HeadphoneOnly:
                    success &= VoiceMeeterAPI.SetStripA1(_stripIndex, true);
                    success &= VoiceMeeterAPI.SetStripA2(_stripIndex, false);
                    break;

                case AudioOutputMode.B_HeadphoneAndSpeaker:
                    success &= VoiceMeeterAPI.SetStripA1(_stripIndex, true);
                    success &= VoiceMeeterAPI.SetStripA2(_stripIndex, true);
                    break;

                case AudioOutputMode.C_SpeakerOnly:
                    success &= VoiceMeeterAPI.SetStripA1(_stripIndex, false);
                    success &= VoiceMeeterAPI.SetStripA2(_stripIndex, true);
                    break;
            }

            return success;
        }

        private void CheckConnectionStatus()
        {
            bool stillConnected = VoiceMeeterAPI.CheckConnection();

            if (!stillConnected && _isConnected)
            {
                AudioLogger.Instance.Warning("Connection lost to VoiceMeeter.");
                _isConnected = false;
                OnConnectionChanged?.Invoke(false);

                if (_enableAutoReconnect)
                {
                    StartReconnect();
                }
            }
        }

        private void StartReconnect()
        {
            if (_isReconnecting) return;

            _isReconnecting = true;
            _reconnectAttempts = 0;
            _lastReconnectAttempt = Time.time;
            AudioLogger.Instance.Info("Starting reconnection...");
            OnReconnecting?.Invoke();
        }

        private void TryReconnect()
        {
            _reconnectAttempts++;

            if (_maxReconnectAttempts > 0 && _reconnectAttempts > _maxReconnectAttempts)
            {
                AudioLogger.Instance.Error($"Max reconnect attempts ({_maxReconnectAttempts}) reached.");
                _isReconnecting = false;
                return;
            }

            AudioLogger.Instance.Info($"Reconnect attempt {_reconnectAttempts}...");

            if (VoiceMeeterAPI.Login())
            {
                _isConnected = true;
                _isReconnecting = false;
                AudioLogger.Instance.Info("Reconnected successfully!");
                AudioLogger.Instance.LogConnectionChange(true);
                OnConnectionChanged?.Invoke(true);
                OnReconnected?.Invoke();

                // 恢复之前的模式
                SetMode(_currentMode);
            }
        }

        #endregion
    }
}
