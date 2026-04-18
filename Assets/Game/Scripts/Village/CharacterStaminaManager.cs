// CharacterStaminaManager — 角色體力管理器（Sprint 5 B13）。
//
// 依 character-content-template.md v1.4 §2.2：
// - 每角色獨立體力條
// - 玩家發問時消耗（a 路徑不扣、b 路徑扣）
// - 探索返回、贈送食品時恢復
// - 體力為 0 時點「對話」→ UI 顯示「現在好累了」（由 View 層判斷、此處僅提供 HasEnough 查詢）
//
// IT 階段 placeholder 數值：
// - Max = 10，每次發問扣 1（實際數值由數值設計師 TBD）
//
// 純邏輯類別（不依賴 MonoBehaviour）。體力狀態為 session 記憶體，不持久化。

using System.Collections.Generic;

namespace ProjectDR.Village
{
    public class CharacterStaminaManager
    {
        private const int DefaultMaxStamina = 10;
        private const int DefaultConsumePerDialogue = 1;

        private readonly int _maxStamina;
        private readonly int _consumePerDialogue;
        private readonly Dictionary<string, int> _staminaByCharacter;

        public CharacterStaminaManager() : this(DefaultMaxStamina, DefaultConsumePerDialogue) { }

        public CharacterStaminaManager(int maxStamina, int consumePerDialogue)
        {
            _maxStamina = maxStamina > 0 ? maxStamina : DefaultMaxStamina;
            _consumePerDialogue = consumePerDialogue > 0 ? consumePerDialogue : DefaultConsumePerDialogue;
            _staminaByCharacter = new Dictionary<string, int>();
        }

        public int MaxStamina => _maxStamina;
        public int ConsumePerDialogue => _consumePerDialogue;

        /// <summary>取得當前體力（未設定時視為滿值 = MaxStamina）。</summary>
        public int GetStamina(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return 0;
            return _staminaByCharacter.TryGetValue(characterId, out int v) ? v : _maxStamina;
        }

        /// <summary>體力是否足夠發動一次玩家發問。</summary>
        public bool HasEnoughForDialogue(string characterId)
        {
            return GetStamina(characterId) >= _consumePerDialogue;
        }

        /// <summary>
        /// 嘗試消耗一次玩家發問所需體力。
        /// 體力不足時回 false 且不扣除。
        /// </summary>
        public bool TryConsumeForDialogue(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return false;
            int current = GetStamina(characterId);
            if (current < _consumePerDialogue) return false;
            _staminaByCharacter[characterId] = current - _consumePerDialogue;
            return true;
        }

        /// <summary>恢復指定值（用於送食品 / 探索返回）。</summary>
        public void Restore(string characterId, int amount)
        {
            if (string.IsNullOrEmpty(characterId) || amount <= 0) return;
            int current = GetStamina(characterId);
            int next = current + amount;
            if (next > _maxStamina) next = _maxStamina;
            _staminaByCharacter[characterId] = next;
        }

        /// <summary>測試友善：直接設定體力值（不檢查上下限）。</summary>
        public void SetStamina(string characterId, int value)
        {
            if (string.IsNullOrEmpty(characterId)) return;
            if (value < 0) value = 0;
            if (value > _maxStamina) value = _maxStamina;
            _staminaByCharacter[characterId] = value;
        }
    }
}
