namespace ProjectDR.Village
{
    /// <summary>
    /// 角色功能選單資料。
    /// 定義角色的顯示名稱、進入時播放的對話、以及可用的功能清單。
    /// </summary>
    public class CharacterMenuData
    {
        /// <summary>角色 ID（對應 CharacterIds 常數）。</summary>
        public string CharacterId { get; }

        /// <summary>角色顯示名稱（繁體中文）。</summary>
        public string DisplayName { get; }

        /// <summary>進入角色互動時播放的對話資料。</summary>
        public DialogueData Dialogue { get; }

        /// <summary>角色可用的功能 ID 陣列（如 "Storage", "Dialogue"）。</summary>
        public string[] FunctionIds { get; }

        /// <summary>
        /// 建立角色功能選單資料。
        /// </summary>
        public CharacterMenuData(string characterId, string displayName, DialogueData dialogue, string[] functionIds)
        {
            CharacterId = characterId;
            DisplayName = displayName;
            Dialogue = dialogue;
            FunctionIds = functionIds ?? System.Array.Empty<string>();
        }
    }
}
