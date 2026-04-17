using NUnit.Framework;
using ProjectDR.Village.Mvp;
using UnityEngine;

namespace ProjectDR.Tests.Village.Mvp
{
    /// <summary>
    /// 驗證 Resources/Config/mvp-config.json 能被 JsonUtility 正確反序列化且通過 MvpConfig 建構驗證。
    /// 這是 Phase 5 整合驗收 + G3「config 化驗證」的關鍵測試。
    /// </summary>
    [TestFixture]
    public class MvpConfigJsonLoadTests
    {
        [Test]
        public void LoadsFromResources_AndBuildsValidConfig()
        {
            TextAsset asset = Resources.Load<TextAsset>("Config/mvp-config");
            Assert.IsNotNull(asset, "Resources/Config/mvp-config.json 必須存在。");
            MvpConfigData data = JsonUtility.FromJson<MvpConfigData>(asset.text);
            Assert.IsNotNull(data);
            MvpConfig cfg = new MvpConfig(data);

            // 確認關鍵值符合 Sprint 4 規格
            Assert.AreEqual(1f, cfg.SearchCooldownSeconds, 0.001f, "搜索冷卻 = 1 秒");
            Assert.AreEqual(1, cfg.SearchWoodGainPerSearch, "搜索每次 +1 木材");
            Assert.AreEqual(5, cfg.FireUnlockWoodThreshold, "生火解鎖門檻 = 5 木材");
            Assert.AreEqual(1, cfg.FireLightCost, "生火耗材 1 木材");
            Assert.AreEqual(60f, cfg.FireDurationSeconds, 0.001f, "火堆 60 秒");
            Assert.AreEqual(1, cfg.FireExtendCost, "延長耗材 1 木材");
            Assert.AreEqual(60f, cfg.FireExtendSeconds, 0.001f, "延長 +60 秒");
            Assert.AreEqual(2f, cfg.ColdActionCooldownMultiplier, 0.001f, "寒冷倍率 ×2");
            Assert.AreEqual(10, cfg.HutWoodCost, "蓋屋 10 木材");
            Assert.AreEqual(1, cfg.HutPopulationCapIncrement, "蓋屋 +1 人口上限");
            Assert.AreEqual(3, cfg.DialogueAffinityGain, "對話 +3 好感度");
            Assert.Greater(cfg.PlayerDialogueCooldownSeconds, 0f, "玩家對話冷卻 > 0");
            Assert.GreaterOrEqual(cfg.DispatchCooldownMultiplier, 1f, "派遣倍率 >= 1（Sprint 4 保留欄位）");
            Assert.Greater(cfg.NpcInitiativeIntervalSeconds, 0f, "主動發話間隔 > 0");
            Assert.Greater(cfg.SearchFeedbackLines.Count, 0, "搜索回饋文本不可為空");
            Assert.Greater(cfg.PlaceholderCharacters.Count, 0, "placeholder 角色不可為空");
            Assert.Greater(cfg.CharacterInitiativeLines.Count, 0, "角色主動對話文本不可為空");
            Assert.Greater(cfg.PlayerInitiativeLines.Count, 0, "玩家主動對話文本不可為空");
        }
    }
}
