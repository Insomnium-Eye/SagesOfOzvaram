namespace SagesOfOzvaram.Units
{
    /// <summary>
    /// Playable/NPC races, per the GDD's race roster (§2).
    /// </summary>
    public enum Race
    {
        Human,
        Lethios,
        Vectium,

        // Summoned-creature races, not playable hero races - see RaceAttacks for each one's
        // innate move. One Race per summon (not a shared species pool) since RaceAttacks only
        // supports one move per Race and each summon needs its own distinct attack - "Race"
        // here functions as "creature type identifier," same as Roachlin already did before
        // these were added.
        Roachlin,
        DuskRoachlinPriest,
        AetherfluffBeetle,
        Bearat,
        MirebackSlogger,
        LanternmothCinderwing,
        Brambleboar,
        SiltfinMawpike,
        GloamravenOssuary,
        RootmossStonegloom
    }
}
