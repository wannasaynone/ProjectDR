using System;

namespace ProjectDR.Village.Dialogue
{
    /// <summary>
    /// 對話資料結構。
    /// 儲存一組對話行文字（純線性），供 DialogueManager 播放。
    ///
    /// 若需要 VN 式選項分支，使用 DialogueManager.AppendLines() 或 DialogueManager.PresentChoices()
    /// 動態延伸對話流程。
    /// </summary>
    public class DialogueData
    {
        private readonly string[] _lines;

        /// <summary>
        /// 建立對話資料。
        /// </summary>
        /// <param name="lines">對話行文字陣列，不可為 null 或空。</param>
        public DialogueData(string[] lines)
        {
            _lines = lines ?? Array.Empty<string>();
        }

        /// <summary>對話行文字陣列（唯讀）。</summary>
        public string[] Lines => _lines;

        /// <summary>對話行數。</summary>
        public int LineCount => _lines.Length;
    }

    /// <summary>
    /// 單一選項資料（不可變）。
    /// 由 DialogueManager 呈現，玩家選擇後透過 DialogueChoiceSelectedEvent 通知。
    /// </summary>
    public class DialogueChoice
    {
        /// <summary>選項識別（用於區分選擇，通常為分支 ID 或索引字串）。</summary>
        public string ChoiceId { get; }

        /// <summary>選項顯示文字。</summary>
        public string Text { get; }

        public DialogueChoice(string choiceId, string text)
        {
            ChoiceId = choiceId ?? string.Empty;
            Text = text ?? string.Empty;
        }
    }
}
