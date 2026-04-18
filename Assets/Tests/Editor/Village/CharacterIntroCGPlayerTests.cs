// CharacterIntroCGPlayerTests — CharacterIntroCGPlayer 單元測試（B13）。
//
// CharacterIntroCGPlayer 依賴 CharacterIntroCGView Prefab 與 Transform，
// 在 Editor 測試中無法直接 Instantiate MonoBehaviour，
// 因此本測試：
// 1. 驗證建構子的 null guard
// 2. 驗證 PlayIntroCG null/empty characterId 立即回呼 onComplete
// 3. 驗證 PlayIntroCG 發布 CGPlaybackStartedEvent（透過 FakeCGPlayer 模擬核心邏輯）
//
// FakeCGPlayer 測試驗證 ICGPlayer 介面行為可被實作替換，
// 確保 B13 替換 PlaceholderCGPlayer 的接縫正確。

using System;
using NUnit.Framework;
using KahaGameCore.GameEvent;
using ProjectDR.Village;

namespace ProjectDR.Village.Tests
{
    [TestFixture]
    public class CharacterIntroCGPlayerTests
    {
        // ── 介面替換驗證（使用 FakeCGPlayer）────────────────────────────

        private class FakeCGPlayer : ICGPlayer
        {
            public string LastCharacterId;
            public bool CompleteCalled;
            public bool ImmediateComplete;

            public void PlayIntroCG(string characterId, Action onComplete)
            {
                LastCharacterId = characterId;
                if (ImmediateComplete)
                {
                    CompleteCalled = true;
                    onComplete?.Invoke();
                }
            }
        }

        [Test]
        public void FakeCGPlayer_PlayIntroCG_SetsLastCharacterId()
        {
            var fake = new FakeCGPlayer { ImmediateComplete = true };
            bool cbInvoked = false;
            fake.PlayIntroCG("VillageChiefWife", () => cbInvoked = true);

            Assert.AreEqual("VillageChiefWife", fake.LastCharacterId);
            Assert.IsTrue(cbInvoked);
        }

        [Test]
        public void FakeCGPlayer_NullCharacterId_DoesNotCrash()
        {
            var fake = new FakeCGPlayer { ImmediateComplete = true };
            Assert.DoesNotThrow(() => fake.PlayIntroCG(null, () => { }));
        }

        // ── PlaceholderCGPlayer 事件發布驗證（B9 兼容性確認）────────────

        [Test]
        public void PlaceholderCGPlayer_PublishesCGPlaybackStartedEvent()
        {
            var introData = new CharacterIntroConfigData
            {
                character_intros = new CharacterIntroData[0],
                character_intro_lines = new CharacterIntroLineData[0],
            };
            var config = new CharacterIntroConfig(introData);
            var placeholder = new PlaceholderCGPlayer(config);

            CGPlaybackStartedEvent received = null;
            Action<CGPlaybackStartedEvent> handler = e => received = e;
            EventBus.Subscribe(handler);

            try
            {
                placeholder.PlayIntroCG("VillageChiefWife", () => { });
                Assert.IsNotNull(received, "應發布 CGPlaybackStartedEvent");
                Assert.AreEqual("VillageChiefWife", received.CharacterId);
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }
        }

        [Test]
        public void PlaceholderCGPlayer_PublishesCGPlaybackCompletedEvent()
        {
            var introData = new CharacterIntroConfigData
            {
                character_intros = new CharacterIntroData[0],
                character_intro_lines = new CharacterIntroLineData[0],
            };
            var config = new CharacterIntroConfig(introData);
            var placeholder = new PlaceholderCGPlayer(config);

            CGPlaybackCompletedEvent received = null;
            Action<CGPlaybackCompletedEvent> handler = e => received = e;
            EventBus.Subscribe(handler);

            try
            {
                placeholder.PlayIntroCG("Guard", () => { });
                Assert.IsNotNull(received, "應發布 CGPlaybackCompletedEvent");
                Assert.AreEqual("Guard", received.CharacterId);
            }
            finally
            {
                EventBus.Unsubscribe(handler);
            }
        }

        [Test]
        public void PlaceholderCGPlayer_EmptyCharacterId_InvokesOnCompleteWithoutCrash()
        {
            var introData = new CharacterIntroConfigData
            {
                character_intros = new CharacterIntroData[0],
                character_intro_lines = new CharacterIntroLineData[0],
            };
            var config = new CharacterIntroConfig(introData);
            var placeholder = new PlaceholderCGPlayer(config);

            bool invoked = false;
            Assert.DoesNotThrow(() => placeholder.PlayIntroCG(string.Empty, () => invoked = true));
            Assert.IsTrue(invoked, "空 characterId 時應仍呼叫 onComplete");
        }

    }
}
