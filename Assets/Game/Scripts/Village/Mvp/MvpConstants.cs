// MvpConstants — MVP 模組的結構性常數。
// 這些是資源 ID / 冷卻 key 等結構性識別字串，不屬於遊戲數值，不需外部化。

namespace ProjectDR.Village.Mvp
{
    /// <summary>MVP 資源 ID。</summary>
    public static class MvpResourceIds
    {
        public const string Wood = "Wood";
    }

    /// <summary>ActionTimeManager 使用的冷卻 key。</summary>
    public static class MvpActionKeys
    {
        public const string Search = "Search";
        public const string FireExtend = "FireExtend";
        public const string HutBuild = "HutBuild";
    }
}
