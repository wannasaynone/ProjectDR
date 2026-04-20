// PlayerQuestionsConfigData — 玩家發問 40 題配置的 JSON DTO 與不可變配置物件（B14）。
// 配置檔路徑：Assets/Game/Resources/Config/player-questions-config.json
//
// 欄位說明（schema_version 1）：
// - question_id      : 全域唯一識別
// - character_id     : 對應角色（CharacterIds）
// - unlock_affinity_stage : 解鎖需求好感度門檻索引（0 = 初始解鎖）
// - question_text    : 問題文字（玩家視角）
// - response_text    : 角色回答（打字機播放）
// - sort_order       : 顯示排序（升序）
//
// 新增欄位（schema_version 2，Sprint 6 擴張 B5/C11）：
// - is_single_use    : bool，預設 false；true = 單次特殊題，問後從清單永久消失
// - trigger_flag     : string，預設空字串；非空時觸發對應 flag 效果（如 "grant_guard_sword"）
// - affinity_gain    : int，預設 0；觸發特殊題時的好感度增量（通常為 0）
//
// ⚠️ 本配置中的所有題目均為 AI 生成 placeholder，待製作人審閱並逐一撰寫正式版本。

using System;
using System.Collections.Generic;

namespace ProjectDR.Village
{
    // ===== JSON DTO =====

    /// <summary>單一問答題 JSON DTO。</summary>
    [Serializable]
    public class PlayerQuestionData
    {
        /// <summary>題目唯一識別。</summary>
        public string question_id;

        /// <summary>所屬角色 ID（對應 CharacterIds）。</summary>
        public string character_id;

        /// <summary>解鎖所需好感度門檻索引（0 表示初始即可見）。</summary>
        public int unlock_affinity_stage;

        /// <summary>問題文字（玩家問角色）。</summary>
        public string question_text;

        /// <summary>角色回答文字。</summary>
        public string response_text;

        /// <summary>排序鍵（升序顯示）。</summary>
        public int sort_order;

        // ===== schema_version 2 新增欄位（Sprint 6 擴張 B5/C11）=====

        /// <summary>
        /// 是否為單次特殊題（schema_version 2）。
        /// true = 問完後從清單永久消失（session 旗標 + 概念上的永久移除）。
        /// </summary>
        public bool is_single_use;

        /// <summary>
        /// 觸發旗標 ID（schema_version 2）。
        /// 非空時 PlayerQuestionsManager 在玩家選題後呼叫對應的 flag handler。
        /// 目前已知旗標：
        ///   "grant_guard_sword" → 贈劍 + 探索重開
        /// </summary>
        public string trigger_flag;

        /// <summary>觸發特殊題時的好感度增量（schema_version 2，通常為 0）。</summary>
        public int affinity_gain;
    }

    /// <summary>玩家發問配置完整 JSON DTO。</summary>
    [Serializable]
    public class PlayerQuestionsConfigData
    {
        /// <summary>資料結構版本。</summary>
        public int schema_version;

        /// <summary>備註。</summary>
        public string note;

        /// <summary>所有問答題目。</summary>
        public PlayerQuestionData[] questions;
    }

    // ===== 不可變配置物件 =====

    /// <summary>單一問答題（不可變）。</summary>
    public class PlayerQuestionInfo
    {
        /// <summary>題目 ID。</summary>
        public string QuestionId { get; }

        /// <summary>所屬角色 ID。</summary>
        public string CharacterId { get; }

        /// <summary>解鎖所需好感度門檻索引（0 = 初始即解鎖）。</summary>
        public int UnlockAffinityStage { get; }

        /// <summary>問題文字。</summary>
        public string QuestionText { get; }

        /// <summary>角色回答文字。</summary>
        public string ResponseText { get; }

        /// <summary>排序鍵。</summary>
        public int SortOrder { get; }

        /// <summary>
        /// 是否為單次特殊題（schema_version 2）。
        /// true = 問完後從清單永久消失。
        /// </summary>
        public bool IsSingleUse { get; }

        /// <summary>
        /// 觸發旗標 ID（schema_version 2）。
        /// 非空時 PlayerQuestionsManager 在玩家選題後呼叫對應 flag handler。
        /// </summary>
        public string TriggerFlag { get; }

        /// <summary>觸發特殊題時的好感度增量（schema_version 2，通常為 0）。</summary>
        public int AffinityGain { get; }

        /// <summary>是否有觸發旗標效果。</summary>
        public bool HasTriggerFlag => !string.IsNullOrEmpty(TriggerFlag);

        public PlayerQuestionInfo(
            string questionId,
            string characterId,
            int unlockAffinityStage,
            string questionText,
            string responseText,
            int sortOrder,
            bool isSingleUse = false,
            string triggerFlag = "",
            int affinityGain = 0)
        {
            QuestionId = questionId;
            CharacterId = characterId;
            UnlockAffinityStage = unlockAffinityStage;
            QuestionText = questionText;
            ResponseText = responseText;
            SortOrder = sortOrder;
            IsSingleUse = isSingleUse;
            TriggerFlag = triggerFlag ?? string.Empty;
            AffinityGain = affinityGain;
        }
    }

    /// <summary>
    /// 玩家發問配置（不可變）。
    /// 提供依角色 ID 查詢題目清單、依好感度階段過濾的 API。
    /// </summary>
    public class PlayerQuestionsConfig
    {
        private readonly Dictionary<string, List<PlayerQuestionInfo>> _byCharacter;
        private readonly Dictionary<string, PlayerQuestionInfo> _byId;

        /// <summary>
        /// 從 JSON DTO 建構不可變配置。
        /// </summary>
        /// <param name="data">JSON 反序列化後的 DTO（不可為 null）。</param>
        /// <exception cref="ArgumentNullException">data 為 null 時拋出。</exception>
        public PlayerQuestionsConfig(PlayerQuestionsConfigData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            _byCharacter = new Dictionary<string, List<PlayerQuestionInfo>>();
            _byId = new Dictionary<string, PlayerQuestionInfo>();

            PlayerQuestionData[] questions = data.questions ?? Array.Empty<PlayerQuestionData>();
            foreach (PlayerQuestionData q in questions)
            {
                if (q == null || string.IsNullOrEmpty(q.question_id) || string.IsNullOrEmpty(q.character_id))
                    continue;

                PlayerQuestionInfo info = new PlayerQuestionInfo(
                    q.question_id,
                    q.character_id,
                    q.unlock_affinity_stage,
                    q.question_text ?? string.Empty,
                    q.response_text ?? string.Empty,
                    q.sort_order,
                    q.is_single_use,
                    q.trigger_flag ?? string.Empty,
                    q.affinity_gain);

                _byId[q.question_id] = info;

                if (!_byCharacter.TryGetValue(q.character_id, out List<PlayerQuestionInfo> bucket))
                {
                    bucket = new List<PlayerQuestionInfo>();
                    _byCharacter[q.character_id] = bucket;
                }
                bucket.Add(info);
            }

            // 依 sort_order 排序每個角色的題目清單
            foreach (List<PlayerQuestionInfo> list in _byCharacter.Values)
            {
                list.Sort((PlayerQuestionInfo a, PlayerQuestionInfo b) => a.SortOrder.CompareTo(b.SortOrder));
            }
        }

        /// <summary>
        /// 取得指定角色的所有題目（依 sort_order 排序）。
        /// 找不到角色時回傳空列表。
        /// </summary>
        public IReadOnlyList<PlayerQuestionInfo> GetQuestionsForCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return Array.AsReadOnly(Array.Empty<PlayerQuestionInfo>());
            if (_byCharacter.TryGetValue(characterId, out List<PlayerQuestionInfo> list))
                return list.AsReadOnly();
            return Array.AsReadOnly(Array.Empty<PlayerQuestionInfo>());
        }

        /// <summary>
        /// 取得指定角色在指定好感度階段或以下可解鎖的題目。
        /// </summary>
        /// <param name="characterId">角色 ID。</param>
        /// <param name="currentAffinityStage">當前已達成的好感度門檻索引（包含此值及以下的題目均可見）。</param>
        public IReadOnlyList<PlayerQuestionInfo> GetUnlockedQuestions(string characterId, int currentAffinityStage)
        {
            IReadOnlyList<PlayerQuestionInfo> all = GetQuestionsForCharacter(characterId);
            List<PlayerQuestionInfo> result = new List<PlayerQuestionInfo>();
            foreach (PlayerQuestionInfo q in all)
            {
                if (q.UnlockAffinityStage <= currentAffinityStage)
                    result.Add(q);
            }
            return result.AsReadOnly();
        }

        /// <summary>依 question_id 取得單一題目。找不到時回傳 null。</summary>
        public PlayerQuestionInfo GetQuestion(string questionId)
        {
            if (string.IsNullOrEmpty(questionId)) return null;
            _byId.TryGetValue(questionId, out PlayerQuestionInfo info);
            return info;
        }
    }
}
