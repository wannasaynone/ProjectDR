// ResourceManager — MVP 版資源管理器。
// 管理木材等資源數值，提供 Add / Spend / Get API，變更時發布 MvpResourceChangedEvent。
// 純邏輯類別（非 MonoBehaviour）。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village.Mvp
{
    /// <summary>
    /// MVP 資源管理器：以 resourceId (string) 管理多種資源數量。
    /// 提供 Add / Spend / Has / GetAmount API，變更時發布事件。
    /// </summary>
    public class ResourceManager
    {
        private readonly Dictionary<string, int> _amounts = new Dictionary<string, int>();

        /// <summary>取得指定資源的當前數量。未初始化的資源回傳 0。</summary>
        /// <exception cref="ArgumentNullException">resourceId 為 null 時拋出。</exception>
        /// <exception cref="ArgumentException">resourceId 為空字串時拋出。</exception>
        public int GetAmount(string resourceId)
        {
            ValidateResourceId(resourceId);
            if (_amounts.TryGetValue(resourceId, out int value))
            {
                return value;
            }
            return 0;
        }

        /// <summary>檢查指定資源是否達到要求數量。</summary>
        /// <exception cref="ArgumentNullException">resourceId 為 null 時拋出。</exception>
        /// <exception cref="ArgumentException">resourceId 為空字串或 requiredAmount 為負時拋出。</exception>
        public bool Has(string resourceId, int requiredAmount)
        {
            ValidateResourceId(resourceId);
            if (requiredAmount < 0)
            {
                throw new ArgumentException("requiredAmount 不可為負。", nameof(requiredAmount));
            }
            return GetAmount(resourceId) >= requiredAmount;
        }

        /// <summary>增加指定資源數量，發布 MvpResourceChangedEvent。</summary>
        /// <exception cref="ArgumentNullException">resourceId 為 null 時拋出。</exception>
        /// <exception cref="ArgumentException">resourceId 為空字串或 amount &lt;= 0 時拋出。</exception>
        public void Add(string resourceId, int amount)
        {
            ValidateResourceId(resourceId);
            if (amount <= 0)
            {
                throw new ArgumentException("amount 必須大於 0。", nameof(amount));
            }

            int oldValue = GetAmount(resourceId);
            int newValue = oldValue + amount;
            _amounts[resourceId] = newValue;

            EventBus.Publish(new MvpResourceChangedEvent
            {
                ResourceId = resourceId,
                NewAmount = newValue,
                Delta = amount
            });
        }

        /// <summary>
        /// 嘗試消耗指定資源數量。資源不足回傳 false 且不扣除。
        /// 成功時發布 MvpResourceChangedEvent。
        /// </summary>
        /// <exception cref="ArgumentNullException">resourceId 為 null 時拋出。</exception>
        /// <exception cref="ArgumentException">resourceId 為空字串或 amount &lt;= 0 時拋出。</exception>
        public bool TrySpend(string resourceId, int amount)
        {
            ValidateResourceId(resourceId);
            if (amount <= 0)
            {
                throw new ArgumentException("amount 必須大於 0。", nameof(amount));
            }

            int oldValue = GetAmount(resourceId);
            if (oldValue < amount)
            {
                return false;
            }

            int newValue = oldValue - amount;
            _amounts[resourceId] = newValue;

            EventBus.Publish(new MvpResourceChangedEvent
            {
                ResourceId = resourceId,
                NewAmount = newValue,
                Delta = -amount
            });
            return true;
        }

        private static void ValidateResourceId(string resourceId)
        {
            if (resourceId == null) throw new ArgumentNullException(nameof(resourceId));
            if (resourceId.Length == 0) throw new ArgumentException("resourceId 不可為空字串。", nameof(resourceId));
        }
    }
}
