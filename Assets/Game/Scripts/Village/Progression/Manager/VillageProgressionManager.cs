using System.Collections.Generic;
using KahaGameCore.GameEvent;
using ProjectDR.Village.Navigation;

namespace ProjectDR.Village.Progression
{
    /// <summary>
    /// 村莊解鎖進度管理器。
    /// 管理哪些區域已解鎖，並提供推進與強制解鎖功能。
    /// 初始狀態僅 Storage 已解鎖。
    /// </summary>
    public class VillageProgressionManager
    {
        private readonly List<string> _unlockedAreas = new List<string>();

        public VillageProgressionManager()
        {
            // 初始狀態：僅 Storage 已解鎖
            _unlockedAreas.Add(AreaIds.Storage);
        }

        /// <summary>查詢指定區域是否已解鎖。</summary>
        public bool IsAreaUnlocked(string areaId)
        {
            return _unlockedAreas.Contains(areaId);
        }

        /// <summary>取得所有已解鎖的區域 ID 清單（唯讀）。</summary>
        public IReadOnlyList<string> GetUnlockedAreas()
        {
            return _unlockedAreas.AsReadOnly();
        }

        /// <summary>
        /// 嘗試根據解鎖條件推進村莊進度。
        /// 若有新區域被解鎖，回傳 true；否則回傳 false。
        /// IT 階段：無自動解鎖條件，永遠回傳 false。
        /// </summary>
        public bool TryAdvanceProgression()
        {
            // IT 階段：自動解鎖條件尚未定義，永遠回傳 false
            // 正式版本應依 GDD 定義的解鎖條件（如完成特定任務）判斷
            return false;
        }

        /// <summary>
        /// 強制解鎖指定區域。
        /// 若區域已解鎖，則忽略（不重複添加、不發布事件）。
        /// 成功解鎖後發布 AreaUnlockedEvent。
        /// </summary>
        public void ForceUnlock(string areaId)
        {
            if (_unlockedAreas.Contains(areaId))
            {
                return;
            }

            _unlockedAreas.Add(areaId);
            EventBus.Publish(new AreaUnlockedEvent { AreaId = areaId });
        }
    }
}
