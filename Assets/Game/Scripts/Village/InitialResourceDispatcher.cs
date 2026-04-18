// InitialResourceDispatcher — 初始資源實際發放實作。
// 依據 GDD `character-unlock-system.md` § 4.3：
//   所有初始資源發放遵循 commission-system.md 的「先背包後倉庫」邏輯。
//
// 本實作依序嘗試：
//   1. 先放入背包（TryAddItem），取得實際加入量
//   2. 剩餘數量放入倉庫（TryAddItem）
//   3. 若倉庫亦無法全部容納，則記錄未處理量（GDD 9.3 TBD，IT 階段採丟棄 + 警告 log）

using System;
using UnityEngine;

namespace ProjectDR.Village
{
    /// <summary>
    /// 依據 GDD「先背包後倉庫」邏輯的初始資源發放器。
    /// 實作 IInitialResourceDispatcher 介面，由 CharacterUnlockManager 呼叫。
    /// </summary>
    public class InitialResourceDispatcher : IInitialResourceDispatcher
    {
        private readonly BackpackManager _backpack;
        private readonly StorageManager _storage;

        public InitialResourceDispatcher(BackpackManager backpack, StorageManager storage)
        {
            _backpack = backpack ?? throw new ArgumentNullException(nameof(backpack));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        /// <inheritdoc />
        public void Dispatch(InitialResourceGrant grant)
        {
            if (grant == null || !grant.HasItem)
            {
                return;
            }

            int remaining = grant.Quantity;

            // 1. 先放背包
            int addedToBackpack = _backpack.AddItem(grant.ItemId, remaining);
            remaining -= addedToBackpack;

            if (remaining <= 0)
            {
                return;
            }

            // 2. 剩餘放倉庫
            int addedToStorage = _storage.TryAddItem(grant.ItemId, remaining);
            remaining -= addedToStorage;

            // 3. 仍有剩餘 → IT 階段記錄警告（GDD TBD）
            if (remaining > 0)
            {
                Debug.LogWarning(
                    $"[InitialResourceDispatcher] Grant '{grant.GrantId}' 剩餘 {remaining} 個 {grant.ItemId} 無法放入背包或倉庫（兩者皆已滿），已丟棄。");
            }
        }
    }
}
