// GuardReturnConfigData — 守衛歸來事件劇情配置的 JSON DTO 與不可變配置物件（B10 Sprint 4）。
// 配置檔路徑：Assets/Game/Resources/Config/guard-return-config.json
//
// 資料結構對應 A3 設計師產出：每行含 line_id / sequence / speaker / text / line_type / phase_id。
// phase_id：alert / clarify / sheathe / gift_sword / closing（純 metadata，供查詢用）。
//
// 守衛歸來事件為「純劇情演出」（無分支），GuardReturnEventController 依 sequence 順序播放全部行。

using System;
using System.Collections.Generic;

namespace ProjectDR.Village
{
    // ===== 階段 ID 常數 =====

    /// <summary>守衛歸來事件的階段 ID 常數（對應 JSON phase_id 欄位）。</summary>
    public static class GuardReturnPhaseIds
    {
        /// <summary>階段 1：警戒（守衛攔住玩家）。</summary>
        public const string Alert = "alert";

        /// <summary>階段 2：澄清（村長夫人介入）。</summary>
        public const string Clarify = "clarify";

        /// <summary>階段 3：收劍（守衛撤回警戒）。</summary>
        public const string Sheathe = "sheathe";

        /// <summary>階段 4：贈劍（守衛贈送武器）。</summary>
        public const string GiftSword = "gift_sword";

        /// <summary>階段 5：收尾（返村）。</summary>
        public const string Closing = "closing";
    }

    /// <summary>守衛歸來事件的行類型常數（對應 JSON line_type 欄位）。</summary>
    public static class GuardReturnLineTypes
    {
        public const string Narration = "narration";
        public const string Dialogue = "dialogue";
    }

    // ===== JSON DTO =====

    /// <summary>守衛歸來事件的單一對話行（JSON DTO）。</summary>
    [Serializable]
    public class GuardReturnLineData
    {
        /// <summary>對話行唯一識別。</summary>
        public string line_id;

        /// <summary>播放順序（升序）。</summary>
        public int sequence;

        /// <summary>說話者（Guard / VillageChiefWife / narrator）。</summary>
        public string speaker;

        /// <summary>對話文字。</summary>
        public string text;

        /// <summary>行類型（GuardReturnLineTypes）。</summary>
        public string line_type;

        /// <summary>階段 ID（GuardReturnPhaseIds）。</summary>
        public string phase_id;
    }

    /// <summary>守衛歸來事件完整外部配置（JSON DTO）。</summary>
    [Serializable]
    public class GuardReturnConfigData
    {
        /// <summary>資料結構版本。</summary>
        public int schema_version;

        /// <summary>配置說明。</summary>
        public string note;

        /// <summary>守衛歸來事件的所有對話行。</summary>
        public GuardReturnLineData[] guard_return_lines;
    }

    // ===== 不可變配置物件 =====

    /// <summary>
    /// 守衛歸來事件的不可變配置。
    /// 從 GuardReturnConfigData 建構，依 sequence 排序。
    /// 提供依 phase_id 查詢的 API 供 UI 預留（本批 B10 不使用，B13 CG 插播時可使用）。
    /// </summary>
    public class GuardReturnConfig
    {
        private readonly List<GuardReturnLineData> _linesOrdered;

        /// <summary>依 sequence 升序排列的所有對話行（唯讀）。</summary>
        public IReadOnlyList<GuardReturnLineData> OrderedLines => _linesOrdered;

        /// <summary>
        /// 從 JSON DTO 建構不可變配置。
        /// </summary>
        /// <param name="data">JSON 反序列化後的 DTO（不可為 null）。</param>
        /// <exception cref="ArgumentNullException">data 為 null 時拋出。</exception>
        public GuardReturnConfig(GuardReturnConfigData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _linesOrdered = new List<GuardReturnLineData>();

            GuardReturnLineData[] lines = data.guard_return_lines ?? Array.Empty<GuardReturnLineData>();
            foreach (GuardReturnLineData line in lines)
            {
                if (line == null) continue;
                _linesOrdered.Add(line);
            }
            _linesOrdered.Sort((a, b) => a.sequence.CompareTo(b.sequence));
        }

        /// <summary>取得所有對話行的純文字陣列（依 sequence 順序）。供 DialogueData 使用。</summary>
        public string[] GetAllLineTexts()
        {
            if (_linesOrdered.Count == 0)
            {
                return Array.Empty<string>();
            }
            string[] texts = new string[_linesOrdered.Count];
            for (int i = 0; i < _linesOrdered.Count; i++)
            {
                texts[i] = _linesOrdered[i]?.text ?? string.Empty;
            }
            return texts;
        }

        /// <summary>取得指定階段的所有對話行（供 UI 層選擇性播放 CG 時使用）。</summary>
        public IReadOnlyList<GuardReturnLineData> GetLinesByPhase(string phaseId)
        {
            if (string.IsNullOrEmpty(phaseId))
            {
                return Array.AsReadOnly(Array.Empty<GuardReturnLineData>());
            }
            List<GuardReturnLineData> result = new List<GuardReturnLineData>();
            foreach (GuardReturnLineData line in _linesOrdered)
            {
                if (line.phase_id == phaseId)
                {
                    result.Add(line);
                }
            }
            return result.AsReadOnly();
        }
    }
}
