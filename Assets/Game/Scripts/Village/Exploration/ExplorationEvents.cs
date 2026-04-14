using KahaGameCore.GameEvent;
using UnityEngine;

namespace ProjectDR.Village.Exploration
{
    /// <summary>Published when a cell is revealed for the first time.</summary>
    public class CellRevealedEvent : GameEventBase
    {
        /// <summary>X coordinate of the revealed cell.</summary>
        public int X;
        /// <summary>Y coordinate of the revealed cell.</summary>
        public int Y;
    }

    /// <summary>Published when the monster counts for all cells need to be recalculated.</summary>
    public class MonsterCountsChangedEvent : GameEventBase { }

    /// <summary>Published when the player begins moving from one cell to another.</summary>
    public class PlayerMoveStartedEvent : GameEventBase
    {
        /// <summary>The cell position the player is moving from.</summary>
        public Vector2Int From;
        /// <summary>The cell position the player is moving to.</summary>
        public Vector2Int To;
        /// <summary>The duration (in seconds) the move animation should take.</summary>
        public float MoveDuration;
    }

    /// <summary>Published when the player's move animation completes and the position is confirmed.</summary>
    public class PlayerMoveCompletedEvent : GameEventBase
    {
        /// <summary>The cell position the player has arrived at.</summary>
        public Vector2Int Position;
    }

    /// <summary>Published when the exploration map has finished initializing.</summary>
    public class ExplorationMapInitializedEvent : GameEventBase
    {
        /// <summary>Width of the map in cells.</summary>
        public int Width;
        /// <summary>Height of the map in cells.</summary>
        public int Height;
        /// <summary>The spawn position of the player on this map.</summary>
        public Vector2Int SpawnPosition;
    }

    /// <summary>Published when the evacuation countdown begins (player stepped on a trigger point).</summary>
    public class EvacuationStartedEvent : GameEventBase
    {
        /// <summary>Total countdown duration in seconds.</summary>
        public float Duration;
    }

    /// <summary>Published when the evacuation countdown is cancelled (player moved away).</summary>
    public class EvacuationCancelledEvent : GameEventBase { }

    /// <summary>Published when the evacuation countdown completes and exploration ends.</summary>
    public class ExplorationCompletedEvent : GameEventBase { }

    // ----- Collection system events (GDD rules 8-13, 44-46) -----

    /// <summary>Published when the player starts gathering at a collectible point.</summary>
    public class CollectionStartedEvent : GameEventBase
    {
        /// <summary>X coordinate of the collectible point.</summary>
        public int X;
        /// <summary>Y coordinate of the collectible point.</summary>
        public int Y;
        /// <summary>Total gathering duration in seconds (first timer).</summary>
        public float GatherDuration;
    }

    /// <summary>Published when the player cancels gathering (accumulated time resets).</summary>
    public class CollectionCancelledEvent : GameEventBase
    {
        /// <summary>X coordinate of the collectible point.</summary>
        public int X;
        /// <summary>Y coordinate of the collectible point.</summary>
        public int Y;
    }

    /// <summary>Published when the first-layer timer completes and the item panel opens.</summary>
    public class GatheringCompletedEvent : GameEventBase
    {
        /// <summary>X coordinate of the collectible point.</summary>
        public int X;
        /// <summary>Y coordinate of the collectible point.</summary>
        public int Y;
    }

    /// <summary>Published when a single item slot finishes unlocking (second-layer timer).</summary>
    public class ItemSlotUnlockedEvent : GameEventBase
    {
        /// <summary>X coordinate of the collectible point.</summary>
        public int X;
        /// <summary>Y coordinate of the collectible point.</summary>
        public int Y;
        /// <summary>Index of the unlocked item slot.</summary>
        public int SlotIndex;
        /// <summary>Item ID of the unlocked item.</summary>
        public string ItemId;
    }

    /// <summary>Published when the player picks up an unlocked item into the backpack.</summary>
    public class ItemPickedUpEvent : GameEventBase
    {
        /// <summary>Item ID that was picked up.</summary>
        public string ItemId;
        /// <summary>Quantity that was added to the backpack.</summary>
        public int Quantity;
    }

    /// <summary>Published when the player closes the collection item panel.</summary>
    public class CollectionPanelClosedEvent : GameEventBase { }
}
