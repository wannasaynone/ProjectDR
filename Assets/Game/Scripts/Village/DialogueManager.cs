using KahaGameCore.GameEvent;

namespace ProjectDR.Village
{
    /// <summary>
    /// 對話播放狀態管理器（純邏輯，無 MonoBehaviour 相依）。
    /// 管理對話行的推進，透過 EventBus 發布開始與結束事件。
    /// </summary>
    public class DialogueManager
    {
        private DialogueData _currentDialogue;
        private int _currentLineIndex;

        /// <summary>是否有進行中的對話。</summary>
        public bool IsActive => _currentDialogue != null && !IsComplete;

        /// <summary>對話是否已全部播完。</summary>
        public bool IsComplete => _currentDialogue != null && _currentLineIndex >= _currentDialogue.LineCount;

        /// <summary>
        /// 開始播放對話。
        /// 若傳入的對話資料為 null 或沒有任何行，則不啟動。
        /// 成功啟動時發布 DialogueStartedEvent。
        /// </summary>
        public void StartDialogue(DialogueData data)
        {
            if (data == null || data.LineCount == 0)
            {
                return;
            }

            _currentDialogue = data;
            _currentLineIndex = 0;

            EventBus.Publish(new DialogueStartedEvent { FirstLine = GetCurrentLine() });
        }

        /// <summary>
        /// 取得當前行的文字。
        /// 若無進行中的對話或已結束，回傳 null。
        /// </summary>
        public string GetCurrentLine()
        {
            if (_currentDialogue == null || _currentLineIndex >= _currentDialogue.LineCount)
            {
                return null;
            }

            return _currentDialogue.Lines[_currentLineIndex];
        }

        /// <summary>
        /// 前進到下一行。
        /// 若已到最後一行，標記為結束並發布 DialogueCompletedEvent，回傳 false。
        /// 若成功前進到下一行，回傳 true。
        /// 若無進行中的對話，回傳 false。
        /// </summary>
        public bool Advance()
        {
            if (_currentDialogue == null)
            {
                return false;
            }

            if (_currentLineIndex >= _currentDialogue.LineCount)
            {
                return false;
            }

            _currentLineIndex++;

            if (_currentLineIndex >= _currentDialogue.LineCount)
            {
                EventBus.Publish(new DialogueCompletedEvent());
                return false;
            }

            return true;
        }
    }
}
