namespace CardGame.Core
{
    /// <summary>
    /// Identifiants des cartes (Magicien + Guerrier) selon carte_spec_complete.md.
    /// </summary>
    public enum CardId
    {
        // --- Magicien (obligatoires Equipé) ---
        CatalyseurArcanaiqueRestraint,
        RuneEnergieArcanique,
        RuneEssenceArcanique,
        RuneForceArcanique,
        // --- Magicien (autres) ---
        ExplosionMagieEphemere,
        Divination,
        ArmurePsychique,
        BouleDeFeu,
        AttaquePlus,
        DefensePlus,
        Concentration,
        LienKarmique,
        Defense,
        Galvanisation,
        Evaluation,
        Attaque,
        OrageDePoche,
        GlaceLocalisee,

        // --- Guerrier (obligatoires Equipé) ---
        HacheOublie,
        RuneEnduranceOublie,
        RuneProtectionOublie,
        RuneAgressiviteOublie,
        // --- Guerrier (autres) ---
        Repositionnement,
        ContreAttaque,
        Parade,
        PositionOffensive,
        AppuisSolide,
        DefenseLourde,
        PositionDefensive,
        SouffleEternel,
        DisciplineEternel,
        Guillotine,
        FendoireMortel,
        AttaqueLourde,
        AttaqueLegere,
        AttaqueTactique
    }
}
