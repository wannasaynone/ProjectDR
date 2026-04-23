// CommissionRecipesConfigData — 委託配方外部配置的 IGameData DTO 與不可變配置物件。
// 對應 Sheets 分頁：CommissionRecipes
// 對應 .txt 檔：commissionrecipes.txt
//
// Sprint 8 Wave 2.5 重構：
//   - CommissionRecipeEntry 改名為 CommissionRecipeData（去 Entry）
//   - 廢棄包裹類 CommissionRecipesConfigData（純陣列格式）
//   - CommissionRecipesConfig 建構子改為接受 CommissionRecipeData[]
//   - duration_seconds 型別保持 float（KGC 工具匯出支援 float）
// ADR-001 / ADR-002 A06

using System;
using System.Collections.Generic;

namespace ProjectDR.Village.Commission
{
    // ===== JSON DTO（供 JsonFx 反序列化純陣列使用） =====

    /// <summary>
    /// 單一委託配方（JSON DTO）。
    /// 實作 IGameData，int id 為流水號主鍵，recipe_id 為語意字串外鍵。
    /// 對應 Sheets 分頁 CommissionRecipes，.txt 檔 commissionrecipes.txt。
    /// </summary>
    [Serializable]
    public class CommissionRecipeData : KahaGameCore.GameData.IGameData
    {
        /// <summary>IGameData 主鍵（流水號）。對應 JSON 欄位 "id"。</summary>
        public int id;

        /// <summary>IGameData 契約實作。</summary>
        public int ID => id;

        /// <summary>配方語意識別符。</summary>
        public string recipe_id;

        /// <summary>語意字串 Key。</summary>
        public string Key => recipe_id;

        /// <summary>執行該配方的角色 ID。</summary>
        public string character_id;

        /// <summary>輸入物品 ID（守衛的空手委託此欄位為空字串）。</summary>
        public string input_item_id;

        /// <summary>輸入物品數量（空手委託為 0）。</summary>
        public int input_quantity;

        /// <summary>產出物品 ID（不可為空）。</summary>
        public string output_item_id;

        /// <summary>產出物品數量（必須 > 0）。</summary>
        public int output_quantity;

        /// <summary>倒數時間（秒）。</summary>
        public float duration_seconds;

        /// <summary>工作台格子上限（同角色各配方應一致；CommissionManager 取最大值）。</summary>
        public int workbench_slot_index_max;

        /// <summary>設計備忘（可為空）。</summary>
        public string description;
    }

    // ===== 不可變資料物件 =====

    /// <summary>單一委託配方的不可變資訊。</summary>
    public class CommissionRecipeInfo
    {
        public int ID { get; }
        public string Key { get; }
        public string RecipeId => Key;
        public string CharacterId { get; }
        public string InputItemId { get; }
        public int InputQuantity { get; }
        public string OutputItemId { get; }
        public int OutputQuantity { get; }
        public float DurationSeconds { get; }
        public int WorkbenchSlotIndexMax { get; }
        public string Description { get; }
        public bool IsEmptyHanded => string.IsNullOrEmpty(InputItemId) || InputQuantity <= 0;

        public CommissionRecipeInfo(
            int id,
            string recipeId,
            string characterId,
            string inputItemId,
            int inputQuantity,
            string outputItemId,
            int outputQuantity,
            float durationSeconds,
            int workbenchSlotIndexMax,
            string description)
        {
            ID = id;
            Key = recipeId;
            CharacterId = characterId;
            InputItemId = inputItemId ?? string.Empty;
            InputQuantity = inputQuantity;
            OutputItemId = outputItemId;
            OutputQuantity = outputQuantity;
            DurationSeconds = durationSeconds;
            WorkbenchSlotIndexMax = workbenchSlotIndexMax;
            Description = description ?? string.Empty;
        }
    }

    // ===== 不可變配置物件 =====

    /// <summary>
    /// 委託配方配置（不可變）。
    /// 從 CommissionRecipeData[]（純陣列 JSON DTO）建構。
    /// </summary>
    public class CommissionRecipesConfig
    {
        private readonly Dictionary<string, CommissionRecipeInfo> _byRecipeId;
        private readonly Dictionary<string, List<CommissionRecipeInfo>> _byCharacter;
        private readonly Dictionary<string, CommissionRecipeInfo> _byCharacterAndInput;
        private readonly Dictionary<string, int> _slotCountByCharacter;
        private readonly IReadOnlyList<CommissionRecipeInfo> _allRecipes;

        public IReadOnlyList<CommissionRecipeInfo> AllRecipes => _allRecipes;

        /// <summary>
        /// 從純陣列 DTO 建構不可變配置。
        /// </summary>
        /// <param name="entries">JsonFx 反序列化後的 CommissionRecipeData 陣列（不可為 null）。</param>
        public CommissionRecipesConfig(CommissionRecipeData[] entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            _byRecipeId = new Dictionary<string, CommissionRecipeInfo>();
            _byCharacter = new Dictionary<string, List<CommissionRecipeInfo>>();
            _byCharacterAndInput = new Dictionary<string, CommissionRecipeInfo>();
            _slotCountByCharacter = new Dictionary<string, int>();
            List<CommissionRecipeInfo> all = new List<CommissionRecipeInfo>();

            foreach (CommissionRecipeData entry in entries)
            {
                if (entry == null) continue;
                if (string.IsNullOrEmpty(entry.recipe_id)) continue;
                if (string.IsNullOrEmpty(entry.character_id)) continue;
                if (string.IsNullOrEmpty(entry.output_item_id)) continue;
                if (entry.output_quantity <= 0) continue;
                if (entry.duration_seconds <= 0f) continue;

                string normalizedInput = entry.input_item_id ?? string.Empty;
                CommissionRecipeInfo info = new CommissionRecipeInfo(
                    entry.id,
                    entry.recipe_id,
                    entry.character_id,
                    normalizedInput,
                    entry.input_quantity,
                    entry.output_item_id,
                    entry.output_quantity,
                    entry.duration_seconds,
                    entry.workbench_slot_index_max,
                    entry.description);

                _byRecipeId[info.RecipeId] = info;

                if (!_byCharacter.TryGetValue(info.CharacterId, out List<CommissionRecipeInfo> list))
                {
                    list = new List<CommissionRecipeInfo>();
                    _byCharacter[info.CharacterId] = list;
                }
                list.Add(info);

                string inputKey = BuildCharacterInputKey(info.CharacterId, info.InputItemId);
                if (!_byCharacterAndInput.ContainsKey(inputKey))
                {
                    _byCharacterAndInput[inputKey] = info;
                }

                if (_slotCountByCharacter.TryGetValue(info.CharacterId, out int currentMax))
                {
                    if (info.WorkbenchSlotIndexMax > currentMax)
                        _slotCountByCharacter[info.CharacterId] = info.WorkbenchSlotIndexMax;
                }
                else
                {
                    _slotCountByCharacter[info.CharacterId] = info.WorkbenchSlotIndexMax;
                }

                all.Add(info);
            }

            _allRecipes = all.AsReadOnly();
        }

        public CommissionRecipeInfo GetRecipe(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return null;
            _byRecipeId.TryGetValue(recipeId, out CommissionRecipeInfo info);
            return info;
        }

        public IReadOnlyList<CommissionRecipeInfo> GetRecipesByCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return Array.AsReadOnly(Array.Empty<CommissionRecipeInfo>());
            if (_byCharacter.TryGetValue(characterId, out List<CommissionRecipeInfo> list))
                return list.AsReadOnly();
            return Array.AsReadOnly(Array.Empty<CommissionRecipeInfo>());
        }

        public CommissionRecipeInfo GetRecipeByInputItem(string characterId, string itemId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;
            string key = BuildCharacterInputKey(characterId, itemId);
            _byCharacterAndInput.TryGetValue(key, out CommissionRecipeInfo info);
            return info;
        }

        public bool CanCharacterProcessItem(string characterId, string itemId)
        {
            return GetRecipeByInputItem(characterId, itemId) != null;
        }

        public int GetWorkbenchSlotCount(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return 0;
            return _slotCountByCharacter.TryGetValue(characterId, out int count) ? count : 0;
        }

        public IReadOnlyCollection<string> GetConfiguredCharacterIds()
        {
            return _slotCountByCharacter.Keys;
        }

        private static string BuildCharacterInputKey(string characterId, string itemId)
        {
            string normalized = itemId ?? string.Empty;
            return characterId + "::" + normalized;
        }
    }
}
