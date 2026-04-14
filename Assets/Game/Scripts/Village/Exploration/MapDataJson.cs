using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectDR.Village.Exploration
{
    // NOTE: MapDataJson and related DTOs are intentionally NOT using Google Sheets / IGameData.
    // Exploration maps are procedural / hand-authored per-map files that ship as JSON TextAssets
    // in Resources/Maps/. They are not balance-tuned numerical data and do not require the
    // GoogleSheet2JsonSetting pipeline. Exception recorded here per development-workflow.md §1.5.

    [Serializable]
    public class MapDataJson
    {
        public int width;
        public int height;
        public int[] cells;
        public PositionJson spawnPosition;
        public EvacuationGroupJson[] evacuationGroups;
        public CollectiblePointJson[] collectiblePoints;
        public MonsterSpawnJson[] monsterSpawns;
    }

    [Serializable]
    public class MonsterSpawnJson
    {
        public PositionJson position;
        public string typeId;
    }

    [Serializable]
    public class PositionJson
    {
        public int x;
        public int y;
    }

    [Serializable]
    public class EvacuationGroupJson
    {
        public PositionJson[] points;
    }

    [Serializable]
    public class CollectiblePointJson
    {
        public PositionJson position;
        public float gatherDurationSeconds;
        public CollectibleItemJson[] items;
    }

    [Serializable]
    public class CollectibleItemJson
    {
        public string itemId;
        public int quantity;
        public float unlockDurationSeconds;
    }

    /// <summary>
    /// Deserializes a map JSON string into a <see cref="MapData"/> domain object.
    /// </summary>
    public static class MapDataLoader
    {
        /// <summary>
        /// Parses <paramref name="json"/> and returns a fully validated <see cref="MapData"/>.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="json"/> is null/empty or contains invalid cell values.
        /// </exception>
        public static MapData Load(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("json must not be null or empty.", nameof(json));

            MapDataJson dto = JsonUtility.FromJson<MapDataJson>(json);

            // Convert int[] cells to CellType[]
            CellType[] cells = new CellType[dto.cells.Length];
            for (int i = 0; i < dto.cells.Length; i++)
            {
                if (dto.cells[i] == 0)
                    cells[i] = CellType.Explorable;
                else if (dto.cells[i] == 1)
                    cells[i] = CellType.Blocked;
                else
                    throw new ArgumentException(
                        $"Invalid cell value {dto.cells[i]} at index {i}. Expected 0 or 1.");
            }

            // Convert spawnPosition
            Vector2Int spawnPos = new Vector2Int(dto.spawnPosition.x, dto.spawnPosition.y);

            // Convert evacuation groups
            List<List<Vector2Int>> groups = new List<List<Vector2Int>>();
            if (dto.evacuationGroups != null)
            {
                for (int g = 0; g < dto.evacuationGroups.Length; g++)
                {
                    List<Vector2Int> points = new List<Vector2Int>();
                    if (dto.evacuationGroups[g].points != null)
                    {
                        for (int p = 0; p < dto.evacuationGroups[g].points.Length; p++)
                        {
                            PositionJson pj = dto.evacuationGroups[g].points[p];
                            points.Add(new Vector2Int(pj.x, pj.y));
                        }
                    }
                    groups.Add(points);
                }
            }

            // Convert collectible points
            List<CollectiblePointData> collectiblePoints = new List<CollectiblePointData>();
            if (dto.collectiblePoints != null)
            {
                for (int cp = 0; cp < dto.collectiblePoints.Length; cp++)
                {
                    CollectiblePointJson cpJson = dto.collectiblePoints[cp];
                    List<CollectibleItemEntry> items = new List<CollectibleItemEntry>();
                    if (cpJson.items != null)
                    {
                        for (int it = 0; it < cpJson.items.Length; it++)
                        {
                            CollectibleItemJson itemJson = cpJson.items[it];
                            items.Add(new CollectibleItemEntry(
                                itemJson.itemId,
                                itemJson.quantity,
                                itemJson.unlockDurationSeconds));
                        }
                    }
                    collectiblePoints.Add(new CollectiblePointData(
                        cpJson.position.x,
                        cpJson.position.y,
                        cpJson.gatherDurationSeconds,
                        items));
                }
            }

            // Convert monster spawn points
            List<MonsterSpawnPoint> monsterSpawns = new List<MonsterSpawnPoint>();
            if (dto.monsterSpawns != null)
            {
                for (int ms = 0; ms < dto.monsterSpawns.Length; ms++)
                {
                    MonsterSpawnJson msJson = dto.monsterSpawns[ms];
                    monsterSpawns.Add(new MonsterSpawnPoint(
                        new Vector2Int(msJson.position.x, msJson.position.y),
                        msJson.typeId));
                }
            }

            return new MapData(dto.width, dto.height, cells, spawnPos, groups, collectiblePoints, monsterSpawns);
        }
    }
}
