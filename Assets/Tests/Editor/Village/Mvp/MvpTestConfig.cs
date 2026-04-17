using ProjectDR.Village.Mvp;

namespace ProjectDR.Tests.Village.Mvp
{
    /// <summary>測試共用：生成合法的 MvpConfigData 與 placeholder。</summary>
    internal static class MvpTestConfig
    {
        public static MvpConfigData MakeDefault()
        {
            return new MvpConfigData
            {
                search = new MvpSearchConfigData
                {
                    cooldownSeconds = 1f,
                    woodGainPerSearch = 1,
                    feedbackLines = new[] { "撿到木頭。", "折下枯枝。" }
                },
                fire = new MvpFireConfigData
                {
                    unlockWoodThreshold = 5,
                    lightCost = 1,
                    durationSeconds = 60f,
                    extendCost = 1,
                    extendSeconds = 60f
                },
                cold = new MvpColdConfigData { actionCooldownMultiplier = 2f },
                hut = new MvpHutConfigData { woodCost = 10, buildSeconds = 10f, populationCapIncrement = 1 },
                population = new MvpPopulationConfigData { initialCap = 0 },
                dialogue = new MvpDialogueConfigData
                {
                    affinityGainPerDialogue = 3,
                    playerDialogueCooldownSeconds = 30f,
                    dispatchCooldownMultiplier = 2f,
                    npcInitiativeIntervalSeconds = 45f
                },
                placeholderCharacters = new[]
                {
                    new MvpPlaceholderCharacterData { characterId = "A", displayName = "倖存者 A" },
                    new MvpPlaceholderCharacterData { characterId = "B", displayName = "倖存者 B" },
                    new MvpPlaceholderCharacterData { characterId = "C", displayName = "倖存者 C" }
                },
                placeholderDialogue = new MvpPlaceholderDialogueData
                {
                    characterInitiativeLines = new[] { "ci line" },
                    playerInitiativeLines = new[] { "pi line" }
                }
            };
        }
    }

    /// <summary>測試共用確定性隨機來源（永遠回傳指定值或按序列取值）。</summary>
    internal class SequenceRandomSource : IRandomSource
    {
        private readonly int[] _sequence;
        private int _index;
        public SequenceRandomSource(params int[] sequence) { _sequence = sequence; }
        public int Range(int minInclusive, int maxExclusive)
        {
            if (_sequence.Length == 0) return minInclusive;
            int v = _sequence[_index % _sequence.Length];
            _index++;
            // 如序列值超出範圍，用取餘落回合法區間（寬鬆處理）
            int range = maxExclusive - minInclusive;
            if (range <= 0) return minInclusive;
            return minInclusive + ((v % range) + range) % range;
        }
    }

    /// <summary>固定回傳 0（選擇第一個候選）。</summary>
    internal class ZeroRandomSource : IRandomSource
    {
        public int Range(int minInclusive, int maxExclusive) => minInclusive;
    }
}
