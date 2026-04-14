namespace ProjectDR.Village
{
    /// <summary>
    /// 對話資料結構。
    /// 儲存一組對話行文字，供 DialogueManager 播放。
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
            _lines = lines ?? System.Array.Empty<string>();
        }

        /// <summary>對話行文字陣列（唯讀）。</summary>
        public string[] Lines => _lines;

        /// <summary>對話行數。</summary>
        public int LineCount => _lines.Length;
    }
}
