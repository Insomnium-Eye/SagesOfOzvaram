namespace SagesOfOzvaram.Units
{
    /// <summary>
    /// How a summoned creature acts on its own turn - only meaningful for a unit with an Owner
    /// (see BaseUnit.Owner/Behavior); ignored for a hero. Only ControllableBySummoner is actually
    /// driven by anything right now (Game1's per-turn player-control handoff, mirroring Test
    /// Mode's own "control whoever's turn it is" pattern) - every summon defined today defaults to
    /// it. The other 5 are written down for the AI/relationship system they'll eventually need
    /// (aggression-based targeting, healer behavior, a second controller, Demons overriding this
    /// entirely with their own Bargain/Dismiss menu - see the GDD's Summoned Unit Behaviors and
    /// Demons & The Bargain sections) - a summon can be given one of these values today, but
    /// nothing in Game1 acts on anything but ControllableBySummoner yet.
    /// </summary>
    public enum SummonBehavior
    {
        /// <summary>The summoner gets a normal turn with it, same as any of their own units - Game1 hands over _playerUnit to it for the duration (see the per-turn control handoff in Update).</summary>
        ControllableBySummoner,

        /// <summary>Not controllable - always hostile toward its own summoner specifically, regardless of anyone else on the field. Typical of Demons that turn on their summoner (see Demons & The Bargain).</summary>
        HostileTowardsSummoner,

        /// <summary>Not controllable - neutral by default; who it actually attacks (if anyone) is decided by the Aggro System's 0-20 per-unit aggro values (see Aggro System), same forced-targeting rule any other aggro-driven unit would follow.</summary>
        NeutralAggressionBased,

        /// <summary>Not controllable - always hostile toward the summoner's enemies (never the summoner or its own team), with target choice among those enemies still decided by the Aggro System.</summary>
        HostileTowardsEnemies,

        /// <summary>Not controllable - spends its turns healing allies rather than attacking at all.</summary>
        HealsAllies,

        /// <summary>Not controllable by its own summoner - instead controllable by some OTHER unit (its actual "handler"), which may itself be friendly, hostile, or neutral toward the original summoner.</summary>
        ControllableByOtherUnit,
    }
}
