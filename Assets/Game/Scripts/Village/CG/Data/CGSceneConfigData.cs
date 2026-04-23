// CGSceneConfigData — CG 場景外部配置的 IGameData DTO 與不可變配置物件。
// 對應 Sheets 分頁：CGScene
// 對應 .txt 檔：cgscene.txt
//
// Sprint 8 Wave 2.5 重構：
//   - CGSceneConfigEntry 改名為 CGSceneData，欄位改 snake_case（cg_scene_id / character_id 等）
//   - 廢棄包裹類 CGSceneConfigData（純陣列格式，JsonFx 直接反序列化 CGSceneData[]）
//   - CGSceneConfig 建構子改為接受 CGSceneData[]，CGSceneInfo 保留不可變物件層
// ADR-001 / ADR-002 A02

using System;
using System.Collections.Generic;

namespace ProjectDR.Village.CG
{
    // ===== JSON DTO（供 JsonFx 反序列化純陣列使用） =====

    /// <summary>
    /// 單一 CG 場景配置（JSON DTO）。
    /// 實作 IGameData，int id 為流水號主鍵，cg_scene_id 為語意字串外鍵。
    /// 對應 Sheets 分頁 CGScene，.txt 檔 cgscene.txt。
    /// </summary>
    [Serializable]
    public class CGSceneData : KahaGameCore.GameData.IGameData
    {
        /// <summary>IGameData 主鍵（流水號）。對應 JSON 欄位 "id"。</summary>
        public int id;

        /// <summary>IGameData 契約實作。</summary>
        public int ID => id;

        /// <summary>CG 場景語意識別符。對應 JSON 欄位 "cg_scene_id"。</summary>
        public string cg_scene_id;

        /// <summary>語意字串 Key。</summary>
        public string Key => cg_scene_id;

        /// <summary>主角角色 ID。</summary>
        public string character_id;

        /// <summary>觸發所需好感度門檻。</summary>
        public int required_threshold;

        /// <summary>對應對話識別符（int）。</summary>
        public int dialogue_id;

        /// <summary>顯示名稱（繁中）。</summary>
        public string display_name;
    }

    // ===== 不可變資料物件 =====

    /// <summary>單一 CG 場景的不可變資訊。</summary>
    public class CGSceneInfo
    {
        public string CgSceneId { get; }
        public string CharacterId { get; }
        public int RequiredThreshold { get; }
        public int DialogueId { get; }
        public string DisplayName { get; }

        public CGSceneInfo(string cgSceneId, string characterId, int requiredThreshold, int dialogueId, string displayName)
        {
            CgSceneId = cgSceneId;
            CharacterId = characterId;
            RequiredThreshold = requiredThreshold;
            DialogueId = dialogueId;
            DisplayName = displayName;
        }
    }

    // ===== 不可變配置物件 =====

    /// <summary>
    /// CG 場景系統的不可變配置。
    /// 從 CGSceneData[]（純陣列 JSON DTO）建構，提供場景查詢 API。
    /// </summary>
    public class CGSceneConfig
    {
        private readonly Dictionary<string, CGSceneInfo> _sceneById;
        private readonly Dictionary<string, List<CGSceneInfo>> _scenesByCharacter;

        /// <summary>
        /// 從純陣列 DTO 建構不可變配置。
        /// </summary>
        /// <param name="entries">JsonFx 反序列化後的 CGSceneData 陣列。</param>
        /// <exception cref="ArgumentNullException">entries 為 null 時拋出。</exception>
        public CGSceneConfig(CGSceneData[] entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            _sceneById = new Dictionary<string, CGSceneInfo>();
            _scenesByCharacter = new Dictionary<string, List<CGSceneInfo>>();

            foreach (CGSceneData entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.cg_scene_id)) continue;

                CGSceneInfo info = new CGSceneInfo(
                    entry.cg_scene_id,
                    entry.character_id,
                    entry.required_threshold,
                    entry.dialogue_id,
                    entry.display_name);

                _sceneById[entry.cg_scene_id] = info;

                if (!string.IsNullOrEmpty(entry.character_id))
                {
                    if (!_scenesByCharacter.TryGetValue(entry.character_id, out List<CGSceneInfo> list))
                    {
                        list = new List<CGSceneInfo>();
                        _scenesByCharacter[entry.character_id] = list;
                    }
                    list.Add(info);
                }
            }
        }

        /// <summary>取得指定角色的所有 CG 場景配置。</summary>
        public IReadOnlyList<CGSceneInfo> GetScenesForCharacter(string characterId)
        {
            if (_scenesByCharacter.TryGetValue(characterId, out List<CGSceneInfo> list))
            {
                return list.AsReadOnly();
            }
            return Array.AsReadOnly(Array.Empty<CGSceneInfo>());
        }

        /// <summary>取得指定角色在指定門檻值下解鎖的場景。</summary>
        public IReadOnlyList<CGSceneInfo> GetScenesByThreshold(string characterId, int thresholdValue)
        {
            if (!_scenesByCharacter.TryGetValue(characterId, out List<CGSceneInfo> list))
            {
                return Array.AsReadOnly(Array.Empty<CGSceneInfo>());
            }

            List<CGSceneInfo> matched = new List<CGSceneInfo>();
            foreach (CGSceneInfo info in list)
            {
                if (info.RequiredThreshold == thresholdValue)
                {
                    matched.Add(info);
                }
            }
            return matched.AsReadOnly();
        }

        /// <summary>依 cg_scene_id 取得場景資訊。找不到時回傳 null。</summary>
        public CGSceneInfo GetSceneInfo(string cgSceneId)
        {
            if (_sceneById.TryGetValue(cgSceneId, out CGSceneInfo info))
            {
                return info;
            }
            return null;
        }
    }
}
