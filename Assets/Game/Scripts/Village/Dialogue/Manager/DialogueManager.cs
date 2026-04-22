using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Village.Dialogue
{
    /// <summary>
    /// 對話播放狀態管理器（純邏輯，無 MonoBehaviour 相依）。
    /// 管理對話行的推進，透過 EventBus 發布開始、選項、結束事件。
    ///
    /// 支援兩種模式：
    /// 1. 純線性對話：StartDialogue(DialogueData) → Advance() 逐行推進。
    /// 2. VN 式選項分支：對話中透過 PresentChoices() 呈現選項，玩家呼叫 SelectChoice() 後，
    ///    由呼叫端透過 AppendLines() 附加後續對話行（通常是選擇對應的分支回應）。
    ///
    /// 選項呈現期間（IsWaitingForChoice = true）不可透過 Advance() 推進，必須先 SelectChoice()。
    /// </summary>
    public class DialogueManager
    {
        // 對話行使用 List 以支援中途附加（VN 分支）
        private readonly List<string> _lines = new List<string>();
        private int _currentLineIndex;
        private bool _isActive;

        private IReadOnlyList<DialogueChoice> _pendingChoices;
        private string _lastChoiceId;

        /// <summary>是否有進行中的對話。</summary>
        public bool IsActive => _isActive && !IsComplete;

        /// <summary>對話是否已全部播完（無待辦選項且已過最後一行）。</summary>
        public bool IsComplete => _isActive && !IsWaitingForChoice && _currentLineIndex >= _lines.Count;

        /// <summary>是否正在等待玩家選擇選項。</summary>
        public bool IsWaitingForChoice => _pendingChoices != null && _pendingChoices.Count > 0;

        /// <summary>當前呈現中的選項清單（若無則為 null）。</summary>
        public IReadOnlyList<DialogueChoice> CurrentChoices => _pendingChoices;

        /// <summary>
        /// 最近一次玩家選擇的選項 ID。
        /// 用於呼叫端在 SelectChoice 後查詢玩家的選擇，以決定要附加哪些分支對話行。
        /// 若從未選擇過則為 null。
        /// </summary>
        public string LastSelectedChoiceId => _lastChoiceId;

        /// <summary>
        /// 重置對話狀態到初始值（未啟動、無對話、無選項）。
        /// 用於切換場景或返回 Hub 時，避免前一個對話的殘留狀態影響下一次判斷。
        /// 不發布任何事件。
        /// </summary>
        public void Reset()
        {
            _lines.Clear();
            _currentLineIndex = 0;
            _isActive = false;
            _pendingChoices = null;
            _lastChoiceId = null;
        }

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

            _lines.Clear();
            _lines.AddRange(data.Lines);
            _currentLineIndex = 0;
            _isActive = true;
            _pendingChoices = null;
            _lastChoiceId = null;

            EventBus.Publish(new DialogueStartedEvent { FirstLine = GetCurrentLine() });
        }

        /// <summary>
        /// 取得當前行的文字。
        /// 若無進行中的對話或已結束，回傳 null。
        /// 選項呈現中時，仍回傳選項出現「當下」的那一行文字（通常是 choice_prompt 行）。
        /// </summary>
        public string GetCurrentLine()
        {
            if (!_isActive || _currentLineIndex >= _lines.Count)
            {
                return null;
            }

            return _lines[_currentLineIndex];
        }

        /// <summary>
        /// 前進到下一行。
        /// 若已到最後一行，標記為結束並發布 DialogueCompletedEvent，回傳 false。
        /// 若成功前進到下一行，回傳 true。
        /// 若無進行中的對話或正在等待選擇，回傳 false。
        /// </summary>
        public bool Advance()
        {
            if (!_isActive)
            {
                return false;
            }

            if (IsWaitingForChoice)
            {
                // 選項呈現中，必須先選擇才能推進
                return false;
            }

            if (_currentLineIndex >= _lines.Count)
            {
                return false;
            }

            _currentLineIndex++;

            if (_currentLineIndex >= _lines.Count)
            {
                EventBus.Publish(new DialogueCompletedEvent());
                return false;
            }

            return true;
        }

        /// <summary>
        /// 在當前對話流程中呈現 VN 式選項。
        /// 發布 DialogueChoicePresentedEvent 讓 UI 層顯示選項按鈕。
        /// 選項呈現期間，Advance() 會被鎖住，必須呼叫 SelectChoice() 解除。
        /// </summary>
        /// <param name="choices">選項清單，不可為 null 或空。</param>
        /// <exception cref="ArgumentNullException">choices 為 null 時拋出。</exception>
        /// <exception cref="ArgumentException">choices 為空時拋出。</exception>
        /// <exception cref="InvalidOperationException">無進行中的對話、或已在等待選擇時拋出。</exception>
        public void PresentChoices(IReadOnlyList<DialogueChoice> choices)
        {
            if (choices == null)
            {
                throw new ArgumentNullException(nameof(choices));
            }
            if (choices.Count == 0)
            {
                throw new ArgumentException("選項清單不可為空。", nameof(choices));
            }
            if (!_isActive)
            {
                throw new InvalidOperationException("無進行中的對話，無法呈現選項。");
            }
            if (IsWaitingForChoice)
            {
                throw new InvalidOperationException("已在等待玩家選擇，不可重複呈現選項。");
            }

            _pendingChoices = choices;

            EventBus.Publish(new DialogueChoicePresentedEvent { Choices = choices });
        }

        /// <summary>
        /// 玩家選擇了某個選項。
        /// 清除待辦選項、記錄選擇 ID、發布 DialogueChoiceSelectedEvent。
        /// 呼叫後呼叫端應視情況 AppendLines() 附加分支後續對話、或直接 Advance() 結束對話。
        /// </summary>
        /// <param name="choiceId">被選擇的選項 ID，必須存在於當前選項清單中。</param>
        /// <exception cref="InvalidOperationException">未在等待選擇時拋出。</exception>
        /// <exception cref="ArgumentException">choiceId 不在當前選項清單中時拋出。</exception>
        public void SelectChoice(string choiceId)
        {
            if (!IsWaitingForChoice)
            {
                throw new InvalidOperationException("目前沒有待選擇的選項。");
            }

            bool found = false;
            foreach (DialogueChoice choice in _pendingChoices)
            {
                if (choice.ChoiceId == choiceId)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                throw new ArgumentException(
                    $"選項 '{choiceId}' 不在當前選項清單中。", nameof(choiceId));
            }

            _pendingChoices = null;
            _lastChoiceId = choiceId;

            EventBus.Publish(new DialogueChoiceSelectedEvent { ChoiceId = choiceId });
        }

        /// <summary>
        /// 在當前對話流程尾端附加額外對話行。
        /// 用於 VN 分支：玩家選擇後呼叫端將該分支的對話行 append 進來，
        /// 繼續 Advance() 即可播放分支對話並在尾端觸發 DialogueCompletedEvent。
        ///
        /// 若附加時對話已走到最後一行（IsComplete），會重新啟用對話並指向新的下一行。
        /// </summary>
        /// <param name="lines">要附加的對話行，不可為 null。</param>
        /// <exception cref="ArgumentNullException">lines 為 null 時拋出。</exception>
        /// <exception cref="InvalidOperationException">無進行中的對話時拋出。</exception>
        public void AppendLines(IReadOnlyList<string> lines)
        {
            if (lines == null)
            {
                throw new ArgumentNullException(nameof(lines));
            }
            if (!_isActive)
            {
                throw new InvalidOperationException("無進行中的對話，無法附加行。");
            }
            if (lines.Count == 0)
            {
                return;
            }

            bool wasComplete = _currentLineIndex >= _lines.Count;
            foreach (string line in lines)
            {
                _lines.Add(line ?? string.Empty);
            }

            if (wasComplete)
            {
                // 原本已 complete；新的下一行從附加起點開始
                // _currentLineIndex 此時 == 原 _lines.Count == 新追加的第一行 index
                // IsComplete 會自動變 false（因為 _currentLineIndex < _lines.Count）
            }
        }
    }
}
