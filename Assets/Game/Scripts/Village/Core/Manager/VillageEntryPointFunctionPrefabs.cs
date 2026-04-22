// VillageEntryPoint partial — Function Prefab 注冊部分（Sprint 7 VillageEntryPoint 瘦身抽出）
// 將 RegisterFunctionPrefabs / RegisterCraftWorkbenchForFunction 與
// ExplorationDepartureInterceptorAdapter 從主檔移至此 partial，降低主檔行數。

using ProjectDR.Village.Navigation;
using ProjectDR.Village.Storage;
using ProjectDR.Village.Backpack;
using ProjectDR.Village.Gift;
using ProjectDR.Village.Farm;
using ProjectDR.Village.CG;
using ProjectDR.Village.CharacterInteraction;
using ProjectDR.Village.CharacterQuestions;
using ProjectDR.Village.IdleChat;
using ProjectDR.Village.Commission;
using ProjectDR.Village.Progression;
using ProjectDR.Village.Affinity;
using ProjectDR.Village.Alchemy;
using ProjectDR.Village.Guard;
using ProjectDR.Village.CharacterUnlock;
using ProjectDR.Village.Exploration;
using ProjectDR.Village.UI;

namespace ProjectDR.Village.Core
{
    public partial class VillageEntryPoint
    {
        // ===== Function Prefab 注冊（CharacterInteractionView 功能面板）=====

        private void RegisterFunctionPrefabs(CharacterInteractionView interactionView)
        {
            if (_storageViewPrefab != null)
                interactionView.RegisterFunctionPrefab(AreaIds.Storage, _storageViewPrefab, v =>
                {
                    StorageAreaView sv = v as StorageAreaView;
                    if (sv != null) { sv.Initialize(_storageManager, _backpackManager, _transferManager, _navigationManager); sv.SetReturnAction(() => interactionView.CloseOverlay()); }
                });

            if (_explorationViewPrefab != null)
                interactionView.RegisterFunctionPrefab(AreaIds.Exploration, _explorationViewPrefab, v =>
                {
                    ExplorationAreaView ev = v as ExplorationAreaView;
                    if (ev != null) { ev.Initialize(_explorationManager, _navigationManager); ev.SetReturnAction(() => interactionView.CloseOverlay()); }
                });

            if (_alchemyViewPrefab != null)
                interactionView.RegisterFunctionPrefab(AreaIds.Alchemy, _alchemyViewPrefab, v =>
                {
                    AlchemyAreaView av = v as AlchemyAreaView;
                    if (av != null) { av.Initialize(_navigationManager); av.SetReturnAction(() => interactionView.CloseOverlay()); }
                });

            if (_farmViewPrefab != null)
                interactionView.RegisterFunctionPrefab(AreaIds.Farm, _farmViewPrefab, v =>
                {
                    FarmAreaView fv = v as FarmAreaView;
                    if (fv != null) { fv.Initialize(_farmManager, _storageManager, _itemTypeResolver, _navigationManager); fv.SetReturnAction(() => interactionView.CloseOverlay()); }
                });

            if (_galleryViewPrefab != null)
                interactionView.RegisterFunctionPrefab(FunctionIds.Gallery, _galleryViewPrefab, v =>
                {
                    CGGalleryView gv = v as CGGalleryView;
                    if (gv != null) { gv.Initialize(_cgUnlockManager, _hcgDialogueSetup, interactionView.CurrentCharacterId); gv.SetReturnAction(() => interactionView.CloseOverlay()); }
                });

            if (_giftViewPrefab != null)
                interactionView.RegisterFunctionPrefab(FunctionIds.Gift, _giftViewPrefab, v =>
                {
                    GiftAreaView gv = v as GiftAreaView;
                    if (gv != null) { gv.Initialize(_giftManager, _affinityManager, _backpackManager, _storageManager, interactionView.CurrentCharacterId); gv.SetReturnAction(() => interactionView.CloseOverlay()); }
                });

            if (_craftWorkbenchPrefab != null)
            {
                RegisterCraftWorkbenchForFunction(interactionView, FunctionIds.CommissionFarm);
                RegisterCraftWorkbenchForFunction(interactionView, FunctionIds.CommissionAlchemy);
                RegisterCraftWorkbenchForFunction(interactionView, FunctionIds.CommissionScout);
            }

            if (_playerQuestionsViewPrefab != null && _playerQuestionsConfig != null)
                interactionView.RegisterFunctionPrefab(FunctionIds.Dialogue, _playerQuestionsViewPrefab, v =>
                {
                    PlayerQuestionsView pv = v as PlayerQuestionsView;
                    if (pv != null)
                    {
                        string charId = interactionView.CurrentCharacterId;
                        pv.Initialize(_playerQuestionsManager, _playerQuestionsConfig, _idleChatPresenter, _staminaManager, _dialogueCooldownManager, _redDotManager, charId, _typewriterCharsPerSecond);
                        pv.SetReturnAction(() => interactionView.CloseOverlay());
                        pv.SetResponseAction(r => interactionView.PlayDialogue(new string[] { r }));
                        pv.SetSingleUseQuestionDependencies(_initialResourceDispatcher, _initialResourcesConfig);
                    }
                });

            if (_characterQuestionsViewPrefab != null && _characterQuestionsConfig != null)
                interactionView.RegisterFunctionPrefab(FunctionIds.CharacterQuestion, _characterQuestionsViewPrefab, v =>
                {
                    CharacterQuestionsView cqv = v as CharacterQuestionsView;
                    if (cqv != null)
                    {
                        string charId = interactionView.CurrentCharacterId;
                        cqv.Initialize(_characterQuestionsManager, _characterQuestionsConfig, _affinityManager, _characterQuestionCountdownManager, _redDotManager, charId, level: 1, _typewriterCharsPerSecond);
                        cqv.SetReturnAction(() => interactionView.CloseOverlay());
                        cqv.SetResponseAction(r => interactionView.PlayDialogue(new string[] { r }));
                    }
                });
        }

        private void RegisterCraftWorkbenchForFunction(CharacterInteractionView interactionView, string functionId)
        {
            if (_craftWorkbenchPrefab == null || _commissionManager == null) return;
            interactionView.RegisterFunctionPrefab(functionId, _craftWorkbenchPrefab, v =>
            {
                CraftWorkbenchView wv = v as CraftWorkbenchView;
                if (wv != null)
                {
                    wv.Initialize(_commissionManager, _commissionRecipesConfig, _backpackManager, _storageManager);
                    wv.SetCharacter(interactionView.CurrentCharacterId);
                    wv.SetReturnAction(() => interactionView.CloseOverlay());
                }
            });
        }

        // ===== 探索出發攔截器（inner class，與 Guard/CharacterUnlock 跨域聯動）=====

        private class ExplorationDepartureInterceptorAdapter : IExplorationDepartureInterceptor
        {
            private readonly GuardReturnEventController _guardReturnController;
            private readonly CharacterUnlockManager _unlockManager;

            public ExplorationDepartureInterceptorAdapter(
                GuardReturnEventController guardReturnController,
                CharacterUnlockManager unlockManager)
            {
                _guardReturnController = guardReturnController;
                _unlockManager = unlockManager;
            }

            public bool TryIntercept()
            {
                if (_guardReturnController == null || _unlockManager == null) return false;
                if (_guardReturnController.HasTriggered) return false;
                if (_unlockManager.IsUnlocked(CharacterIds.Guard)) return false;
                return _guardReturnController.TriggerEvent();
            }
        }
    }
}
