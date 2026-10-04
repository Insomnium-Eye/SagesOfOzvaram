namespace SagesOfOzvaram.Combat
{
    /// <summary>One entry in a unit's turn menu - see TurnMenuBuilder for how the actual list gets built fresh each time the menu opens.</summary>
    public class TurnMenuOption
    {
        /// <summary>Stable key Game1 dispatches on (e.g. "Attack", "Move") - never changes even when Label does (e.g. "Move" displays as "Stand Up" while Knocked Down, "End" as "Guard" once it's affordable).</summary>
        public string Id { get; set; }

        public string Label { get; set; }
    }
}
