namespace AudioControl
{
    /// <summary>
    /// 音频输出模式枚举
    /// 对应 UR-RT4 的输出控制逻辑
    /// </summary>
    public enum AudioOutputMode
    {
        /// <summary>
        /// A 模式：仅耳机输出
        /// VoiceMeeter: A1=1, A2=0
        /// </summary>
        A_HeadphoneOnly,

        /// <summary>
        /// B 模式：耳机 + 音箱同时输出
        /// VoiceMeeter: A1=1, A2=1
        /// </summary>
        B_HeadphoneAndSpeaker,

        /// <summary>
        /// C 模式：仅音箱输出
        /// VoiceMeeter: A1=0, A2=1
        /// </summary>
        C_SpeakerOnly
    }
}
