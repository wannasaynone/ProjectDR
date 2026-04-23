// DialogueFlowInstaller — 對話流功能域 Installer（ADR-003 B4f / Sprint 7 E3）。
//
// 負責招呼語、角色發問、閒聊、玩家發問、倒數 CD、體力等對話相關 Manager 的建構、
// 事件訂閱與 Tick 驅動。
//
// Install 依賴：
//   - VillageContext（ctx 不可為 null；ctx.AffinityReadOnly 必須已由 AffinityInstaller 填入）
//   - 建構子注入：CharacterQuestionsConfigData、GreetingConfigData、IdleChatConfigData、
//                 RedDotManager（ProjectDR.Village.Progression，E5 已搬移完成）、
//                 float characterQuestionCountdownSeconds、float dialogueCooldownBaseSeconds
//
// 事件訂閱（3 對，對稱）：
//   - CommissionStartedEvent  → OnCommissionStartedForDialogue
//   - CommissionClaimedEvent  → OnCommissionClaimedForDialogue
//   - CharacterUnlockedEvent  → OnCharacterUnlockedForDialogueRedDot
//
// Tick 驅動：CharacterQuestionCountdownManager、DialogueCooldownManager
// 暴露至 VillageContext：無（ADR-003 D5 #6 DialogueFlow 無新 ctx 欄位）
//
// ADR 遵循：ADR-003（IVillageInstaller 契約）、ADR-004（Village/Core/ 路徑）、
//           ADR-002 A04/A07/A10（IGameData 改造後的 ConfigData 型別）

using ProjectDR.Village.Affinity;
using ProjectDR.Village.Progression;
using System;
using KahaGameCore.GameEvent;
using ProjectDR.Village.CharacterQuestions;
using ProjectDR.Village.CharacterStamina;
using ProjectDR.Village.IdleChat;
using ProjectDR.Village.Greeting;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.Dialogue;

namespace ProjectDR.Village.Core
{
    /// <summary>
    /// 對話流功能域 Installer（#6，最後安裝）。
    /// 純 POCO，禁止繼承 MonoBehaviour。
    /// 實作 IVillageTickable 以驅動 Countdown / Cooldown 兩個 Tick 類 Manager。
    /// </summary>
    public class DialogueFlowInstaller : IVillageInstaller, IVillageTickable
    {
        // ===== 建構子注入（由 VillageEntryPoint 傳入） =====

        private readonly CharacterQuestionData[] _questionEntries;
        private readonly CharacterQuestionOptionData[] _questionOptionEntries;
        private readonly CharacterProfileData[] _profileEntries;
        private readonly PersonalityAffinityRuleData[] _affinityRuleEntries;
        private readonly GreetingData[] _greetingEntries;
        private readonly IdleChatTopicData[] _idleChatTopicEntries;
        private readonly IdleChatAnswerData[] _idleChatAnswerEntries;
        private readonly RedDotManager _redDotManager;
        private readonly float _characterQuestionCountdownSeconds;
        private readonly float _dialogueCooldownBaseSeconds;

        // ===== Install 後的 Manager 實例 =====

        private CharacterQuestionsConfig _characterQuestionsConfig;
        private CharacterQuestionsManager _characterQuestionsManager;
        private CharacterQuestionCountdownManager _characterQuestionCountdownManager;
        private GreetingConfig _greetingConfig;
        private GreetingPresenter _greetingPresenter;
        private IdleChatConfig _idleChatConfig;
        private IdleChatPresenter _idleChatPresenter;
        private DialogueCooldownManager _dialogueCooldownManager;
        private CharacterStaminaManager _staminaManager;

        // ===== 事件 Handler 快取（供 Uninstall 對稱解除） =====

        private Action<CommissionStartedEvent> _onCommissionStarted;
        private Action<CommissionClaimedEvent> _onCommissionClaimed;
        private Action<CharacterUnlockedEvent> _onCharacterUnlocked;

        /// <summary>
        /// 建構 DialogueFlowInstaller，注入所需配置純陣列與依賴。
        /// </summary>
        /// <param name="questionEntries">角色發問主表 DTO 陣列（不可為 null）。</param>
        /// <param name="questionOptionEntries">角色發問選項子表 DTO 陣列（不可為 null）。</param>
        /// <param name="profileEntries">角色偏好個性 DTO 陣列（不可為 null）。</param>
        /// <param name="affinityRuleEntries">個性好感度規則 DTO 陣列（不可為 null）。</param>
        /// <param name="greetingEntries">招呼語 DTO 陣列（不可為 null）。</param>
        /// <param name="idleChatTopicEntries">閒聊主題主表 DTO 陣列（不可為 null）。</param>
        /// <param name="idleChatAnswerEntries">閒聊回答子表 DTO 陣列（不可為 null）。</param>
        /// <param name="redDotManager">
        ///   紅點管理器。可傳 null；null 時 GreetingPresenter 視為沒有紅點（一律播招呼語）。
        /// </param>
        /// <param name="characterQuestionCountdownSeconds">角色發問倒數秒數（預設 60s）。</param>
        /// <param name="dialogueCooldownBaseSeconds">玩家發問 CD 基礎秒數（預設 60s）。</param>
        public DialogueFlowInstaller(
            CharacterQuestionData[] questionEntries,
            CharacterQuestionOptionData[] questionOptionEntries,
            CharacterProfileData[] profileEntries,
            PersonalityAffinityRuleData[] affinityRuleEntries,
            GreetingData[] greetingEntries,
            IdleChatTopicData[] idleChatTopicEntries,
            IdleChatAnswerData[] idleChatAnswerEntries,
            RedDotManager redDotManager,
            float characterQuestionCountdownSeconds,
            float dialogueCooldownBaseSeconds)
        {
            _questionEntries = questionEntries
                ?? throw new ArgumentNullException(nameof(questionEntries));
            _questionOptionEntries = questionOptionEntries
                ?? throw new ArgumentNullException(nameof(questionOptionEntries));
            _profileEntries = profileEntries
                ?? throw new ArgumentNullException(nameof(profileEntries));
            _affinityRuleEntries = affinityRuleEntries
                ?? throw new ArgumentNullException(nameof(affinityRuleEntries));
            _greetingEntries = greetingEntries
                ?? throw new ArgumentNullException(nameof(greetingEntries));
            _idleChatTopicEntries = idleChatTopicEntries
                ?? throw new ArgumentNullException(nameof(idleChatTopicEntries));
            _idleChatAnswerEntries = idleChatAnswerEntries
                ?? throw new ArgumentNullException(nameof(idleChatAnswerEntries));
            _redDotManager = redDotManager; // 允許 null（GreetingPresenter 自行防守）
            _characterQuestionCountdownSeconds = characterQuestionCountdownSeconds > 0f
                ? characterQuestionCountdownSeconds : 60f;
            _dialogueCooldownBaseSeconds = dialogueCooldownBaseSeconds > 0f
                ? dialogueCooldownBaseSeconds : 60f;
        }

        // ===== IVillageInstaller =====

        /// <summary>
        /// 建構對話流各 Manager 並訂閱所需事件。
        /// 依賴 ctx 的欄位（由前序 Installer 填入）：無直接 ctx 欄位依賴（暫留 RedDotManager 透過建構子注入）。
        /// </summary>
        /// <param name="ctx">Village 跨 Installer 共用服務容器（不可為 null）。</param>
        /// <exception cref="InvalidOperationException">ctx 為 null 時拋出。</exception>
        public void Install(VillageContext ctx)
        {
            if (ctx == null)
                throw new InvalidOperationException("DialogueFlowInstaller.Install: ctx 不可為 null");

            // ===== 角色發問 =====
            _characterQuestionsConfig = new CharacterQuestionsConfig(
                _questionEntries, _questionOptionEntries, _profileEntries, _affinityRuleEntries);

            // AffinityInstaller（B4c）已將 AffinityManager 以 IAffinityQuery 暴露至 ctx.AffinityReadOnly。
            // CharacterQuestionsManager 需要寫入好感度（AddAffinity），因此 cast 回具體型別。
            // 此 cast 在框架內有保證：AffinityInstaller 一定先於 DialogueFlowInstaller 安裝。
            if (ctx.AffinityReadOnly == null)
                throw new InvalidOperationException(
                    "DialogueFlowInstaller.Install: ctx.AffinityReadOnly 尚未填入，請確認 AffinityInstaller 已先 Install。");

            Affinity.AffinityManager affinityManager = ctx.AffinityReadOnly as Affinity.AffinityManager
                ?? throw new InvalidOperationException(
                    "DialogueFlowInstaller.Install: ctx.AffinityReadOnly 不是 AffinityManager 實例。");

            _characterQuestionsManager = new CharacterQuestionsManager(
                _characterQuestionsConfig, affinityManager);

            _characterQuestionCountdownManager =
                new CharacterQuestionCountdownManager(_characterQuestionCountdownSeconds);

            // ===== 招呼語 =====
            _greetingConfig = new GreetingConfig(_greetingEntries);
            _greetingPresenter = new GreetingPresenter(_greetingConfig, _redDotManager);

            // ===== 閒聊 =====
            _idleChatConfig = new IdleChatConfig(_idleChatTopicEntries, _idleChatAnswerEntries);
            _idleChatPresenter = new IdleChatPresenter(_idleChatConfig);

            // ===== 對話 CD / 體力 =====
            _dialogueCooldownManager = new DialogueCooldownManager(_dialogueCooldownBaseSeconds);
            _staminaManager = new CharacterStaminaManager();

            // ===== 事件訂閱（3 對，對稱見 Uninstall） =====
            _onCommissionStarted = OnCommissionStartedForDialogue;
            _onCommissionClaimed = OnCommissionClaimedForDialogue;
            _onCharacterUnlocked = OnCharacterUnlockedForDialogueRedDot;

            EventBus.Subscribe(_onCommissionStarted);
            EventBus.Subscribe(_onCommissionClaimed);
            EventBus.Subscribe(_onCharacterUnlocked);
        }

        /// <summary>
        /// 解除所有事件訂閱、Dispose Manager。
        /// Install 訂閱幾次，Uninstall 就要解除幾次（對稱）。
        /// </summary>
        public void Uninstall()
        {
            // 解除事件訂閱（對稱 Install 的 3 次 Subscribe）
            if (_onCommissionStarted != null)
            {
                EventBus.Unsubscribe(_onCommissionStarted);
                _onCommissionStarted = null;
            }
            if (_onCommissionClaimed != null)
            {
                EventBus.Unsubscribe(_onCommissionClaimed);
                _onCommissionClaimed = null;
            }
            if (_onCharacterUnlocked != null)
            {
                EventBus.Unsubscribe(_onCharacterUnlocked);
                _onCharacterUnlocked = null;
            }

            // Dispose Tick 類 Manager
            if (_characterQuestionCountdownManager != null)
            {
                _characterQuestionCountdownManager.Dispose();
                _characterQuestionCountdownManager = null;
            }
            if (_dialogueCooldownManager != null)
            {
                _dialogueCooldownManager.Dispose();
                _dialogueCooldownManager = null;
            }

            // 清除其他 Manager 引用
            _characterQuestionsManager = null;
            _characterQuestionsConfig = null;
            _greetingPresenter = null;
            _greetingConfig = null;
            _idleChatPresenter = null;
            _idleChatConfig = null;
            _staminaManager = null;
        }

        // ===== IVillageTickable =====

        /// <summary>
        /// 每幀更新：驅動 CharacterQuestionCountdownManager 與 DialogueCooldownManager 的 Tick。
        /// </summary>
        /// <param name="deltaSeconds">unscaledDeltaTime（秒）。</param>
        public void Tick(float deltaSeconds)
        {
            _characterQuestionCountdownManager?.Tick(deltaSeconds);
            _dialogueCooldownManager?.Tick(deltaSeconds);
        }

        // ===== 公開 Accessor（供 VillageEntryPoint 或上層組裝 View 使用） =====

        /// <summary>角色發問 Manager（Install 後可用；Uninstall 後回 null）。</summary>
        public CharacterQuestionsManager CharacterQuestionsManager => _characterQuestionsManager;

        /// <summary>角色發問倒數 Manager（Install 後可用；Uninstall 後回 null）。</summary>
        public CharacterQuestionCountdownManager CharacterQuestionCountdownManager => _characterQuestionCountdownManager;

        /// <summary>招呼語 Presenter（Install 後可用；Uninstall 後回 null）。</summary>
        public GreetingPresenter GreetingPresenter => _greetingPresenter;

        /// <summary>閒聊 Presenter（Install 後可用；Uninstall 後回 null）。</summary>
        public IdleChatPresenter IdleChatPresenter => _idleChatPresenter;

        /// <summary>玩家發問 CD Manager（Install 後可用；Uninstall 後回 null）。</summary>
        public DialogueCooldownManager DialogueCooldownManager => _dialogueCooldownManager;

        /// <summary>體力 Manager（Install 後可用；Uninstall 後回 null）。</summary>
        public CharacterStaminaManager StaminaManager => _staminaManager;

        // ===== Event Handlers =====

        /// <summary>
        /// 委託開始 → 切換該角色為工作中。
        /// CharacterQuestionCountdownManager 暫停倒數、DialogueCooldownManager 啟用 ×2 倍率。
        /// （ADR-003 事件分散表：CommissionStartedEvent → DialogueFlowInstaller）
        /// </summary>
        private void OnCommissionStartedForDialogue(CommissionStartedEvent e)
        {
            if (e == null || string.IsNullOrEmpty(e.CharacterId)) return;
            _characterQuestionCountdownManager?.SetWorking(e.CharacterId, true);
            _dialogueCooldownManager?.SetWorking(e.CharacterId, true);
        }

        /// <summary>
        /// 委託領取（工作完成）→ 恢復該角色為非工作中。
        /// 依 GDD §1.2：工作完成領取的當下，若 L2 倒數已到則立刻亮紅點（由 Countdown Tick 自然處理）。
        /// （ADR-003 事件分散表：CommissionClaimedEvent → DialogueFlowInstaller）
        /// </summary>
        private void OnCommissionClaimedForDialogue(CommissionClaimedEvent e)
        {
            if (e == null || string.IsNullOrEmpty(e.CharacterId)) return;
            _characterQuestionCountdownManager?.SetWorking(e.CharacterId, false);
            _dialogueCooldownManager?.SetWorking(e.CharacterId, false);
        }

        /// <summary>
        /// 女角（農女/魔女）解鎖時，立刻在該角色的對話按鈕亮 L2 紅點，
        /// 讓玩家進入互動畫面即看到「可以對話」提示，不需等 60s 倒數。
        /// 守衛不在此範圍（守衛由歸來事件觸發，另行流程）。
        /// （ADR-003 事件分散表：CharacterUnlockedEvent → OnCharacterUnlockedForDialogueRedDot → DialogueFlowInstaller）
        /// </summary>
        private void OnCharacterUnlockedForDialogueRedDot(CharacterUnlockedEvent e)
        {
            if (_redDotManager == null || e == null || string.IsNullOrEmpty(e.CharacterId)) return;
            if (e.CharacterId != CharacterIds.FarmGirl && e.CharacterId != CharacterIds.Witch) return;
            _redDotManager.SetCharacterQuestionFlag(e.CharacterId, true);
        }
    }
}
