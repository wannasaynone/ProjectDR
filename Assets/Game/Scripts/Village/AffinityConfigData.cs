// AffinityConfigData — 好感度系統外部配置的 JSON DTO 與不可變配置物件。
// 配置檔路徑：Assets/Game/Resources/Config/affinity-config.json
// 此配置不經由 Google Sheets 管理，因為 IT 階段好感度門檻為簡易固定值，
// 正式版本再視需求決定是否遷移至 Google Sheets。

using System;
using System.Collections.Generic;

namespace ProjectDR.Village
{
    // ===== JSON DTO（供 JsonUtility.FromJson 使用） =====

    /// <summary>單一角色的好感度門檻配置（JSON DTO）。</summary>
    [Serializable]
    public class AffinityCharacterConfigData
    {
        /// <summary>角色 ID。</summary>
        public string characterId;

        /// <summary>好感度門檻值陣列（升序排列）。</summary>
        public int[] thresholds;
    }

    /// <summary>好感度系統的完整外部配置（JSON DTO）。</summary>
    [Serializable]
    public class AffinityConfigData
    {
        /// <summary>各角色的門檻配置。</summary>
        public AffinityCharacterConfigData[] characters;

        /// <summary>未明確配置角色使用的預設門檻。</summary>
        public int[] defaultThresholds;
    }

    // ===== 不可變配置物件 =====

    /// <summary>
    /// 好感度系統的不可變配置。
    /// 從 AffinityConfigData（JSON DTO）建構，提供門檻查詢 API。
    /// </summary>
    public class AffinityConfig
    {
        private readonly Dictionary<string, IReadOnlyList<int>> _characterThresholds;
        private readonly IReadOnlyList<int> _defaultThresholds;

        /// <summary>
        /// 從 JSON DTO 建構不可變配置。
        /// </summary>
        /// <param name="data">JSON 反序列化後的 DTO。</param>
        /// <exception cref="ArgumentNullException">data 為 null 時拋出。</exception>
        public AffinityConfig(AffinityConfigData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _defaultThresholds = data.defaultThresholds != null
                ? Array.AsReadOnly(data.defaultThresholds)
                : Array.AsReadOnly(Array.Empty<int>());

            _characterThresholds = new Dictionary<string, IReadOnlyList<int>>();

            if (data.characters != null)
            {
                foreach (AffinityCharacterConfigData characterConfig in data.characters)
                {
                    if (characterConfig == null || string.IsNullOrEmpty(characterConfig.characterId))
                    {
                        continue;
                    }

                    int[] thresholds = characterConfig.thresholds ?? Array.Empty<int>();
                    _characterThresholds[characterConfig.characterId] = Array.AsReadOnly(thresholds);
                }
            }
        }

        /// <summary>
        /// 取得指定角色的好感度門檻清單。
        /// 若角色未明確配置，回傳 defaultThresholds。
        /// </summary>
        /// <param name="characterId">角色 ID。</param>
        /// <returns>門檻值的唯讀清單。</returns>
        public IReadOnlyList<int> GetThresholds(string characterId)
        {
            if (_characterThresholds.TryGetValue(characterId, out IReadOnlyList<int> thresholds))
            {
                return thresholds;
            }
            return _defaultThresholds;
        }
    }
}
