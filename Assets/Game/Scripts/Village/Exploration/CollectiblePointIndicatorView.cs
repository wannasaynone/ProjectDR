// CollectiblePointIndicatorView — 採集點地圖標記 View。
// 在已探索且有採集點的格子上，將格子顏色疊加為淡藍色以區別普通格子。
// 訂閱 CellRevealedEvent 與 PlayerMoveCompletedEvent，隨狀態即時更新。

using System;
using System.Collections.Generic;
using KahaGameCore.GameEvent;
using UnityEngine;

namespace ProjectDR.Village.Exploration
{
    /// <summary>
    /// 為地圖上所有有採集點的格子提供視覺標記。
    /// 已探索且有採集點的格子會在 GridCellView 上疊加一個小藍色方塊，
    /// 讓玩家一眼辨識可採集的位置。
    /// 採集期間標記持續顯示，讓玩家看到周圍其他採集點。
    /// </summary>
    public class CollectiblePointIndicatorView : MonoBehaviour
    {
        private GridMap _gridMap;
        private ExplorationMapView _mapView;
        private MapData _mapData;

        // 每個採集點格子建立一個小方塊 GameObject
        private readonly List<GameObject> _indicators = new List<GameObject>();

        private Action<CellRevealedEvent> _onCellRevealed;

        private static readonly Color CollectibleTint = new Color(0.4f, 0.7f, 1f, 0.85f);

        /// <summary>
        /// 初始化指示器 View，掃描地圖中所有採集點並為已探索格子建立標記。
        /// </summary>
        public void Initialize(GridMap gridMap, ExplorationMapView mapView, MapData mapData)
        {
            _gridMap = gridMap;
            _mapView = mapView;
            _mapData = mapData;

            // 為已探索的採集點建立初始標記
            for (int y = 0; y < mapData.Height; y++)
            {
                for (int x = 0; x < mapData.Width; x++)
                {
                    if (gridMap.HasCollectiblePoint(x, y) && gridMap.IsExplored(x, y))
                        CreateIndicator(x, y);
                }
            }

            // 訂閱格子揭開事件，揭開時如有採集點立即顯示標記
            _onCellRevealed = (e) =>
            {
                if (_gridMap.HasCollectiblePoint(e.X, e.Y))
                    CreateIndicator(e.X, e.Y);
            };
            EventBus.Subscribe<CellRevealedEvent>(_onCellRevealed);
        }

        private void CreateIndicator(int x, int y)
        {
            Vector3 worldPos = _mapView.GridToWorldPosition(x, y);

            GameObject obj = new GameObject($"CollectibleIndicator_{x}_{y}");
            obj.transform.SetParent(transform);
            obj.transform.position = worldPos + new Vector3(0f, 0f, -0.05f);
            obj.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateWhiteSprite();
            sr.color = CollectibleTint;
            sr.sortingOrder = 2;

            _indicators.Add(obj);
        }

        private void OnDestroy()
        {
            if (_onCellRevealed != null)
                EventBus.Unsubscribe<CellRevealedEvent>(_onCellRevealed);
        }

        private static Sprite CreateWhiteSprite()
        {
            Texture2D tex = new Texture2D(4, 4);
            Color[] pixels = new Color[16];
            for (int i = 0; i < 16; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
    }
}
