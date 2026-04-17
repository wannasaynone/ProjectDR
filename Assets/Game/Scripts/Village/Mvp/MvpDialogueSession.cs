// MvpDialogueSession — MVP 對話 session 協調器。
// 整合既有 DialogueManager（對話文字推進）與既有 AffinityManager（好感度 +N）。
// 由 UI 層呼叫 StartPlayerInitiatedDialogue / StartCharacterInitiatedDialogue 啟動，
// 結束時（DialogueCompletedEvent）自動 +N 並發布 MvpDialogueSessionCompletedEvent。
// 同時負責啟動玩家對話冷卻 / 消費角色主動 Ready 狀態。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;

namespace ProjectDR.Village.Mvp
{
    /// <summary>
    /// MVP 對話 session 協調器。
    /// 整合 DialogueManager + AffinityManager + DialogueCooldownManager + NPCInitiativeManager。
    /// </summary>
    public class MvpDialogueSession : IDisposable
    {
        private readonly DialogueManager _dialogueManager;
        private readonly AffinityManager _affinityManager;
        private readonly DialogueCooldownManager _cooldownManager;
        private readonly NPCInitiativeManager _initiativeManager;
        private readonly MvpConfig _config;
        private readonly IRandomSource _random;

        private string _currentCharacterId;
        private MvpDialogueDirection _currentDirection;
        private bool _active;
        private bool _subscribed;

        public bool IsActive => _active;
        public string CurrentCharacterId => _currentCharacterId;
        public MvpDialogueDirection CurrentDirection => _currentDirection;

        public MvpDialogueSession(
            DialogueManager dialogueManager,
            AffinityManager affinityManager,
            DialogueCooldownManager cooldownManager,
            NPCInitiativeManager initiativeManager,
            MvpConfig config,
            IRandomSource random)
        {
            _dialogueManager = dialogueManager ?? throw new ArgumentNullException(nameof(dialogueManager));
            _affinityManager = affinityManager ?? throw new ArgumentNullException(nameof(affinityManager));
            _cooldownManager = cooldownManager ?? throw new ArgumentNullException(nameof(cooldownManager));
            _initiativeManager = initiativeManager ?? throw new ArgumentNullException(nameof(initiativeManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _random = random ?? throw new ArgumentNullException(nameof(random));

            EventBus.Subscribe<DialogueCompletedEvent>(OnDialogueCompleted);
            _subscribed = true;
        }

        /// <summary>
        /// 玩家主動發起對話（玩家提問角色方向）。
        /// - 若已在對話中：回傳 false。
        /// - 若玩家冷卻中 + 角色 Initiative 未 Ready：回傳 false。
        /// - 若玩家冷卻中但角色 Initiative Ready：允許進入（紅點忽略玩家冷卻，採用角色主動路徑）。
        /// 成功時啟動對話 + 啟動玩家冷卻（非紅點路徑）或 消費 Initiative Ready（紅點路徑）。
        /// </summary>
        public bool TryStartPlayerInitiatedDialogue(string characterId)
        {
            ValidateId(characterId);
            if (_active) return false;

            bool initiativeReady = _initiativeManager.IsReady(characterId);
            bool playerOnCooldown = _cooldownManager.IsOnCooldown(characterId);

            MvpDialogueDirection direction;
            if (initiativeReady)
            {
                // 紅點路徑：無論玩家冷卻，皆可進入。方向 = CharacterInitiative（角色提問玩家）
                direction = MvpDialogueDirection.CharacterInitiative;
                _initiativeManager.ConsumeInitiative(characterId);
                // 紅點路徑下不啟動玩家冷卻（紅點忽略玩家冷卻）
            }
            else
            {
                if (playerOnCooldown) return false;
                direction = MvpDialogueDirection.PlayerInitiative;
                _cooldownManager.TryStartPlayerDialogueCooldown(characterId);
            }

            return BeginSession(characterId, direction);
        }

        /// <summary>
        /// 強制以「角色主動」方向啟動對話（例：UI 紅點被點擊）。
        /// 消費角色 Initiative Ready（若未 Ready 仍可強行啟動，視為提前接受）。
        /// </summary>
        public bool TryStartCharacterInitiatedDialogue(string characterId)
        {
            ValidateId(characterId);
            if (_active) return false;

            _initiativeManager.ConsumeInitiative(characterId);
            return BeginSession(characterId, MvpDialogueDirection.CharacterInitiative);
        }

        private bool BeginSession(string characterId, MvpDialogueDirection direction)
        {
            // 根據方向挑 placeholder 文本
            IReadOnlyList<string> pool = direction == MvpDialogueDirection.CharacterInitiative
                ? _config.CharacterInitiativeLines
                : _config.PlayerInitiativeLines;

            string line = pool.Count > 0
                ? pool[ClampIdx(_random.Range(0, pool.Count), pool.Count)]
                : "...";

            DialogueData data = new DialogueData(new[] { line });

            _currentCharacterId = characterId;
            _currentDirection = direction;
            _active = true;

            EventBus.Publish(new MvpDialogueSessionStartedEvent
            {
                CharacterId = characterId,
                Direction = direction
            });

            _dialogueManager.StartDialogue(data);
            return true;
        }

        /// <summary>
        /// 前進到下一行對話。若對話結束 DialogueManager 會發布 DialogueCompletedEvent，
        /// 觸發本類別的 OnDialogueCompleted 進行好感度 +N。
        /// </summary>
        public bool AdvanceDialogue() => _dialogueManager.Advance();

        private void OnDialogueCompleted(DialogueCompletedEvent e)
        {
            if (!_active) return;

            string charId = _currentCharacterId;
            MvpDialogueDirection dir = _currentDirection;
            int gain = _config.DialogueAffinityGain;

            _active = false;
            _currentCharacterId = null;

            _affinityManager.AddAffinity(charId, gain);

            EventBus.Publish(new MvpDialogueSessionCompletedEvent
            {
                CharacterId = charId,
                Direction = dir,
                AffinityGained = gain
            });
        }

        private static int ClampIdx(int idx, int count)
        {
            if (idx < 0) return 0;
            if (idx >= count) return count - 1;
            return idx;
        }

        public void Dispose()
        {
            if (_subscribed)
            {
                EventBus.Unsubscribe<DialogueCompletedEvent>(OnDialogueCompleted);
                _subscribed = false;
            }
        }

        private static void ValidateId(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (id.Length == 0) throw new ArgumentException("characterId 不可為空字串。", nameof(id));
        }
    }
}
