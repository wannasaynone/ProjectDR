using System;

namespace ProjectDR.Village.Exploration.Combat
{
    /// <summary>
    /// Static damage calculation utility.
    /// GDD rule 48: DMG = ATK - DEF, minimum 1.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// Calculates damage dealt. DMG = attackerAtk - defenderDef, minimum 1.
        /// </summary>
        public static int Calculate(int attackerAtk, int defenderDef)
        {
            int raw = attackerAtk - defenderDef;
            return Math.Max(1, raw);
        }
    }
}
