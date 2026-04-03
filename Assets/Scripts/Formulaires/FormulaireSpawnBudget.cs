using System;
using System.Collections.Generic;
using Barrage.Formulaires;
using UnityEngine;

/// <summary>
/// ScriptableObject partagé entre la scène Barrage et la scène MapRoad.
///
/// Rôle :
///   1. À la fin d'un barrage, <see cref="DefinirBudget"/> calcule quels types de véhicules
///      doivent apparaître sur la prochaine MapRoad et combien de patterns sont nécessaires
///      pour que le joueur puisse collecter tous les formulaires demandés.
///   2. Sur MapRoad, <see cref="SpawnObstacleV2"/> consomme le budget via <see cref="ConsumeNext"/>
///      pour forcer le bon skin sur les véhicules Good spawner.
///
/// Règle de budget :
///   - 2 véhicules par type demandé au prochain barrage.
///   - 1 véhicule par type NON demandé (pour maintenir la diversité et ne pas révéler la demande).
/// </summary>
[CreateAssetMenu(fileName = "FormulaireSpawnBudget", menuName = "Barrage/Formulaire Spawn Budget")]
public class FormulaireSpawnBudget : ScriptableObject
{
    // ── Données calculées (runtime, jamais persistées sur disque) ─────────────

    /// <summary>File d'attente des types à forcer sur les prochains véhicules Good spawned.</summary>
    private readonly Queue<FormulaireType> _file = new();

    /// <summary>
    /// Nombre de patterns normaux nécessaires pour que tous les véhicules budgetés aient été spawned.
    /// Calculé dans <see cref="DefinirBudget"/> et sauvegardé dans <see cref="DonnéesSession"/>.
    /// </summary>
    public int PatternsNécessaires { get; private set; }

    /// <summary>Vrai quand tous les véhicules budgetés ont été consommés par le spawner.</summary>
    public bool BudgetÉpuisé => _file.Count == 0;

    /// <summary>Nombre de véhicules encore en attente dans le budget.</summary>
    public int VéhiculesRestants => _file.Count;

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Calcule le budget de véhicules et le nombre de patterns nécessaires
    /// à partir de la prochaine demande de barrage.
    ///
    /// À appeler dans la scène Barrage, juste après <see cref="DemandeBarrage.Régénérer"/>,
    /// avant de retourner sur MapRoad.
    /// </summary>
    /// <param name="demande">Types de formulaires demandés au prochain barrage.</param>
    /// <param name="patterns">Patterns normaux utilisés sur MapRoad (pour calculer la moyenne de véhicules Good).</param>
    public void DefinirBudget(IReadOnlyList<FormulaireType> demande, ObstaclePattern[] patterns)
    {
        _file.Clear();

        if (demande == null || demande.Count == 0)
        {
            PatternsNécessaires = 0;
            Debug.LogWarning("[FormulaireSpawnBudget] DefinirBudget appelé avec une demande vide — budget nul.");
            return;
        }

        // ── Compter combien de fois chaque type est demandé ──────────────────
        var comptesDemandés = new Dictionary<FormulaireType, int>();
        foreach (var type in demande)
        {
            comptesDemandés.TryGetValue(type, out int c);
            comptesDemandés[type] = c + 1;
        }

        var tousLesTypes = (FormulaireType[])Enum.GetValues(typeof(FormulaireType));
        var budgetListe  = new List<FormulaireType>();

        // 2 véhicules par type demandé (quel que soit le nombre d'exemplaires demandés)
        foreach (var kv in comptesDemandés)
            for (int i = 0; i < 2; i++)
                budgetListe.Add(kv.Key);

        // 1 véhicule par type NON demandé
        foreach (var type in tousLesTypes)
            if (!comptesDemandés.ContainsKey(type))
                budgetListe.Add(type);

        // Mélange Fisher-Yates pour éviter que le joueur anticipe l'ordre
        for (int i = budgetListe.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (budgetListe[i], budgetListe[j]) = (budgetListe[j], budgetListe[i]);
        }

        foreach (var t in budgetListe)
            _file.Enqueue(t);

        // ── Calculer le nombre de patterns nécessaires ────────────────────────
        float moyenne = CalculerMoyenneVoituresParPattern(patterns);
        PatternsNécessaires = moyenne > 0f
            ? Mathf.CeilToInt(budgetListe.Count / moyenne)
            : Mathf.Max(5, budgetListe.Count); // fallback conservateur

        Debug.Log($"[FormulaireSpawnBudget] Budget défini — " +
                  $"{budgetListe.Count} véhicule(s) : {string.Join(", ", budgetListe)} | " +
                  $"moyenne Good/pattern={moyenne:F1} | " +
                  $"patterns nécessaires={PatternsNécessaires}");
    }

    /// <summary>
    /// Retourne le prochain type de formulaire à forcer sur un véhicule Good,
    /// ou null si le budget est épuisé (le skin sera alors aléatoire).
    /// </summary>
    public FormulaireType? ConsumeNext()
    {
        if (_file.Count == 0) return null;
        return _file.Dequeue();
    }

    /// <summary>Remet le budget à zéro (nouvelle partie).</summary>
    public void Réinitialiser()
    {
        _file.Clear();
        PatternsNécessaires = 0;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private float CalculerMoyenneVoituresParPattern(ObstaclePattern[] patterns)
    {
        if (patterns == null || patterns.Length == 0) return 1f;

        float total = 0f;
        int   count = 0;

        foreach (var p in patterns)
        {
            if (p == null) continue;
            total += p.bonusVehiculeCount;
            count++;
        }

        return count > 0 ? total / count : 1f;
    }
}
