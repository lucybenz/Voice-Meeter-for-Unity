using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioControl
{
    /// <summary>
    /// 音频控制日志系统
    /// 记录模式切换历史，便于调试
    /// </summary>
    public class AudioLogger
    {
        public enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error
        }

        public struct LogEntry
        {
            public DateTime Timestamp;
            public LogLevel Level;
            public string Message;
            public string Context;

            public override string ToString()
            {
                return $"[{Timestamp:HH:mm:ss.fff}] [{Level}] {Message}";
            }
        }

        private static AudioLogger _instance;
        public static AudioLogger Instance => _instance ??= new AudioLogger();

        private readonly List<LogEntry> _logs = new List<LogEntry>();
        private readonly int _maxLogCount;
        private bool _enableUnityLog;

        public IReadOnlyList<LogEntry> Logs => _logs;
        public event Action<LogEntry> OnLogAdded;

        public bool EnableUnityLog
        {
            get => _enableUnityLog;
            set => _enableUnityLog = value;
        }

        public AudioLogger(int maxLogCount = 1000, bool enableUnityLog = true)
        {
            _maxLogCount = maxLogCount;
            _enableUnityLog = enableUnityLog;
        }

        public void Log(LogLevel level, string message, string context = "AudioControl")
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                Context = context
            };

            _logs.Add(entry);

            // 限制日志数量
            if (_logs.Count > _maxLogCount)
            {
                _logs.RemoveAt(0);
            }

            // 输出到 Unity Console
            if (_enableUnityLog)
            {
                string formattedMessage = $"[{context}] {message}";
                switch (level)
                {
                    case LogLevel.Debug:
                    case LogLevel.Info:
                        UnityEngine.Debug.Log(formattedMessage);
                        break;
                    case LogLevel.Warning:
                        UnityEngine.Debug.LogWarning(formattedMessage);
                        break;
                    case LogLevel.Error:
                        UnityEngine.Debug.LogError(formattedMessage);
                        break;
                }
            }

            OnLogAdded?.Invoke(entry);
        }

        public void Debug(string message, string context = "AudioControl") => Log(LogLevel.Debug, message, context);
        public void Info(string message, string context = "AudioControl") => Log(LogLevel.Info, message, context);
        public void Warning(string message, string context = "AudioControl") => Log(LogLevel.Warning, message, context);
        public void Error(string message, string context = "AudioControl") => Log(LogLevel.Error, message, context);

        public void LogModeChange(AudioOutputMode from, AudioOutputMode to)
        {
            Info($"Mode changed: {from} -> {to}", "ModeSwitch");
        }

        public void LogConnectionChange(bool connected)
        {
            Info($"VoiceMeeter connection: {(connected ? "Connected" : "Disconnected")}", "Connection");
        }

        public void Clear()
        {
            _logs.Clear();
        }

        /// <summary>
        /// 导出日志为字符串
        /// </summary>
        public string Export()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== Audio Control Log Export ===");
            sb.AppendLine($"Export Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Total Entries: {_logs.Count}");
            sb.AppendLine("================================");
            sb.AppendLine();

            foreach (var log in _logs)
            {
                sb.AppendLine(log.ToString());
            }

            return sb.ToString();
        }
    }
}
