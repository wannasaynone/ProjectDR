// MvpConfigData — MVP 遊戲循環的外部配置 JSON DTO 與不可變配置物件。
// 配置檔路徑：Assets/Game/Resources/Config/mvp-config.json
// 所有遊戲數值一律外部化至此檔，禁止寫死於程式碼中。

using System;
using System.Collections.Generic;

namespace ProjectDR.Village.Mvp
{
    // ===== JSON DTO（供 JsonUtility.FromJson 使用） =====

    [Serializable]
    public class MvpSearchConfigData
    {
        public float cooldownSeconds;
        public int woodGainPerSearch;
        public string[] feedbackLines;
    }

    [Serializable]
    public class MvpFireConfigData
    {
        public int unlockWoodThreshold;
        public int lightCost;
        public float durationSeconds;
        public int extendCost;
        public float extendSeconds;
    }

    [Serializable]
    public class MvpColdConfigData
    {
        public float actionCooldownMultiplier;
    }

    [Serializable]
    public class MvpHutConfigData
    {
        public int woodCost;
        public float buildSeconds;
        public int populationCapIncrement;
    }

    [Serializable]
    public class MvpPopulationConfigData
    {
        public int initialCap;
    }

    [Serializable]
    public class MvpDialogueConfigData
    {
        public int affinityGainPerDialogue;
        public float playerDialogueCooldownSeconds;
        public float dispatchCooldownMultiplier;
        public float npcInitiativeIntervalSeconds;
    }

    [Serializable]
    public class MvpPlaceholderCharacterData
    {
        public string characterId;
        public string displayName;
    }

    [Serializable]
    public class MvpPlaceholderDialogueData
    {
        public string[] characterInitiativeLines;
        public string[] playerInitiativeLines;
    }

    [Serializable]
    public class MvpConfigData
    {
        public MvpSearchConfigData search;
        public MvpFireConfigData fire;
        public MvpColdConfigData cold;
        public MvpHutConfigData hut;
        public MvpPopulationConfigData population;
        public MvpDialogueConfigData dialogue;
        public MvpPlaceholderCharacterData[] placeholderCharacters;
        public MvpPlaceholderDialogueData placeholderDialogue;
    }

    // ===== 不可變配置物件 =====

    /// <summary>
    /// MVP 遊戲循環的不可變配置。
    /// 從 MvpConfigData（JSON DTO）建構，提供防禦性拷貝的資料存取 API。
    /// </summary>
    public class MvpConfig
    {
        public float SearchCooldownSeconds { get; }
        public int SearchWoodGainPerSearch { get; }
        private readonly string[] _searchFeedbackLines;

        public int FireUnlockWoodThreshold { get; }
        public int FireLightCost { get; }
        public float FireDurationSeconds { get; }
        public int FireExtendCost { get; }
        public float FireExtendSeconds { get; }

        public float ColdActionCooldownMultiplier { get; }

        public int HutWoodCost { get; }
        public float HutBuildSeconds { get; }
        public int HutPopulationCapIncrement { get; }

        public int InitialPopulationCap { get; }

        public int DialogueAffinityGain { get; }
        public float PlayerDialogueCooldownSeconds { get; }
        public float DispatchCooldownMultiplier { get; }
        public float NpcInitiativeIntervalSeconds { get; }

        private readonly MvpPlaceholderCharacterData[] _placeholderCharacters;
        private readonly string[] _characterInitiativeLines;
        private readonly string[] _playerInitiativeLines;

        /// <summary>
        /// 從 JSON DTO 建構不可變配置。
        /// </summary>
        /// <param name="data">JSON 反序列化後的 DTO。</param>
        /// <exception cref="ArgumentNullException">data 為 null 時拋出。</exception>
        /// <exception cref="ArgumentException">任何關鍵欄位不合法時拋出。</exception>
        public MvpConfig(MvpConfigData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.search == null) throw new ArgumentException("search 欄位為 null。", nameof(data));
            if (data.fire == null) throw new ArgumentException("fire 欄位為 null。", nameof(data));
            if (data.cold == null) throw new ArgumentException("cold 欄位為 null。", nameof(data));
            if (data.hut == null) throw new ArgumentException("hut 欄位為 null。", nameof(data));
            if (data.population == null) throw new ArgumentException("population 欄位為 null。", nameof(data));
            if (data.dialogue == null) throw new ArgumentException("dialogue 欄位為 null。", nameof(data));

            if (data.search.cooldownSeconds <= 0f) throw new ArgumentException("search.cooldownSeconds 必須大於 0。");
            if (data.search.woodGainPerSearch <= 0) throw new ArgumentException("search.woodGainPerSearch 必須大於 0。");
            if (data.fire.unlockWoodThreshold < 0) throw new ArgumentException("fire.unlockWoodThreshold 不可為負。");
            if (data.fire.lightCost < 0) throw new ArgumentException("fire.lightCost 不可為負。");
            if (data.fire.durationSeconds <= 0f) throw new ArgumentException("fire.durationSeconds 必須大於 0。");
            if (data.fire.extendCost < 0) throw new ArgumentException("fire.extendCost 不可為負。");
            if (data.fire.extendSeconds <= 0f) throw new ArgumentException("fire.extendSeconds 必須大於 0。");
            if (data.cold.actionCooldownMultiplier < 1f) throw new ArgumentException("cold.actionCooldownMultiplier 必須 >= 1。");
            if (data.hut.woodCost < 0) throw new ArgumentException("hut.woodCost 不可為負。");
            if (data.hut.buildSeconds <= 0f) throw new ArgumentException("hut.buildSeconds 必須大於 0。");
            if (data.hut.populationCapIncrement <= 0) throw new ArgumentException("hut.populationCapIncrement 必須大於 0。");
            if (data.population.initialCap < 0) throw new ArgumentException("population.initialCap 不可為負。");
            if (data.dialogue.affinityGainPerDialogue <= 0) throw new ArgumentException("dialogue.affinityGainPerDialogue 必須大於 0。");
            if (data.dialogue.playerDialogueCooldownSeconds < 0f) throw new ArgumentException("dialogue.playerDialogueCooldownSeconds 不可為負。");
            if (data.dialogue.dispatchCooldownMultiplier < 1f) throw new ArgumentException("dialogue.dispatchCooldownMultiplier 必須 >= 1。");
            if (data.dialogue.npcInitiativeIntervalSeconds <= 0f) throw new ArgumentException("dialogue.npcInitiativeIntervalSeconds 必須大於 0。");

            SearchCooldownSeconds = data.search.cooldownSeconds;
            SearchWoodGainPerSearch = data.search.woodGainPerSearch;
            _searchFeedbackLines = data.search.feedbackLines != null
                ? (string[])data.search.feedbackLines.Clone()
                : Array.Empty<string>();

            FireUnlockWoodThreshold = data.fire.unlockWoodThreshold;
            FireLightCost = data.fire.lightCost;
            FireDurationSeconds = data.fire.durationSeconds;
            FireExtendCost = data.fire.extendCost;
            FireExtendSeconds = data.fire.extendSeconds;

            ColdActionCooldownMultiplier = data.cold.actionCooldownMultiplier;

            HutWoodCost = data.hut.woodCost;
            HutBuildSeconds = data.hut.buildSeconds;
            HutPopulationCapIncrement = data.hut.populationCapIncrement;

            InitialPopulationCap = data.population.initialCap;

            DialogueAffinityGain = data.dialogue.affinityGainPerDialogue;
            PlayerDialogueCooldownSeconds = data.dialogue.playerDialogueCooldownSeconds;
            DispatchCooldownMultiplier = data.dialogue.dispatchCooldownMultiplier;
            NpcInitiativeIntervalSeconds = data.dialogue.npcInitiativeIntervalSeconds;

            _placeholderCharacters = data.placeholderCharacters != null
                ? (MvpPlaceholderCharacterData[])data.placeholderCharacters.Clone()
                : Array.Empty<MvpPlaceholderCharacterData>();

            if (data.placeholderDialogue != null)
            {
                _characterInitiativeLines = data.placeholderDialogue.characterInitiativeLines != null
                    ? (string[])data.placeholderDialogue.characterInitiativeLines.Clone()
                    : Array.Empty<string>();
                _playerInitiativeLines = data.placeholderDialogue.playerInitiativeLines != null
                    ? (string[])data.placeholderDialogue.playerInitiativeLines.Clone()
                    : Array.Empty<string>();
            }
            else
            {
                _characterInitiativeLines = Array.Empty<string>();
                _playerInitiativeLines = Array.Empty<string>();
            }
        }

        /// <summary>取得搜索隨機回饋文字（唯讀）。</summary>
        public IReadOnlyList<string> SearchFeedbackLines => Array.AsReadOnly(_searchFeedbackLines);

        /// <summary>取得 Placeholder 角色清單（唯讀）。</summary>
        public IReadOnlyList<MvpPlaceholderCharacterData> PlaceholderCharacters => Array.AsReadOnly(_placeholderCharacters);

        /// <summary>取得角色主動發話 placeholder 文本池（唯讀）。</summary>
        public IReadOnlyList<string> CharacterInitiativeLines => Array.AsReadOnly(_characterInitiativeLines);

        /// <summary>取得玩家主動發話 placeholder 文本池（唯讀）。</summary>
        public IReadOnlyList<string> PlayerInitiativeLines => Array.AsReadOnly(_playerInitiativeLines);
    }
}
