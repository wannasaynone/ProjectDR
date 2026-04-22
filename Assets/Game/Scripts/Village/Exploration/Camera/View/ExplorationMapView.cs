using ProjectDR.Village.Exploration.Map;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Camera
{
    /// <summary>
    /// Spawns and owns all <see cref="GridCellView"/> instances for the exploration map.
    /// Blocked cells are skipped — only walkable cells receive a visual representation.
    /// </summary>
    public class ExplorationMapView : MonoBehaviour
    {
        private const float CellSize = 1.0f;

        // Slightly less than 1 to leave a thin gap between cells.
        private const float CellScale = 0.95f;

        private Dictionary<Vector2Int, GridCellView> _cellViews;
        private Sprite _whiteSprite;

        /// <summary>
        /// Builds the visual grid from <paramref name="mapData"/>, binding each cell to
        /// <paramref name="gridMap"/> for state queries.
        /// </summary>
        public void Initialize(GridMap gridMap, MapData mapData)
        {
            _cellViews = new Dictionary<Vector2Int, GridCellView>();
            _whiteSprite = CreateWhiteSprite();

            for (int y = 0; y < mapData.Height; y++)
            {
                for (int x = 0; x < mapData.Width; x++)
                {
                    if (mapData.GetCellType(x, y) == CellType.Blocked)
                        continue;

                    CreateCellView(x, y, gridMap);
                }
            }
        }

        /// <summary>
        /// Converts a grid coordinate to a world-space position centred on that cell.
        /// Y is inverted so that row 0 is at the top (positive Y) and increases downward.
        /// </summary>
        public Vector3 GridToWorldPosition(int gridX, int gridY)
        {
            return transform.position + new Vector3(gridX * CellSize, -gridY * CellSize, 0f);
        }

        private void CreateCellView(int x, int y, GridMap gridMap)
        {
            GameObject cellObj = new GameObject($"Cell_{x}_{y}");
            cellObj.transform.SetParent(transform);
            cellObj.transform.localPosition = new Vector3(x * CellSize, -y * CellSize, 0f);
            cellObj.transform.localScale = new Vector3(CellScale, CellScale, 1f);

            SpriteRenderer sr = cellObj.AddComponent<SpriteRenderer>();
            sr.sprite = _whiteSprite;
            sr.sortingOrder = 0;

            // World-space TMP overlay for the adjacent-monster count number.
            GameObject textObj = new GameObject("Number");
            textObj.transform.SetParent(cellObj.transform);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            textObj.transform.localScale = Vector3.one;

            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.fontSize = 4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = 1;

            // RectTransform must match cell size so the text is centred correctly.
            RectTransform rt = tmp.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1f, 1f);
            textObj.SetActive(false);

            GridCellView cellView = cellObj.AddComponent<GridCellView>();
            cellView.Initialize(x, y, gridMap, sr, tmp);
            cellView.UpdateVisual();

            _cellViews[new Vector2Int(x, y)] = cellView;
        }

        // Creates a plain white 4x4 texture sprite used as the cell background.
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
