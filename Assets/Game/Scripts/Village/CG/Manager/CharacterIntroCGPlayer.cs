// CharacterIntroCGPlayer — ICGPlayer 的真正實作（B13）。
//
// 替換 PlaceholderCGPlayer，提供視覺化的 CG + 短劇情播放。
//
// 流程：
// 1. 從 CharacterIntroConfig 取得該角色的 intro 資料
// 2. 從 Resources/CG/ 載入 CgSpriteId 對應的 Sprite（找不到則用 null，View 顯示 placeholder 色塊）
// 3. Instantiate CharacterIntroCGView Prefab 到 _uiContainer
// 4. 發布 CGPlaybackStartedEvent
// 5. View 播放完成後：發布 CGPlaybackCompletedEvent + 呼叫 onComplete + 銷毀 View

using System;
using KahaGameCore.GameEvent;
using UnityEngine;
using ProjectDR.Village.UI;
using ProjectDR.Village.Navigation;
using ProjectDR.Village.CharacterIntro;

namespace ProjectDR.Village.CG
{
    /// <summary>
    /// ICGPlayer 的視覺化實作。
    /// 在 _uiContainer 下 Instantiate CharacterIntroCGView Prefab 播放登場 CG + 短劇情。
    /// </summary>
    public class CharacterIntroCGPlayer : ICGPlayer
    {
        private readonly CharacterIntroConfig _introConfig;
        private readonly CharacterIntroCGView _cgViewPrefab;
        private readonly Transform _uiContainer;
        private readonly float _charsPerSecond;

        // 目前播放中的 View 實例（防止重複呼叫）
        private CharacterIntroCGView _currentView;

        /// <summary>
        /// 建構 CG 播放器。
        /// </summary>
        /// <param name="introConfig">角色登場配置（不可為 null）。</param>
        /// <param name="cgViewPrefab">CharacterIntroCGView Prefab（不可為 null）。</param>
        /// <param name="uiContainer">Instantiate 的父 Transform（不可為 null）。</param>
        /// <param name="charsPerSecond">打字機速度（>0）。</param>
        public CharacterIntroCGPlayer(
            CharacterIntroConfig introConfig,
            CharacterIntroCGView cgViewPrefab,
            Transform uiContainer,
            float charsPerSecond)
        {
            _introConfig    = introConfig    ?? throw new ArgumentNullException(nameof(introConfig));
            _cgViewPrefab   = cgViewPrefab   ?? throw new ArgumentNullException(nameof(cgViewPrefab));
            _uiContainer    = uiContainer    ?? throw new ArgumentNullException(nameof(uiContainer));
            _charsPerSecond = charsPerSecond > 0f ? charsPerSecond : 20f;
        }

        /// <inheritdoc />
        public void PlayIntroCG(string characterId, Action onComplete)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                onComplete?.Invoke();
                return;
            }

            // 防止重複播放
            if (_currentView != null)
            {
                Debug.LogWarning("[CharacterIntroCGPlayer] 已有 CG 播放中，忽略重複呼叫。");
                return;
            }

            // 取得 intro 資料
            CharacterIntroInfo intro = _introConfig.GetIntroByCharacter(characterId);
            string introId = intro?.IntroId ?? string.Empty;

            // 發布開始事件
            EventBus.Publish(new CGPlaybackStartedEvent
            {
                IntroId     = introId,
                CharacterId = characterId,
            });

            // 載入 CG Sprite（找不到則為 null，View 自行顯示 placeholder）
            Sprite cgSprite = null;
            if (intro != null && !string.IsNullOrEmpty(intro.CgSpriteId))
            {
                cgSprite = Resources.Load<Sprite>("CG/" + intro.CgSpriteId);
            }

            // Instantiate View
            CharacterIntroCGView view = UnityEngine.Object.Instantiate(_cgViewPrefab, _uiContainer);
            _currentView = view;

            string capturedIntroId  = introId;
            string capturedCharId   = characterId;

            view.SetContent(
                cgSprite,
                intro?.Lines,
                _charsPerSecond,
                () => OnPlaybackComplete(view, capturedIntroId, capturedCharId, onComplete));

            view.Show();
        }

        private void OnPlaybackComplete(
            CharacterIntroCGView view,
            string introId,
            string characterId,
            Action onComplete)
        {
            // 銷毀 View
            if (view != null)
            {
                view.Hide();
                UnityEngine.Object.Destroy(view.gameObject);
            }
            _currentView = null;

            // 發布完成事件
            EventBus.Publish(new CGPlaybackCompletedEvent
            {
                IntroId     = introId,
                CharacterId = characterId,
            });

            onComplete?.Invoke();
        }
    }
}
