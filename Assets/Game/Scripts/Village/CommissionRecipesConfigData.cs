// CommissionRecipesConfigData — 委託配方外部配置的 JSON DTO 與不可變配置物件。
// 配置檔路徑：Assets/Game/Resources/Config/commission-recipes-config.json
// 結構參照 GDD `commission-system.md` v1.1 § 3.2「單物品配方」：
//     配方 = 單一輸入物品 → 單一產出物品 + 倒數時間
// 雙層設計：DTO 供 JsonUtility 反序列化，不可變配置物件供 CommissionManager 查詢。
//
// 注意：此配置暫不經由 Google Sheets 管理，目前為 A7 placeholder，
// 正式版本再視需求決定是否遷移至 Google Sheets。

using System;
using System.Collections.Generic;

namespace ProjectDR.Village
{
    // ===== JSON DTO（供 JsonUtility.FromJson 使用） =====

    /// <summary>單一委託配方的 JSON DTO。</summary>
    [Serializable]
    public class CommissionRecipeEntry
    {
        /// <summary>配方唯一 ID（如 farm_tomato、witch_gem）。</summary>
        public string recipe_id;

        /// <summary>執行該配方的角色 ID（對應 CharacterIds：FarmGirl、Witch、Guard）。</summary>
        public string character_id;

        /// <summary>
        /// 輸入物品 ID。
        /// 守衛的空手委託此欄位為空字串。
        /// </summary>
        public string input_item_id;

        /// <summary>輸入物品數量。空手委託為 0。</summary>
        public int input_quantity;

        /// <summary>產出物品 ID（不可為空）。</summary>
        public string output_item_id;

        /// <summary>產出物品數量（必須 &gt; 0）。</summary>
        public int output_quantity;

        /// <summary>倒數時間（現實秒數）。</summary>
        public float duration_seconds;

        /// <summary>
        /// 該角色工作台的格子上限（決定該角色可同時進行多少委託）。
        /// 同一 character_id 的所有配方此欄位應一致。CommissionManager 取最大值為該角色的 slotCount。
        /// </summary>
        public int workbench_slot_index_max;

        /// <summary>配方描述（備註用）。</summary>
        public string description;
    }

    /// <summary>委託配方配置的完整 JSON DTO。</summary>
    [Serializable]
    public class CommissionRecipesConfigData
    {
        /// <summary>資料結構版本。</summary>
        public int schema_version;

        /// <summary>所有委託配方。</summary>
        public CommissionRecipeEntry[] recipes;
    }

    // ===== 不可變資料物件 =====

    /// <summary>單一委託配方的不可變資訊。</summary>
    public class CommissionRecipeInfo
    {
        /// <summary>配方 ID。</summary>
        public string RecipeId { get; }

        /// <summary>角色 ID。</summary>
        public string CharacterId { get; }

        /// <summary>輸入物品 ID（空手委託為空字串）。</summary>
        public string InputItemId { get; }

        /// <summary>輸入數量。</summary>
        public int InputQuantity { get; }

        /// <summary>產出物品 ID。</summary>
        public string OutputItemId { get; }

        /// <summary>產出數量。</summary>
        public int OutputQuantity { get; }

        /// <summary>倒數秒數。</summary>
        public float DurationSeconds { get; }

        /// <summary>對應角色的工作台格子數上限。</summary>
        public int WorkbenchSlotIndexMax { get; }

        /// <summary>配方描述。</summary>
        public string Description { get; }

        /// <summary>是否為空手委託（無輸入物品）。</summary>
        public bool IsEmptyHanded => string.IsNullOrEmpty(InputItemId) || InputQuantity <= 0;

        public CommissionRecipeInfo(
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
            RecipeId = recipeId;
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
    /// 從 CommissionRecipesConfigData（JSON DTO）建構，提供依角色、依輸入物品的查詢 API。
    /// </summary>
    public class CommissionRecipesConfig
    {
        private readonly Dictionary<string, CommissionRecipeInfo> _byRecipeId;
        private readonly Dictionary<string, List<CommissionRecipeInfo>> _byCharacter;
        // 鍵格式："characterId::inputItemId"，空手委託鍵為 "characterId::" (empty)
        private readonly Dictionary<string, CommissionRecipeInfo> _byCharacterAndInput;
        private readonly Dictionary<string, int> _slotCountByCharacter;
        private readonly IReadOnlyList<CommissionRecipeInfo> _allRecipes;

        /// <summary>所有配方的唯讀清單。</summary>
        public IReadOnlyList<CommissionRecipeInfo> AllRecipes => _allRecipes;

        /// <summary>
        /// 從 JSON DTO 建構不可變配置。
        /// </summary>
        /// <param name="data">JSON 反序列化後的 DTO（不可為 null）。</param>
        /// <exception cref="ArgumentNullException">data 為 null 時拋出。</exception>
        public CommissionRecipesConfig(CommissionRecipesConfigData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _byRecipeId = new Dictionary<string, CommissionRecipeInfo>();
            _byCharacter = new Dictionary<string, List<CommissionRecipeInfo>>();
            _byCharacterAndInput = new Dictionary<string, CommissionRecipeInfo>();
            _slotCountByCharacter = new Dictionary<string, int>();
            List<CommissionRecipeInfo> all = new List<CommissionRecipeInfo>();

            CommissionRecipeEntry[] entries = data.recipes ?? Array.Empty<CommissionRecipeEntry>();
            foreach (CommissionRecipeEntry entry in entries)
            {
                if (entry == null) continue;
                if (string.IsNullOrEmpty(entry.recipe_id)) continue;
                if (string.IsNullOrEmpty(entry.character_id)) continue;
                if (string.IsNullOrEmpty(entry.output_item_id)) continue;
                if (entry.output_quantity <= 0) continue;
                if (entry.duration_seconds <= 0f) continue;

                string normalizedInput = entry.input_item_id ?? string.Empty;
                CommissionRecipeInfo info = new CommissionRecipeInfo(
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
                // 同一 (character, input) 鍵若重複，保留先出現者（後續忽略，避免 IT 階段設定失誤造成例外）
                if (!_byCharacterAndInput.ContainsKey(inputKey))
                {
                    _byCharacterAndInput[inputKey] = info;
                }

                // 取該角色各配方中最大的 workbench_slot_index_max 作為 slot 數
                if (_slotCountByCharacter.TryGetValue(info.CharacterId, out int currentMax))
                {
                    if (info.WorkbenchSlotIndexMax > currentMax)
                    {
                        _slotCountByCharacter[info.CharacterId] = info.WorkbenchSlotIndexMax;
                    }
                }
                else
                {
                    _slotCountByCharacter[info.CharacterId] = info.WorkbenchSlotIndexMax;
                }

                all.Add(info);
            }

            _allRecipes = all.AsReadOnly();
        }

        /// <summary>依 recipe_id 取得配方。找不到回傳 null。</summary>
        public CommissionRecipeInfo GetRecipe(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return null;
            _byRecipeId.TryGetValue(recipeId, out CommissionRecipeInfo info);
            return info;
        }

        /// <summary>取得指定角色的所有配方。找不到回傳空清單。</summary>
        public IReadOnlyList<CommissionRecipeInfo> GetRecipesByCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return Array.AsReadOnly(Array.Empty<CommissionRecipeInfo>());
            }
            if (_byCharacter.TryGetValue(characterId, out List<CommissionRecipeInfo> list))
            {
                return list.AsReadOnly();
            }
            return Array.AsReadOnly(Array.Empty<CommissionRecipeInfo>());
        }

        /// <summary>
        /// 依角色 ID 與輸入物品 ID 取得對應配方。
        /// 空手委託：itemId 傳入 null 或空字串即可。
        /// 找不到回傳 null。
        /// </summary>
        public CommissionRecipeInfo GetRecipeByInputItem(string characterId, string itemId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;
            string key = BuildCharacterInputKey(characterId, itemId);
            _byCharacterAndInput.TryGetValue(key, out CommissionRecipeInfo info);
            return info;
        }

        /// <summary>
        /// 判斷指定物品是否可由指定角色處理。
        /// 實作 GDD § 3.4「物品層級」判斷。
        /// </summary>
        public bool CanCharacterProcessItem(string characterId, string itemId)
        {
            return GetRecipeByInputItem(characterId, itemId) != null;
        }

        /// <summary>
        /// 取得指定角色的工作台格子數。
        /// 規則：取該角色所有配方中 workbench_slot_index_max 的最大值；未配置角色回傳 0。
        /// </summary>
        public int GetWorkbenchSlotCount(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return 0;
            return _slotCountByCharacter.TryGetValue(characterId, out int count) ? count : 0;
        }

        /// <summary>取得所有已配置的角色 ID。</summary>
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
