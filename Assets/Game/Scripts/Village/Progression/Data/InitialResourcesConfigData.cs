// InitialResourcesConfigData — 初始資源發放配置的 JSON DTO 與不可變配置物件。
// 配置檔路徑：Assets/Game/Resources/Config/initial-resources-config.json
// 此配置不經由 Google Sheets 管理，因為 IT 階段解鎖事件與贈送物資為簡易固定值，
// 正式版本再視需求決定是否遷移至 Google Sheets。
//
// ADR-001 / ADR-002 A11 改造（2026-04-22）：
//   InitialResourceGrantData 實作 KahaGameCore.GameData.IGameData，
//   加 int id 欄位（流水號主鍵）+ 保留 grant_id（語意字串外鍵）。
//   InitialResourceGrant 新增 int ID + string Key 屬性。
//   同時清除廢棄觸發器常數（UnlockFarmGirl / UnlockWitch / GuardReturnEvent）。

using System;
using System.Collections.Generic;

namespace ProjectDR.Village.Progression
{
    // ===== 觸發器 ID 常數 =====

    /// <summary>初始資源 grant 的觸發器 ID 常數（對應 JSON trigger_id 欄位）。</summary>
    public static class InitialResourcesTriggerIds
    {
        /// <summary>節點 0 開局。</summary>
        public const string Node0Start = "node0_start";

        /// <summary>玩家主動向守衛發問「要拿劍」特殊題成功（Sprint 6 擴張）。</summary>
        public const string GuardSwordAsked = "guard_sword_asked";
    }

    // ===== JSON DTO（供 JsonUtility.FromJson 使用） =====

    /// <summary>
    /// 單筆資源發放的配置項（JSON DTO）。
    /// 實作 IGameData，int id 為流水號主鍵，grant_id 為語意字串外鍵。
    /// </summary>
    [Serializable]
    public class InitialResourceGrantData : KahaGameCore.GameData.IGameData
    {
        /// <summary>IGameData 主鍵（流水號）。對應 JSON 欄位 "id"。</summary>
        public int id;

        /// <summary>IGameData 契約實作。回傳 int id 流水號。</summary>
        public int ID => id;

        /// <summary>grant 唯一 ID。</summary>
        public string grant_id;

        /// <summary>觸發器 ID（見 InitialResourcesTriggerIds）。</summary>
        public string trigger_id;

        /// <summary>贈送的物品 ID（可為空字串，表示只執行觸發、不給物品）。</summary>
        public string item_id;

        /// <summary>贈送數量（item_id 為空時此欄位被忽略）。</summary>
        public int quantity;

        /// <summary>描述（撰寫者備註）。</summary>
        public string description;
    }

    /// <summary>初始資源配置的完整外部資料（JSON DTO）。</summary>
    [Serializable]
    public class InitialResourcesConfigData
    {
        /// <summary>資料結構版本。</summary>
        public int schema_version;

        /// <summary>所有資源發放項。</summary>
        public InitialResourceGrantData[] grants;
    }

    // ===== 不可變資料物件 =====

    /// <summary>單筆資源發放的不可變資訊。</summary>
    public class InitialResourceGrant
    {
        /// <summary>IGameData 流水號主鍵（對應 InitialResourceGrantData.id）。</summary>
        public int ID { get; }

        /// <summary>語意字串外鍵（對應 grant_id，為 IGameData Key 慣例）。</summary>
        public string Key { get; }

        /// <summary>grant ID（同 Key，保留向後相容）。</summary>
        public string GrantId => Key;

        /// <summary>觸發器 ID。</summary>
        public string TriggerId { get; }

        /// <summary>贈送物品 ID。空字串表示純標記用，不實際給物品。</summary>
        public string ItemId { get; }

        /// <summary>贈送數量。</summary>
        public int Quantity { get; }

        /// <summary>描述。</summary>
        public string Description { get; }

        /// <summary>此筆 grant 是否實際發放物品（ItemId 非空且 Quantity &gt; 0）。</summary>
        public bool HasItem => !string.IsNullOrEmpty(ItemId) && Quantity > 0;

        public InitialResourceGrant(
            int id,
            string grantId,
            string triggerId,
            string itemId,
            int quantity,
            string description)
        {
            ID = id;
            Key = grantId;
            TriggerId = triggerId;
            ItemId = itemId;
            Quantity = quantity;
            Description = description;
        }
    }

    // ===== 不可變配置物件 =====

    /// <summary>
    /// 初始資源配置（不可變）。
    /// 從 InitialResourcesConfigData（JSON DTO）建構，提供依 GrantId / TriggerId 查詢 API。
    /// </summary>
    public class InitialResourcesConfig
    {
        private readonly Dictionary<string, InitialResourceGrant> _grantsByGrantId;
        private readonly Dictionary<string, List<InitialResourceGrant>> _grantsByTriggerId;

        /// <summary>
        /// 從 JSON DTO 建構不可變配置。
        /// </summary>
        /// <param name="data">JSON 反序列化後的 DTO。</param>
        /// <exception cref="ArgumentNullException">data 為 null 時拋出。</exception>
        public InitialResourcesConfig(InitialResourcesConfigData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _grantsByGrantId = new Dictionary<string, InitialResourceGrant>();
            _grantsByTriggerId = new Dictionary<string, List<InitialResourceGrant>>();

            InitialResourceGrantData[] grants = data.grants ?? Array.Empty<InitialResourceGrantData>();
            foreach (InitialResourceGrantData grant in grants)
            {
                if (grant == null || string.IsNullOrEmpty(grant.grant_id))
                {
                    continue;
                }

                InitialResourceGrant info = new InitialResourceGrant(
                    grant.id,
                    grant.grant_id,
                    grant.trigger_id ?? string.Empty,
                    grant.item_id ?? string.Empty,
                    grant.quantity,
                    grant.description ?? string.Empty);

                _grantsByGrantId[grant.grant_id] = info;

                if (!string.IsNullOrEmpty(info.TriggerId))
                {
                    if (!_grantsByTriggerId.TryGetValue(info.TriggerId, out List<InitialResourceGrant> list))
                    {
                        list = new List<InitialResourceGrant>();
                        _grantsByTriggerId[info.TriggerId] = list;
                    }
                    list.Add(info);
                }
            }
        }

        /// <summary>依 grant_id 取得單筆資源發放資訊。找不到時回傳 null。</summary>
        public InitialResourceGrant GetGrant(string grantId)
        {
            if (string.IsNullOrEmpty(grantId)) return null;
            _grantsByGrantId.TryGetValue(grantId, out InitialResourceGrant grant);
            return grant;
        }

        /// <summary>依 trigger_id 取得所有對應的資源發放項（可能為 0 筆、1 筆、或多筆）。</summary>
        public IReadOnlyList<InitialResourceGrant> GetGrantsByTrigger(string triggerId)
        {
            if (string.IsNullOrEmpty(triggerId))
            {
                return Array.AsReadOnly(Array.Empty<InitialResourceGrant>());
            }

            if (_grantsByTriggerId.TryGetValue(triggerId, out List<InitialResourceGrant> list))
            {
                return list.AsReadOnly();
            }
            return Array.AsReadOnly(Array.Empty<InitialResourceGrant>());
        }
    }
}
