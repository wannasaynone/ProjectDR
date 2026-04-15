// CGGalleryView -- CG 回憶圖鑑畫面（overlay 模式）。
// 顯示指定角色已解鎖的 CG 場景清單，點擊可重播 HCG 劇情。

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectDR.Village.UI
{
    /// <summary>
    /// CG 回憶圖鑑畫面（overlay 模式）。
    /// 顯示指定角色已解鎖的 CG 場景清單，
    /// 點擊可透過 HCGDialogueSetup 重播 HCG 劇情。
    /// </summary>
    public class CGGalleryView : ViewBase
    {
        [Header("場景清單")]
        [SerializeField] private Transform _sceneListContainer;
        [SerializeField] private Button _sceneButtonPrefab;

        [Header("提示")]
        [SerializeField] private TMP_Text _emptyHintLabel;

        [Header("導航")]
        [SerializeField] private Button _returnButton;

        private CGUnlockManager _unlockManager;
        private HCGDialogueSetup _hcgSetup;
        private string _characterId;

        private System.Action _returnAction;
        private bool _isPlayingScene;

        /// <summary>
        /// 由 VillageEntryPoint 注入相依。
        /// </summary>
        /// <param name="unlockManager">CG 解鎖管理器。</param>
        /// <param name="hcgSetup">HCG 劇情播放整合層。</param>
        /// <param name="characterId">當前角色 ID。</param>
        public void Initialize(CGUnlockManager unlockManager, HCGDialogueSetup hcgSetup, string characterId)
        {
            _unlockManager = unlockManager;
            _hcgSetup = hcgSetup;
            _characterId = characterId;
        }

        /// <summary>
        /// 設定返回按鈕觸發的回呼（overlay 模式）。
        /// </summary>
        public void SetReturnAction(System.Action returnAction)
        {
            _returnAction = returnAction;
        }

        protected override void OnShow()
        {
            if (_returnButton != null)
            {
                _returnButton.onClick.AddListener(OnReturnClicked);
            }

            RefreshSceneList();
        }

        protected override void OnHide()
        {
            if (_returnButton != null)
            {
                _returnButton.onClick.RemoveListener(OnReturnClicked);
            }

            ClearSceneList();
        }

        private void RefreshSceneList()
        {
            ClearSceneList();

            if (_unlockManager == null || string.IsNullOrEmpty(_characterId))
            {
                ShowEmptyHint(true);
                return;
            }

            IReadOnlyList<CGSceneInfo> unlockedScenes = _unlockManager.GetUnlockedScenes(_characterId);

            if (unlockedScenes.Count == 0)
            {
                ShowEmptyHint(true);
                return;
            }

            ShowEmptyHint(false);

            for (int i = 0; i < unlockedScenes.Count; i++)
            {
                CGSceneInfo scene = unlockedScenes[i];
                CreateSceneButton(scene);
            }
        }

        private void CreateSceneButton(CGSceneInfo scene)
        {
            if (_sceneButtonPrefab == null || _sceneListContainer == null) return;

            Button button = Instantiate(_sceneButtonPrefab, _sceneListContainer);
            button.gameObject.SetActive(true);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = scene.DisplayName;
            }

            int dialogueId = scene.DialogueId;
            button.onClick.AddListener(() => OnSceneClicked(dialogueId));
        }

        private void OnSceneClicked(int dialogueId)
        {
            if (_isPlayingScene || _hcgSetup == null) return;

            _isPlayingScene = true;

            // 隱藏 Gallery UI（但不 Hide，因為 HCG 播放完要回來）
            if (_sceneListContainer != null)
            {
                _sceneListContainer.gameObject.SetActive(false);
            }
            if (_returnButton != null)
            {
                _returnButton.gameObject.SetActive(false);
            }

            _hcgSetup.PlayCGScene(dialogueId, OnCGSceneCompleted);
        }

        private void OnCGSceneCompleted()
        {
            _isPlayingScene = false;

            // 恢復 Gallery UI
            if (_sceneListContainer != null)
            {
                _sceneListContainer.gameObject.SetActive(true);
            }
            if (_returnButton != null)
            {
                _returnButton.gameObject.SetActive(true);
            }
        }

        private void ClearSceneList()
        {
            if (_sceneListContainer == null) return;

            for (int i = _sceneListContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_sceneListContainer.GetChild(i).gameObject);
            }
        }

        private void ShowEmptyHint(bool show)
        {
            if (_emptyHintLabel != null)
            {
                _emptyHintLabel.gameObject.SetActive(show);
                if (show)
                {
                    _emptyHintLabel.text = "尚未解鎖任何回憶";
                }
            }
        }

        private void OnReturnClicked()
        {
            if (_isPlayingScene) return; // 播放中不允許返回

            _returnAction?.Invoke();
        }
    }
}
