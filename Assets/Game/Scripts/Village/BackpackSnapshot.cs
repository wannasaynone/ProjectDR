// BackpackSnapshot — 背包快照（不可變）。
// 用於出發前拍攝背包狀態，死亡時可回溯至此快照。

using System.Collections.Generic;

namespace ProjectDR.Village
{
    /// <summary>
    /// 背包快照（不可變）。
    /// 儲存拍攝當下的所有格子狀態，供後續回溯使用。
    /// </summary>
    public class BackpackSnapshot
    {
        private readonly BackpackSlot[] _slots;

        /// <summary>快照中的格子數量。</summary>
        public int SlotCount => _slots.Length;

        public BackpackSnapshot(IReadOnlyList<BackpackSlot> slots)
        {
            _slots = new BackpackSlot[slots.Count];
            for (int i = 0; i < slots.Count; i++)
            {
                _slots[i] = slots[i];
            }
        }

        /// <summary>取得快照中所有格子的唯讀副本。</summary>
        public IReadOnlyList<BackpackSlot> GetSlots()
        {
            return _slots;
        }
    }
}
