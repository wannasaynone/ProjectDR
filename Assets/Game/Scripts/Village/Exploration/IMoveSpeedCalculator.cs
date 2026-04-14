namespace ProjectDR.Village.Exploration
{
    /// <summary>Calculates the duration for a single player cell movement.</summary>
    public interface IMoveSpeedCalculator
    {
        float CalculateMoveDuration();
    }
}
