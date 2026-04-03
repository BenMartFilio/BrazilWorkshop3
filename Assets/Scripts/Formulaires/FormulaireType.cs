namespace Barrage.Formulaires
{
    /// <summary>
    /// Enumère tous les types de cartes manipulables dans le système de barrage.
    /// Les quatre premiers sont des formulaires standards validés par le garde.
    /// Les trois derniers sont des objets spéciaux : draggables comme des formulaires
    /// mais déclenchent un effet propre quand remis au garde au lieu de valider la séquence.
    /// </summary>
    public enum FormulaireType
    {
        // ── Formulaires standards ──────────────────────────────────────────────
        ReclassificationDesIndividus,
        CentraleDuRavitaillement,
        SecuritéDesFrontièresIntérieures,
        ConformitéSociale,

        // ── Objets spéciaux (draggables, effets propres) ───────────────────────
        LiasseDeBillets,
        FormulairePasePartout,
        BadgeDuGouvernement,
    }
}
