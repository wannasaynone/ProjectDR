// CGUnlockManager -- CG 解鎖管理器。
// 監聯 AffinityThresholdReachedEvent，根據 CGSceneConfig 解鎖對應 CG 場景。
// IT 階段不做持久化，解鎖狀態僅存在於執行期記憶體。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village
{
    /// <summary>
    /// CG 解鎖管理器。
    /// 管理 CG 場景的解鎖狀態，監聽好感度門檻事件自動解鎖。
    /// IT 階段不做持久化，解鎖狀態僅存在於執行期記憶體。
    /// 純邏輯類別（非 MonoBehaviour），透過建構子注入配置。
    /// </summary>
    public class CGUnlockManager : IDisposable
    {
        private readonly CGSceneConfig _config;
        private readonly HashSet<string> _unlockedSceneIds;

        /// <summary>
        /// 建構 CG 解鎖管理器。
        /// 建構時訂閱好感度門檻事件。
        /// </summary>
        /// <param name="config">CG 場景配置（不可為 null）。</param>
        /// <exception cref="ArgumentNullException">config 為 null 時拋出。</exception>
        public CGUnlockManager(CGSceneConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _unlockedSceneIds = new HashSet<string>();

            EventBus.Subscribe<AffinityThresholdReachedEvent>(OnAffinityThresholdReached);
        }

        /// <summary>
        /// 檢查指定 CG 場景是否已解鎖。
        /// </summary>
        /// <param name="cgSceneId">CG 場景 ID（不可為 null 或空字串）。</param>
        /// <returns>是否已解鎖。</returns>
        /// <exception cref="ArgumentNullException">cgSceneId 為 null 時拋出。</exception>
        /// <exception cref="ArgumentException">cgSceneId 為空字串時拋出。</exception>
        public bool IsUnlocked(string cgSceneId)
        {
            ValidateCgSceneId(cgSceneId);
            return _unlockedSceneIds.Contains(cgSceneId);
        }

        /// <summary>
        /// 解鎖指定 CG 場景。若已解鎖則不重複觸發事件。
        /// </summary>
        /// <param name="cgSceneId">CG 場景 ID（不可為 null 或空字串）。</param>
        /// <exception cref="ArgumentNullException">cgSceneId 為 null 時拋出。</exception>
        /// <exception cref="ArgumentException">cgSceneId 為空字串時拋出。</exception>
        public void UnlockScene(string cgSceneId)
        {
            ValidateCgSceneId(cgSceneId);

            if (_unlockedSceneIds.Contains(cgSceneId))
            {
                return; // 已解鎖，不重複處理
            }

            _unlockedSceneIds.Add(cgSceneId);

            // 發布事件（僅當配置中有該場景時才發布，因為需要 characterId）
            CGSceneInfo sceneInfo = _config.GetSceneInfo(cgSceneId);
            if (sceneInfo != null)
            {
                EventBus.Publish(new CGUnlockedEvent
                {
                    CgSceneId = cgSceneId,
                    CharacterId = sceneInfo.CharacterId
                });
            }
        }

        /// <summary>
        /// 取得指定角色已解鎖的 CG 場景清單。
        /// </summary>
        /// <param name="characterId">角色 ID（不可為 null）。</param>
        /// <returns>已解鎖的場景資訊清單。</returns>
        /// <exception cref="ArgumentNullException">characterId 為 null 時拋出。</exception>
        public IReadOnlyList<CGSceneInfo> GetUnlockedScenes(string characterId)
        {
            if (characterId == null)
            {
                throw new ArgumentNullException(nameof(characterId));
            }

            IReadOnlyList<CGSceneInfo> allScenes = _config.GetScenesForCharacter(characterId);
            List<CGSceneInfo> unlocked = new List<CGSceneInfo>();

            for (int i = 0; i < allScenes.Count; i++)
            {
                if (_unlockedSceneIds.Contains(allScenes[i].CgSceneId))
                {
                    unlocked.Add(allScenes[i]);
                }
            }

            return unlocked.AsReadOnly();
        }

        /// <summary>
        /// 檢查指定角色是否有任何已解鎖的 CG 場景。
        /// </summary>
        /// <param name="characterId">角色 ID（不可為 null）。</param>
        /// <returns>是否有已解鎖場景。</returns>
        /// <exception cref="ArgumentNullException">characterId 為 null 時拋出。</exception>
        public bool HasUnlockedScenes(string characterId)
        {
            if (characterId == null)
            {
                throw new ArgumentNullException(nameof(characterId));
            }

            IReadOnlyList<CGSceneInfo> allScenes = _config.GetScenesForCharacter(characterId);
            for (int i = 0; i < allScenes.Count; i++)
            {
                if (_unlockedSceneIds.Contains(allScenes[i].CgSceneId))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 釋放資源，取消事件訂閱。
        /// </summary>
        public void Dispose()
        {
            EventBus.Unsubscribe<AffinityThresholdReachedEvent>(OnAffinityThresholdReached);
        }

        // ===== 事件處理 =====

        private void OnAffinityThresholdReached(AffinityThresholdReachedEvent e)
        {
            // 根據角色 ID 和門檻值查找對應的 CG 場景
            IReadOnlyList<CGSceneInfo> matchedScenes = _config.GetScenesByThreshold(e.CharacterId, e.ThresholdValue);

            for (int i = 0; i < matchedScenes.Count; i++)
            {
                UnlockScene(matchedScenes[i].CgSceneId);
            }
        }

        private static void ValidateCgSceneId(string cgSceneId)
        {
            if (cgSceneId == null)
            {
                throw new ArgumentNullException(nameof(cgSceneId));
            }
            if (cgSceneId.Length == 0)
            {
                throw new ArgumentException("CG 場景 ID 不可為空字串。", nameof(cgSceneId));
            }
        }

    }
}
