// ColdStatusSystem — 寒冷狀態系統。
// 監聽 MvpFireStateChangedEvent：火堆熄滅 → IsCold = true；重新點燃 → IsCold = false。
// 本身不觸發時間流逝，僅反映火堆狀態。狀態變更時發布 MvpColdStateChangedEvent。

using System;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village.Mvp
{
    /// <summary>
    /// 寒冷狀態系統：反映火堆是否處於熄滅狀態。
    /// ActionTimeManager 查詢此系統以決定是否套用冷卻倍率。
    /// </summary>
    public class ColdStatusSystem : IDisposable
    {
        private bool _isCold;
        private bool _subscribed;

        /// <summary>當前是否為寒冷狀態。</summary>
        public bool IsCold => _isCold;

        /// <summary>
        /// 建構並訂閱 MvpFireStateChangedEvent。
        /// 預設為寒冷狀態（火堆尚未點燃）。
        /// </summary>
        public ColdStatusSystem()
        {
            // 玩家初始狀態：火堆尚未點燃，但尚無「寒冷」概念觸發（Sprint X.4 寒冷只在火堆歸零時觸發）。
            // 選擇預設 false（未點燃過火堆時不視為寒冷），避免開局即寒冷影響搜索節奏。
            _isCold = false;

            EventBus.Subscribe<MvpFireStateChangedEvent>(OnFireStateChanged);
            _subscribed = true;
        }

        /// <summary>
        /// 火堆狀態變更時回調。
        /// 點燃 → 解除寒冷；熄滅（先前曾點燃）→ 進入寒冷。
        /// </summary>
        private void OnFireStateChanged(MvpFireStateChangedEvent e)
        {
            bool newCold;
            if (e.IsLit)
            {
                newCold = false;
            }
            else
            {
                // 熄滅代表火堆曾經點燃且現在歸零 → 進入寒冷
                newCold = true;
            }

            if (newCold != _isCold)
            {
                _isCold = newCold;
                EventBus.Publish(new MvpColdStateChangedEvent { IsCold = _isCold });
            }
        }

        public void Dispose()
        {
            if (_subscribed)
            {
                EventBus.Unsubscribe<MvpFireStateChangedEvent>(OnFireStateChanged);
                _subscribed = false;
            }
        }
    }
}
