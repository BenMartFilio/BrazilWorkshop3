using Barrage.UI;
using UnityEngine;

/// <summary>
/// Pont entre la scène MapRoad et le SessionManager.
/// Place ce composant sur un GameObject de la scène MapRoad.
/// Il restaure l'état au démarrage et expose la méthode pour partir au Barrage.
///
/// Le démarrage effectif du jeu (TimeManager, sol, spawner, joueur) est différé
/// via <see cref="DémarrerJeu"/> afin de laisser <c>AffichagePremierBarrageUI</c>
/// afficher la demande du premier barrage avant que l'action commence.
/// </summary>
public class MapRoadSessionBridge : MonoBehaviour
{
    [Header("Références scène MapRoad")]
    [SerializeField] private PlayerMovement          joueur;
    [SerializeField] private ScoreManager            scoreManager;
    [SerializeField] private SpawnObstacleV2         spawner;
    [SerializeField] private GoundMouvement[]        sols;
    [SerializeField] private EndManager              end;
    [SerializeField] private BarreProgressionBarrage barreProgression;

    [Header("Systèmes à geler pendant l'intro")]
    [Tooltip("TimeManager à démarrer après l'intro.")]
    [SerializeField] private TimeManager timeManager;

    [Header("Affichage intro")]
    [Tooltip("Si assigné, délègue le démarrage du jeu à AffichagePremierBarrageUI. " +
             "Laissez vide pour démarrer immédiatement (compatibilité ascendante).")]
    [SerializeField] private AffichagePremierBarrageUI affichageIntro;

    private void Start()
    {
        if (SessionManager.Instance != null)
            SessionManager.Instance.RestaurerMapRoad(joueur, scoreManager, spawner, end, sols);

        if (barreProgression != null)
            barreProgression.RéinitialiserPourNouveauCycle();

        // Geler les systèmes de jeu : le sol, le spawner, le joueur et le timer
        // resteront bloqués jusqu'à ce que DémarrerJeu() soit appelé.
        GelerJeu();

        if (affichageIntro != null)
        {
            // L'AffichagePremierBarrageUI appellera DémarrerJeu() au bout de 3 s.
            affichageIntro.Lancer(this);
        }
        else
        {
            // Aucune intro configurée — on démarre directement.
            DémarrerJeu();
        }
    }

    /// <summary>
    /// Libère tous les systèmes de jeu gelés par <see cref="GelerJeu"/>.
    /// Appelé soit directement (pas d'intro), soit par <see cref="AffichagePremierBarrageUI"/>.
    /// </summary>
    public void DémarrerJeu()
    {
        if (timeManager != null)
            timeManager.StartTime();

        if (sols != null)
            foreach (var sol in sols)
                sol.StartMove();

        joueur?.StartMove();
        spawner?.StartSpawning();
    }

    // ── Privé ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Bloque tous les systèmes de jeu afin que la scène soit figée
    /// pendant l'affichage de l'intro.
    /// </summary>
    private void GelerJeu()
    {
        // Arrêter le TimeManager avant qu'il ne démarre de lui-même dans Start().
        // On ne l'arrête pas explicitement (il vient de démarrer dans TimeManager.Start()),
        // donc on le stoppe immédiatement.
        if (timeManager != null)
            timeManager.StopTime();

        if (sols != null)
            foreach (var sol in sols)
                sol.StopMove();

        joueur?.StopMove();
        spawner?.StopSpawning();
    }

    /// <summary>
    /// Sauvegarde l'état et charge la scène Barrage.
    /// À appeler depuis la logique de déclenchement du barrage.
    /// </summary>
    public void DéclencherBarrage()
    {
        if (SessionManager.Instance == null)
        {
            Debug.LogError("[MapRoadSessionBridge] SessionManager introuvable dans la scène.");
            return;
        }

        SessionManager.Instance.AllerAuBarrage(joueur, scoreManager, spawner, end, sols);
    }
}
