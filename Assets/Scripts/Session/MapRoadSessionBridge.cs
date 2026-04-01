using UnityEngine;

/// <summary>
/// Pont entre la scène MapRoad et le SessionManager.
/// Restaure l'état de session au démarrage et expose le déclenchement du barrage.
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

    private void Start()
    {
        if (SessionManager.Instance != null)
        {
            // RestaurerMapRoad gère les deux cas :
            //   • sessionValide=true  → restauration complète (retour de barrage)
            //   • sessionValide=false → réinitialisation propre (nouvelle partie / game over)
            SessionManager.Instance.RestaurerMapRoad(joueur, scoreManager, spawner, end, sols);
        }

        // Réinitialiser la barre de progression uniquement quand on vient du Barrage
        // (session valide au moment de l'appel précédent — mais elle est invalidée dans RestaurerMapRoad).
        // On laisse BarreProgressionBarrage.Start() gérer son propre état initial.
        // RéinitialiserPourNouveauCycle() n'est plus appelé ici car il écrasait
        // le cycle en cours lors d'un simple retour de barrage.
    }

    /// <summary>
    /// Sauvegarde l'état et charge la scène Barrage.
    /// À appeler depuis la logique de déclenchement du barrage.
    /// La vitesse du sol est lue AVANT tout ralentissement — si le sol a déjà ralenti
    /// au moment de l'appel, passer vitesseSolAvantRalentissement explicitement.
    /// </summary>
    public void DéclencherBarrage()
    {
        if (SessionManager.Instance == null)
        {
            Debug.LogError("[MapRoadSessionBridge] SessionManager introuvable dans la scène.");
            return;
        }

        // Capturer la vitesse actuelle du sol comme vitesse de référence.
        // Si des systèmes de ralentissement ont déjà tourné, la vitesse peut être réduite —
        // dans ce cas, SegmentBarrage.DeclencherTransition() doit être utilisé à la place
        // car il capture la vitesse avant le ralentissement.
        float vitesseAvantRalentissement = sols != null && sols.Length > 0 ? sols[0].speed : -1f;

        SessionManager.Instance.AllerAuBarrage(
            joueur, scoreManager, spawner, end, sols,
            vitesseSolAvantRalentissement: vitesseAvantRalentissement);
    }
}
