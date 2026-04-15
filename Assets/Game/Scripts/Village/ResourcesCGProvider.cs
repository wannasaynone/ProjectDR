// ResourcesCGProvider -- IT 階段用的 CG 圖片載入器。
// 從 Resources/CG/ 資料夾載入 Texture2D，找不到時程式生成 placeholder 紋理。
// 正式版本應替換為 Addressables 載入。

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ProjectBSR.DialogueSystem;
using UnityEngine;

namespace ProjectDR.Village
{
    /// <summary>
    /// IT 階段用的 CG 圖片提供者。
    /// 優先從 Resources/CG/ 載入，找不到時生成純色 placeholder Texture2D。
    /// </summary>
    public class ResourcesCGProvider : ICGProvider
    {
        private const int PlaceholderWidth = 1920;
        private const int PlaceholderHeight = 1080;

        private readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();

        public UniTask<Texture2D> LoadCGAsync(string cgName)
        {
            if (_cache.TryGetValue(cgName, out Texture2D cached))
            {
                return UniTask.FromResult(cached);
            }

            // 嘗試從 Resources/CG/ 載入
            Texture2D texture = Resources.Load<Texture2D>("CG/" + cgName);

            if (texture == null)
            {
                // 生成 placeholder 紋理
                texture = CreatePlaceholderTexture(cgName);
            }

            _cache[cgName] = texture;
            return UniTask.FromResult(texture);
        }

        public void ReleaseCG(string cgName)
        {
            if (_cache.TryGetValue(cgName, out Texture2D texture))
            {
                _cache.Remove(cgName);
                // 不銷毀 Resources 載入的紋理（由 Unity 管理），
                // 但 placeholder 是程式生成的，可以銷毀
                if (texture != null && texture.name.StartsWith("Placeholder_"))
                {
                    Object.Destroy(texture);
                }
            }
        }

        public void ReleaseAll()
        {
            foreach (KeyValuePair<string, Texture2D> kvp in _cache)
            {
                if (kvp.Value != null && kvp.Value.name.StartsWith("Placeholder_"))
                {
                    Object.Destroy(kvp.Value);
                }
            }
            _cache.Clear();
        }

        /// <summary>
        /// 生成 placeholder 紋理（深紫色底 + 白色文字標記）。
        /// </summary>
        private static Texture2D CreatePlaceholderTexture(string cgName)
        {
            Texture2D texture = new Texture2D(PlaceholderWidth, PlaceholderHeight, TextureFormat.RGBA32, false);
            texture.name = "Placeholder_" + cgName;

            // 填充深紫色底
            Color fillColor = new Color(0.2f, 0.1f, 0.3f, 1f);
            Color[] pixels = new Color[PlaceholderWidth * PlaceholderHeight];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = fillColor;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }
    }
}
