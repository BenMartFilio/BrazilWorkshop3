using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton persistant entre les scènes (DontDestroyOnLoad).
/// Orchestre la sauvegarde de l'état MapRoad avant d'aller au Barrage,
/// et la restauration à son retour.
/// </summary>
public class SessionManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static SessionManager Instance { get; private set; }

    // ── Configuration ─────────────────────────────────────────────────────────

    [Tooltip("ScriptableObject partagé contenant les données de session.")]
    [SerializeField] private DonnéesSession donnees;

    [Tooltip("Nom exact de la scène MapRoad.")]
    [SerializeField] private string nomScèneMapRoad = "MapRoad";

    [Tooltip("Nom exact de la scène Barrage.")]
    [SerializeField] private string nomScèneBarrage = "Barrage";

    // ── Cycle de vie ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Sauvegarde l'état courant de la scène MapRoad puis charge la scène Barrage.
    /// À appeler depuis MapRoad quand le joueur atteint un barrage.
    /// </summary>
    public void AllerAuBarrage(
        PlayerMovement joueur,
        ScoreManager   scoreManager,
        SpawnObstacleV2 spawner,
        GoundMouvement[] sols,
        float vitesseSolAvantRalentissement = -1f)
    {
        SauvegarderMapRoad(joueur, scoreManager, spawner, sols, vitesseSolAvantRalentissement);
        SceneManager.LoadScene(nomScèneBarrage);
    }

    /// <summary>
    /// Charge la scène MapRoad. La restauration se fait automatiquement
    /// via <see cref="RestaurerMapRoad"/> appelé depuis MapRoad au démarrage.
    /// </summary>
    public void RetournerAMapRoad()
    {
        SceneManager.LoadScene(nomScèneMapRoad);
    }

    /// <summary>
    /// Restaure l'état MapRoad depuis les données sauvegardées.
    /// À appeler depuis MapRoad dans Start() (après que tous les composants sont prêts).
    /// Si <see cref="DonnéesSession.sessionValide"/> est false (nouvelle partie),
    /// aucune restauration n'est effectuée et les composants conservent leurs valeurs par défaut.
    /// </summary>
    public void RestaurerMapRoad(
        PlayerMovement   joueur,
        ScoreManager     scoreManager,
        SpawnObstacleV2  spawner,
        GoundMouvement[] sols)
    {
        if (donnees == null)
        {
            Debug.LogError("[SessionManager] DonnéesSession non assigné dans l'Inspector — restauration annulée.");
            return;
        }

        // Pas de session sauvegardée → nouvelle partie, rien à restaurer.
        if (!donnees.sessionValide)
            return;

        if (joueur == null)       { Debug.LogError("[SessionManager] joueur est null — restauration annulée.");       return; }
        if (scoreManager == null) { Debug.LogError("[SessionManager] scoreManager est null — restauration annulée."); return; }
        if (spawner == null)      { Debug.LogError("[SessionManager] spawner est null — restauration annulée.");      return; }

        joueur.RestaurerDepuisSession(donnees.indexLane, donnees.pièces);
        scoreManager.RestaurerDepuisSession(donnees.score, donnees.vitesseScore);
        spawner.RestaurerDepuisSession(donnees.vitesseGénérale);

        if (sols != null)
            foreach (var sol in sols)
                sol.RestaurerDepuisSession(donnees.vitesseSol);

        // Invalider la session immédiatement après restauration.
        // Tout rechargement ultérieur de MapRoad (nouvelle partie, mort, menu)
        // démarrera proprement sans restaurer cet état.
        donnees.sessionValide = false;
    }

    /// <summary>Remet la session à zéro (nouvelle partie).</summary>
    public void NouvellePartie()
    {
        donnees.Reinitialiser();
    }

    // ── Sauvegarde interne ────────────────────────────────────────────────────

    private void SauvegarderMapRoad(
        PlayerMovement   joueur,
        ScoreManager     scoreManager,
        SpawnObstacleV2  spawner,
        GoundMouvement[] sols,
        float vitesseSolAvantRalentissement = -1f)
    {
        if (donnees == null)
        {
            Debug.LogError("[SessionManager] DonnéesSession non assigné dans l'Inspector — sauvegarde annulée.");
            return;
        }

        joueur.SauvegarderDansSession(donnees);
        scoreManager.SauvegarderDansSession(donnees);
        spawner.SauvegarderDansSession(donnees);

        // Si une vitesse pré-ralentissement est fournie, on la sauvegarde
        // à la place de la vitesse actuelle (qui serait 0 après le freinage).
        if (vitesseSolAvantRalentissement >= 0f)
            donnees.vitesseSol = vitesseSolAvantRalentissement;
        else if (sols != null && sols.Length > 0)
            donnees.vitesseSol = sols[0].speed;

        donnees.sessionValide = true;
    }
}
