// AffinityManager — 好感度管理器。
// 管理各角色的好感度數值，當好感度達到門檻時發布事件。
// IT 階段不需持久化（不需存檔），所有數值僅存在於執行期記憶體中。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village
{
    /// <summary>
    /// 好感度管理器。
    /// 管理各角色的好感度整數值，支援增加好感度並在達到門檻時發布事件。
    /// 純邏輯類別（非 MonoBehaviour），透過建構子注入配置。
    /// </summary>
    public class AffinityManager
    {
        private readonly AffinityConfig _config;

        /// <summary>各角色的當前好感度值。</summary>
        private readonly Dictionary<string, int> _affinityValues;

        /// <summary>各角色已達成的門檻索引（追蹤到哪個門檻已觸發）。</summary>
        private readonly Dictionary<string, int> _reachedThresholdIndices;

        /// <summary>
        /// 建構好感度管理器。
        /// </summary>
        /// <param name="config">好感度外部配置（不可為 null）。</param>
        /// <exception cref="ArgumentNullException">config 為 null 時拋出。</exception>
        public AffinityManager(AffinityConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _affinityValues = new Dictionary<string, int>();
            _reachedThresholdIndices = new Dictionary<string, int>();
        }

        /// <summary>
        /// 取得指定角色的當前好感度值。
        /// </summary>
        /// <param name="characterId">角色 ID（不可為 null 或空字串）。</param>
        /// <returns>當前好感度值，未初始化的角色回傳 0。</returns>
        /// <exception cref="ArgumentNullException">characterId 為 null 時拋出。</exception>
        /// <exception cref="ArgumentException">characterId 為空字串時拋出。</exception>
        public int GetAffinity(string characterId)
        {
            ValidateCharacterId(characterId);

            if (_affinityValues.TryGetValue(characterId, out int value))
            {
                return value;
            }
            return 0;
        }

        /// <summary>
        /// 增加指定角色的好感度。
        /// 增加後會發布 AffinityChangedEvent，若達到新門檻則額外發布 AffinityThresholdReachedEvent。
        /// </summary>
        /// <param name="characterId">角色 ID（不可為 null 或空字串）。</param>
        /// <param name="amount">增加量（必須大於 0）。</param>
        /// <exception cref="ArgumentNullException">characterId 為 null 時拋出。</exception>
        /// <exception cref="ArgumentException">characterId 為空字串或 amount 小於等於 0 時拋出。</exception>
        public void AddAffinity(string characterId, int amount)
        {
            ValidateCharacterId(characterId);

            if (amount <= 0)
            {
                throw new ArgumentException("好感度增加量必須大於 0。", nameof(amount));
            }

            // 取得舊值並計算新值
            int oldValue = GetAffinity(characterId);
            int newValue = oldValue + amount;
            _affinityValues[characterId] = newValue;

            // 發布好感度變更事件
            EventBus.Publish(new AffinityChangedEvent
            {
                CharacterId = characterId,
                NewValue = newValue,
                Amount = amount
            });

            // 檢查門檻觸發
            CheckThresholds(characterId, oldValue, newValue);
        }

        /// <summary>
        /// 取得指定角色的所有門檻值。
        /// </summary>
        /// <param name="characterId">角色 ID（不可為 null）。</param>
        /// <returns>門檻值的唯讀清單。</returns>
        /// <exception cref="ArgumentNullException">characterId 為 null 時拋出。</exception>
        public IReadOnlyList<int> GetThresholds(string characterId)
        {
            if (characterId == null)
            {
                throw new ArgumentNullException(nameof(characterId));
            }
            return _config.GetThresholds(characterId);
        }

        /// <summary>
        /// 取得指定角色已達成的門檻值清單。
        /// </summary>
        /// <param name="characterId">角色 ID（不可為 null）。</param>
        /// <returns>已達成門檻值的唯讀清單。</returns>
        /// <exception cref="ArgumentNullException">characterId 為 null 時拋出。</exception>
        public IReadOnlyList<int> GetReachedThresholds(string characterId)
        {
            if (characterId == null)
            {
                throw new ArgumentNullException(nameof(characterId));
            }

            IReadOnlyList<int> thresholds = _config.GetThresholds(characterId);
            int reachedCount = GetReachedThresholdCount(characterId);

            if (reachedCount == 0)
            {
                return Array.AsReadOnly(Array.Empty<int>());
            }

            int[] reached = new int[reachedCount];
            for (int i = 0; i < reachedCount; i++)
            {
                reached[i] = thresholds[i];
            }

            return Array.AsReadOnly(reached);
        }

        /// <summary>
        /// 檢查並觸發好感度門檻事件。
        /// 從上次已達成的門檻索引開始，逐一檢查是否有新門檻被達成。
        /// </summary>
        private void CheckThresholds(string characterId, int oldValue, int newValue)
        {
            IReadOnlyList<int> thresholds = _config.GetThresholds(characterId);
            int startIndex = GetReachedThresholdCount(characterId);

            for (int i = startIndex; i < thresholds.Count; i++)
            {
                if (newValue >= thresholds[i])
                {
                    // 更新已達成門檻索引
                    _reachedThresholdIndices[characterId] = i + 1;

                    // 發布門檻達成事件
                    EventBus.Publish(new AffinityThresholdReachedEvent
                    {
                        CharacterId = characterId,
                        ThresholdValue = thresholds[i]
                    });
                }
                else
                {
                    // 門檻是升序排列，後面的不可能達到
                    break;
                }
            }
        }

        /// <summary>取得指定角色已達成的門檻數量。</summary>
        private int GetReachedThresholdCount(string characterId)
        {
            if (_reachedThresholdIndices.TryGetValue(characterId, out int count))
            {
                return count;
            }
            return 0;
        }

        /// <summary>驗證角色 ID 參數。</summary>
        private static void ValidateCharacterId(string characterId)
        {
            if (characterId == null)
            {
                throw new ArgumentNullException(nameof(characterId));
            }
            if (characterId.Length == 0)
            {
                throw new ArgumentException("角色 ID 不可為空字串。", nameof(characterId));
            }
        }
    }
}
