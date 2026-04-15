namespace ProjectDR.Village.Exploration
{
    /// <summary>
    /// Provides the player's movement speed in world units per second.
    /// Used by PlayerFreeMovement for continuous movement.
    /// </summary>
    public interface IMoveSpeedProvider
    {
        /// <summary>Returns the movement speed in units/second.</summary>
        float GetMoveSpeed();
    }
}
