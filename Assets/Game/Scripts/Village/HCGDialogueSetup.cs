// HCGDialogueSetup -- HCG 劇情播放整合層。
// 建立 KGC DialogueSystem 的 GameStaticDataManager、DialogueManager、DialogueView，
// 並注入 IT 階段硬編碼的 HCG 對話資料。
// 提供 PlayCGScene(dialogueId, onComplete) API。

using System;
using System.Text;
using KahaGameCore.GameData;
using KahaGameCore.GameData.Implemented;
using ProjectBSR.DialogueSystem;
using UnityEngine;

// 使用 alias 避免 DialogueData 名稱衝突
using KGCDialogueData = ProjectBSR.DialogueSystem.DialogueData;
using KGCDialogueManager = ProjectBSR.DialogueSystem.DialogueManager;
using KGCDialogueView = ProjectBSR.DialogueSystem.View.DialogueView;

namespace ProjectDR.Village
{
    /// <summary>
    /// HCG 劇情播放整合層。
    /// 負責建立 KGC DialogueSystem 所需的元件，
    /// 並提供簡潔的 PlayCGScene API 供 CGGalleryView 呼叫。
    /// </summary>
    public class HCGDialogueSetup
    {
        private readonly GameStaticDataManager _staticDataManager;
        private readonly ICGProvider _cgProvider;
        private KGCDialogueManager _kgcDialogueManager;
        private KGCDialogueView _dialogueView;
        private GameObject _dialogueViewInstance;

        /// <summary>
        /// 建構 HCGDialogueSetup。
        /// </summary>
        /// <param name="dialogueViewPrefab">KGC DialogueView Prefab。</param>
        /// <param name="parentCanvas">DialogueView 要掛載的 Canvas Transform。</param>
        /// <param name="cgProvider">CG 圖片提供者（可為 null，使用 ResourcesCGProvider）。</param>
        public HCGDialogueSetup(KGCDialogueView dialogueViewPrefab, Transform parentCanvas, ICGProvider cgProvider = null)
        {
            if (dialogueViewPrefab == null)
            {
                throw new ArgumentNullException(nameof(dialogueViewPrefab));
            }

            _cgProvider = cgProvider ?? new ResourcesCGProvider();

            // 建立 GameStaticDataManager 並注入 HCG 對話資料
            _staticDataManager = new GameStaticDataManager();
            InjectHCGDialogueData();

            // Instantiate DialogueView
            _dialogueViewInstance = UnityEngine.Object.Instantiate(dialogueViewPrefab.gameObject, parentCanvas);
            _dialogueView = _dialogueViewInstance.GetComponent<KGCDialogueView>();
            _dialogueViewInstance.SetActive(false);

            // 建立 KGC DialogueManager（commandFactoryContainer 傳 null 使用預設）
            _kgcDialogueManager = new KGCDialogueManager(
                _dialogueView,
                _staticDataManager,
                null,        // commandFactoryContainer: null -> 使用預設內建指令
                _cgProvider,
                null         // audioProvider: null -> 使用預設
            );
        }

        /// <summary>
        /// 播放指定對話 ID 的 CG 場景。
        /// </summary>
        /// <param name="dialogueId">KGC DialogueSystem 的對話 ID。</param>
        /// <param name="onComplete">播放完成時的回呼。</param>
        public void PlayCGScene(int dialogueId, Action onComplete)
        {
            _dialogueViewInstance.SetActive(true);
            _kgcDialogueManager.StartDialogue(dialogueId, () =>
            {
                _dialogueViewInstance.SetActive(false);
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// 銷毀 DialogueView 實例並釋放 CG 資源。
        /// </summary>
        public void Dispose()
        {
            if (_cgProvider is ResourcesCGProvider resourcesProvider)
            {
                resourcesProvider.ReleaseAll();
            }

            if (_dialogueViewInstance != null)
            {
                UnityEngine.Object.Destroy(_dialogueViewInstance);
                _dialogueViewInstance = null;
            }
        }

        /// <summary>
        /// IT 階段：硬編碼 HCG 對話資料。
        /// TODO: 正式版本應從外部資料源（Google Sheets -> JSON）載入。
        /// 每角色 1 個 CG 場景，包含漸黑、CG 顯示、對話、CG 隱藏等指令。
        /// 使用 GameStaticDataDeserializer（JsonFx）反序列化，因為 KGCDialogueData 的屬性為 private set。
        /// </summary>
        private void InjectHCGDialogueData()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");

            // ID=1001 村長夫人場景
            AppendLine(sb, 1001, 1, "BlackIn", "0.5");
            AppendLine(sb, 1001, 2, "ShowFullScreenImage", "cg_village_chief_wife_1", "1.0");
            AppendLine(sb, 1001, 3, "BlackOut", "0.5");
            AppendSay(sb, 1001, 4, "村長夫人", "...你真的很溫柔呢。");
            AppendSay(sb, 1001, 5, "村長夫人", "謝謝你一直陪在我身邊。");
            AppendLine(sb, 1001, 6, "HideFullScreenImage", "cg_village_chief_wife_1", "1.0");

            // ID=1002 守衛場景
            AppendLine(sb, 1002, 1, "BlackIn", "0.5");
            AppendLine(sb, 1002, 2, "ShowFullScreenImage", "cg_guard_1", "1.0");
            AppendLine(sb, 1002, 3, "BlackOut", "0.5");
            AppendSay(sb, 1002, 4, "守衛", "今晚的月色真美。");
            AppendSay(sb, 1002, 5, "守衛", "能和你一起看到...我很開心。");
            AppendLine(sb, 1002, 6, "HideFullScreenImage", "cg_guard_1", "1.0");

            // ID=1003 魔女場景
            AppendLine(sb, 1003, 1, "BlackIn", "0.5");
            AppendLine(sb, 1003, 2, "ShowFullScreenImage", "cg_witch_1", "1.0");
            AppendLine(sb, 1003, 3, "BlackOut", "0.5");
            AppendSay(sb, 1003, 4, "魔女", "別碰那個燒杯...算了，你碰吧。");
            AppendSay(sb, 1003, 5, "魔女", "反正你在的時候...實驗總是比較順利。");
            AppendLine(sb, 1003, 6, "HideFullScreenImage", "cg_witch_1", "1.0");

            // ID=1004 農女場景
            AppendLine(sb, 1004, 1, "BlackIn", "0.5");
            AppendLine(sb, 1004, 2, "ShowFullScreenImage", "cg_farmgirl_1", "1.0");
            AppendLine(sb, 1004, 3, "BlackOut", "0.5");
            AppendSay(sb, 1004, 4, "農女", "嘿嘿，今天的收成特別好喔！");
            AppendSay(sb, 1004, 5, "農女", "都是因為你幫我的忙啦...謝謝你。");
            // 最後一行不加逗號
            AppendLine(sb, 1004, 6, "HideFullScreenImage", "cg_farmgirl_1", "1.0", true);

            sb.Append("]");

            GameStaticDataDeserializer deserializer = new GameStaticDataDeserializer();
            KGCDialogueData[] dialogueDataArray = deserializer.Read<KGCDialogueData[]>(sb.ToString());
            _staticDataManager.Add<KGCDialogueData>(dialogueDataArray as IGameData[]);
        }

        private static void AppendSay(StringBuilder sb, int id, int line, string speaker, string text, bool isLast = false)
        {
            sb.AppendFormat(
                "{{\"ID\":{0},\"Line\":{1},\"Command\":\"Say\",\"Arg1\":\"{2}\",\"Arg2\":\"{3}\",\"Arg3\":\"\",\"Arg4\":\"\",\"Arg5\":\"\"}}",
                id, line, EscapeJson(speaker), EscapeJson(text));
            if (!isLast) sb.Append(",");
        }

        private static void AppendLine(StringBuilder sb, int id, int line, string command, string arg1, string arg2 = "", bool isLast = false)
        {
            sb.AppendFormat(
                "{{\"ID\":{0},\"Line\":{1},\"Command\":\"{2}\",\"Arg1\":\"{3}\",\"Arg2\":\"{4}\",\"Arg3\":\"\",\"Arg4\":\"\",\"Arg5\":\"\"}}",
                id, line, command, EscapeJson(arg1), EscapeJson(arg2));
            if (!isLast) sb.Append(",");
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
