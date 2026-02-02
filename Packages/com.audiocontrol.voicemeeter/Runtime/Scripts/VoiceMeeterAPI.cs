using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AudioControl
{
    /// <summary>
    /// VoiceMeeter Remote API 封装
    /// 通过 P/Invoke 调用 VoicemeeterRemote64.dll
    /// </summary>
    public static class VoiceMeeterAPI
    {
        private const string DLL_NAME = "VoicemeeterRemote64";

        #region Native API Imports

        [DllImport(DLL_NAME)]
        private static extern int VBVMR_Login();

        [DllImport(DLL_NAME)]
        private static extern int VBVMR_Logout();

        [DllImport(DLL_NAME)]
        private static extern int VBVMR_SetParameterFloat(string paramName, float value);

        [DllImport(DLL_NAME)]
        private static extern int VBVMR_GetParameterFloat(string paramName, ref float value);

        [DllImport(DLL_NAME)]
        private static extern int VBVMR_IsParametersDirty();

        [DllImport(DLL_NAME)]
        private static extern int VBVMR_GetVoicemeeterType(ref int type);

        #endregion

        #region Connection State

        private static bool _isConnected = false;
        public static bool IsConnected => _isConnected;

        private static DateTime _lastConnectionCheck = DateTime.MinValue;
        private static bool _dllAvailable = true;

        #endregion

        #region Public Methods

        /// <summary>
        /// 检查 DLL 是否可用
        /// </summary>
        public static bool IsDllAvailable => _dllAvailable;

        /// <summary>
        /// 连接到 VoiceMeeter
        /// </summary>
        /// <returns>连接是否成功</returns>
        public static bool Login()
        {
            if (_isConnected)
            {
                return true;
            }

            if (!_dllAvailable)
            {
                return false;
            }

            try
            {
                int result = VBVMR_Login();
                // 0 = OK, 1 = OK but VoiceMeeter not running (will launch it)
                if (result == 0 || result == 1)
                {
                    _isConnected = true;
                    _lastConnectionCheck = DateTime.Now;
                    AudioLogger.Instance.Info($"Login successful. Result: {result}", "VoiceMeeterAPI");
                    return true;
                }
                else
                {
                    AudioLogger.Instance.Error($"Login failed. Error code: {result}", "VoiceMeeterAPI");
                    return false;
                }
            }
            catch (DllNotFoundException e)
            {
                _dllAvailable = false;
                AudioLogger.Instance.Error($"DLL not found: {e.Message}", "VoiceMeeterAPI");
                return false;
            }
            catch (Exception e)
            {
                AudioLogger.Instance.Error($"Login exception: {e.Message}", "VoiceMeeterAPI");
                return false;
            }
        }

        /// <summary>
        /// 断开与 VoiceMeeter 的连接
        /// </summary>
        public static void Logout()
        {
            if (!_isConnected)
            {
                return;
            }

            // 直接标记为断开，避免阻塞
            _isConnected = false;

            try
            {
                VBVMR_Logout();
                AudioLogger.Instance.Info("Logout successful.", "VoiceMeeterAPI");
            }
            catch (Exception e)
            {
                AudioLogger.Instance.Error($"Logout exception: {e.Message}", "VoiceMeeterAPI");
            }
        }

        /// <summary>
        /// 检查连接是否仍然有效
        /// </summary>
        public static bool CheckConnection()
        {
            if (!_isConnected || !_dllAvailable)
            {
                return false;
            }

            try
            {
                // 尝试获取 VoiceMeeter 类型来验证连接
                int type = 0;
                int result = VBVMR_GetVoicemeeterType(ref type);
                bool valid = result == 0 && type > 0;

                if (!valid)
                {
                    _isConnected = false;
                }

                _lastConnectionCheck = DateTime.Now;
                return valid;
            }
            catch
            {
                _isConnected = false;
                return false;
            }
        }

        /// <summary>
        /// 设置浮点参数
        /// </summary>
        public static bool SetParameter(string paramName, float value)
        {
            if (!_isConnected)
            {
                AudioLogger.Instance.Error("Not connected. Call Login() first.", "VoiceMeeterAPI");
                return false;
            }

            try
            {
                int result = VBVMR_SetParameterFloat(paramName, value);
                if (result != 0)
                {
                    AudioLogger.Instance.Error($"SetParameter failed for {paramName}. Error: {result}", "VoiceMeeterAPI");
                    return false;
                }
                AudioLogger.Instance.Debug($"SetParameter: {paramName} = {value}", "VoiceMeeterAPI");
                return true;
            }
            catch (Exception e)
            {
                AudioLogger.Instance.Error($"SetParameter exception: {e.Message}", "VoiceMeeterAPI");
                return false;
            }
        }

        /// <summary>
        /// 获取浮点参数
        /// </summary>
        public static bool GetParameter(string paramName, out float value)
        {
            value = 0f;

            if (!_isConnected)
            {
                return false;
            }

            try
            {
                float result = 0f;
                int code = VBVMR_GetParameterFloat(paramName, ref result);
                if (code != 0)
                {
                    return false;
                }
                value = result;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查参数是否有变化
        /// </summary>
        public static bool IsParametersDirty()
        {
            if (!_isConnected) return false;

            try
            {
                return VBVMR_IsParametersDirty() == 1;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取 VoiceMeeter 类型
        /// </summary>
        /// <returns>1=VoiceMeeter, 2=Banana, 3=Potato, 0=未知</returns>
        public static int GetVoiceMeeterType()
        {
            if (!_isConnected) return 0;

            try
            {
                int type = 0;
                VBVMR_GetVoicemeeterType(ref type);
                return type;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 获取 VoiceMeeter 类型名称
        /// </summary>
        public static string GetVoiceMeeterTypeName()
        {
            return GetVoiceMeeterType() switch
            {
                1 => "VoiceMeeter",
                2 => "VoiceMeeter Banana",
                3 => "VoiceMeeter Potato",
                _ => "Unknown"
            };
        }

        #endregion

        #region Strip Control Methods

        public static bool SetStripA1(int stripIndex, bool enabled)
        {
            return SetParameter($"Strip[{stripIndex}].A1", enabled ? 1f : 0f);
        }

        public static bool SetStripA2(int stripIndex, bool enabled)
        {
            return SetParameter($"Strip[{stripIndex}].A2", enabled ? 1f : 0f);
        }

        public static bool SetStripA3(int stripIndex, bool enabled)
        {
            return SetParameter($"Strip[{stripIndex}].A3", enabled ? 1f : 0f);
        }

        public static bool GetStripA1(int stripIndex)
        {
            return GetParameter($"Strip[{stripIndex}].A1", out float value) && value > 0.5f;
        }

        public static bool GetStripA2(int stripIndex)
        {
            return GetParameter($"Strip[{stripIndex}].A2", out float value) && value > 0.5f;
        }

        public static bool GetStripA3(int stripIndex)
        {
            return GetParameter($"Strip[{stripIndex}].A3", out float value) && value > 0.5f;
        }

        public static bool SetStripMute(int stripIndex, bool muted)
        {
            return SetParameter($"Strip[{stripIndex}].Mute", muted ? 1f : 0f);
        }

        public static bool SetStripGain(int stripIndex, float gainDb)
        {
            return SetParameter($"Strip[{stripIndex}].Gain", Mathf.Clamp(gainDb, -60f, 12f));
        }

        #endregion
    }
}
