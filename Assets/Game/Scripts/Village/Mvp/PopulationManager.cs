// PopulationManager — 村莊人口管理器。
// Cap：村莊可容納上限（初始值由 config 提供，蓋小屋時 IncreaseCap）。
// Count：當前人口數（NpcArrivalManager 呼叫 Increment 增加，上限為 Cap）。

using System;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village.Mvp
{
    /// <summary>
    /// 人口管理器：維護 Cap（上限）與 Count（當前人口）兩個值。
    /// IncreaseCap 增加容量並發布事件；Increment 增加當前人口（受 Cap 限制）。
    /// </summary>
    public class PopulationManager
    {
        private int _cap;
        private int _count;

        /// <summary>當前人口上限。</summary>
        public int Cap => _cap;

        /// <summary>當前人口數。</summary>
        public int Count => _count;

        /// <summary>是否還有空位（Count &lt; Cap）。</summary>
        public bool HasVacancy => _count < _cap;

        public PopulationManager(int initialCap)
        {
            if (initialCap < 0) throw new ArgumentException("initialCap 不可為負。", nameof(initialCap));
            _cap = initialCap;
            _count = 0;
        }

        /// <summary>增加人口上限並發布 MvpPopulationCapIncreasedEvent。</summary>
        public void IncreaseCap(int increment)
        {
            if (increment <= 0) throw new ArgumentException("increment 必須大於 0。", nameof(increment));
            _cap += increment;
            EventBus.Publish(new MvpPopulationCapIncreasedEvent
            {
                Increment = increment,
                NewCap = _cap
            });
        }

        /// <summary>
        /// 嘗試增加當前人口 1 位。若 Count 已達 Cap 回傳 false。
        /// 成功時發布 MvpPopulationChangedEvent。
        /// </summary>
        public bool TryIncrementCount()
        {
            if (_count >= _cap) return false;
            _count++;
            EventBus.Publish(new MvpPopulationChangedEvent
            {
                NewCount = _count,
                CurrentCap = _cap
            });
            return true;
        }
    }
}
