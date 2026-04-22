using System.Collections.Generic;
using UnityEngine;

namespace ProjectDR.Village.Exploration.Combat
{
    /// <summary>Provides the current positions of all active monsters on the map.</summary>
    public interface IMonsterPositionProvider
    {
        IReadOnlyList<Vector2Int> GetMonsterPositions();
    }
}
