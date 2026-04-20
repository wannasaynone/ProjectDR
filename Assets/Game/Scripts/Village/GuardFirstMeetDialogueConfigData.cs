// GuardFirstMeetDialogueConfigData — 守衛首次進入互動畫面自動觸發的取劍對白配置 DTO。
// 配置檔路徑：Assets/Game/Resources/Config/guard-first-meet-dialogue-config.json
//
// Sprint 6 決策 6-13：守衛取劍改為「首次進入自動對白觸發」。
// 對白內容為 placeholder，待製作人撰寫正式台詞。

using System;
using System.Collections.Generic;

namespace ProjectDR.Village
{
    // ===== JSON DTO（供 JsonUtility.FromJson 使用） =====

    /// <summary>守衛首次進入取劍對白配置（JSON DTO）。</summary>
    [Serializable]
    public class GuardFirstMeetDialogueConfigData
    {
        /// <summary>資料結構版本。</summary>
        public int schema_version;

        /// <summary>備註。</summary>
        public string note;

        /// <summary>對白行（按順序播放）。</summary>
        public string[] dialogue_lines;
    }

    // ===== 不可變配置物件 =====

    /// <summary>
    /// 守衛首次進入取劍對白配置（不可變）。
    /// 從 GuardFirstMeetDialogueConfigData（JSON DTO）建構。
    /// </summary>
    public class GuardFirstMeetDialogueConfig
    {
        private static readonly string[] FallbackLines = new string[]
        {
            "（守衛遞給你一把劍）",
            "拿好它。在森林裡要小心。【placeholder】"
        };

        /// <summary>對白行（按順序播放）。空配置時使用 fallback。</summary>
        public IReadOnlyList<string> DialogueLines { get; }

        public GuardFirstMeetDialogueConfig(GuardFirstMeetDialogueConfigData data)
        {
            if (data == null || data.dialogue_lines == null || data.dialogue_lines.Length == 0)
            {
                DialogueLines = Array.AsReadOnly(FallbackLines);
            }
            else
            {
                DialogueLines = Array.AsReadOnly(data.dialogue_lines);
            }
        }
    }
}
