// CGSceneConfigData -- CG 場景配置的 JSON DTO 與不可變配置物件。
// 配置檔路徑：Assets/Game/Resources/Config/cg-scene-config.json
// 此配置不經由 Google Sheets 管理，因為 IT 階段 CG 場景為簡易固定配置，
// 正式版本再視需求決定是否遷移至 Google Sheets。

using System;
using System.Collections.Generic;

namespace ProjectDR.Village
{
    // ===== JSON DTO（供 JsonUtility.FromJson 使用） =====

    /// <summary>單一 CG 場景的配置項（JSON DTO）。</summary>
    [Serializable]
    public class CGSceneConfigEntry
    {
        /// <summary>CG 場景唯一 ID。</summary>
        public string cgSceneId;

        /// <summary>所屬角色 ID。</summary>
        public string characterId;

        /// <summary>解鎖所需的好感度門檻值。</summary>
        public int requiredThreshold;

        /// <summary>對應的 KGC DialogueSystem 對話 ID。</summary>
        public int dialogueId;

        /// <summary>場景顯示名稱。</summary>
        public string displayName;
    }

    /// <summary>CG 場景配置的完整外部資料（JSON DTO）。</summary>
    [Serializable]
    public class CGSceneConfigData
    {
        /// <summary>所有 CG 場景配置。</summary>
        public CGSceneConfigEntry[] scenes;
    }

    // ===== 不可變資料物件 =====

    /// <summary>
    /// 單一 CG 場景的不可變資訊。
    /// </summary>
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
    /// 從 CGSceneConfigData（JSON DTO）建構，提供場景查詢 API。
    /// </summary>
    public class CGSceneConfig
    {
        private readonly Dictionary<string, CGSceneInfo> _sceneById;
        private readonly Dictionary<string, List<CGSceneInfo>> _scenesByCharacter;

        /// <summary>
        /// 從 JSON DTO 建構不可變配置。
        /// </summary>
        /// <param name="data">JSON 反序列化後的 DTO。</param>
        /// <exception cref="ArgumentNullException">data 為 null 時拋出。</exception>
        public CGSceneConfig(CGSceneConfigData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _sceneById = new Dictionary<string, CGSceneInfo>();
            _scenesByCharacter = new Dictionary<string, List<CGSceneInfo>>();

            if (data.scenes == null) return;

            foreach (CGSceneConfigEntry entry in data.scenes)
            {
                if (entry == null || string.IsNullOrEmpty(entry.cgSceneId)) continue;

                CGSceneInfo info = new CGSceneInfo(
                    entry.cgSceneId,
                    entry.characterId,
                    entry.requiredThreshold,
                    entry.dialogueId,
                    entry.displayName
                );

                _sceneById[entry.cgSceneId] = info;

                if (!string.IsNullOrEmpty(entry.characterId))
                {
                    if (!_scenesByCharacter.TryGetValue(entry.characterId, out List<CGSceneInfo> list))
                    {
                        list = new List<CGSceneInfo>();
                        _scenesByCharacter[entry.characterId] = list;
                    }
                    list.Add(info);
                }
            }
        }

        /// <summary>
        /// 取得指定角色的所有 CG 場景配置。
        /// </summary>
        public IReadOnlyList<CGSceneInfo> GetScenesForCharacter(string characterId)
        {
            if (_scenesByCharacter.TryGetValue(characterId, out List<CGSceneInfo> list))
            {
                return list.AsReadOnly();
            }
            return Array.AsReadOnly(Array.Empty<CGSceneInfo>());
        }

        /// <summary>
        /// 取得指定角色在指定門檻值下解鎖的場景。
        /// </summary>
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

        /// <summary>
        /// 依 cgSceneId 取得場景資訊。找不到時回傳 null。
        /// </summary>
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
