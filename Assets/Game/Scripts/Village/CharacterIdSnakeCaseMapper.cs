// CharacterIdSnakeCaseMapper — Sprint 5 對話功能修正專用（B4/B15/B18）。
//
// 背景：
// - Sprint 4 以前：CharacterIds 常數用 PascalCase，JSON 配置也用 PascalCase
// - Sprint 5 新增配置（character-questions / greeting / idle-chat）採 snake_case，
//   以對齊設計部產出的文件命名慣例（村長夫人 = village_chief_wife）
//
// 為維持 Sprint 4 既有配置不變，新 config Manager 在建構時將 JSON 的 snake_case
// 映射為 CharacterIds 常數（PascalCase），使業務邏輯層統一使用 CharacterIds。

namespace ProjectDR.Village
{
    internal static class CharacterIdSnakeCaseMapper
    {
        /// <summary>
        /// 將 JSON 中的 snake_case character_id 轉為 CharacterIds 常數格式（PascalCase）。
        /// 找不到對應時原樣返回（允許未知角色 ID 透傳而不崩潰）。
        /// </summary>
        public static string ToPascal(string snakeCase)
        {
            if (string.IsNullOrEmpty(snakeCase)) return snakeCase;
            switch (snakeCase)
            {
                case "village_chief_wife": return CharacterIds.VillageChiefWife;
                case "farm_girl":          return CharacterIds.FarmGirl;
                case "witch":              return CharacterIds.Witch;
                case "guard":              return CharacterIds.Guard;
                default:                   return snakeCase;
            }
        }
    }
}
