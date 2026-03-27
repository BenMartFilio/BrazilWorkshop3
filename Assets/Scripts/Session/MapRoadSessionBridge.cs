using UnityEngine;

/// <summary>
/// Pont entre la scène MapRoad et le SessionManager.
/// Place ce composant sur un GameObject de la scène MapRoad.
/// Il restaure l'état au démarrage et expose la méthode pour partir au Barrage.
/// </summary>
public class MapRoadSessionBridge : MonoBehaviour
{
    [Header("Références scène MapRoad")]
    [SerializeField] private PlayerMovement       joueur;
    [SerializeField] private ScoreManager         scoreManager;
    [SerializeField] private SpawnObstacleV2      spawner;
    [SerializeField] private GoundMouvement[]     sols;
    [SerializeField] private BarreProgressionBarrage barreProgression;

    private void Start()
    {
        if (SessionManager.Instance == null) return;

        SessionManager.Instance.RestaurerMapRoad(joueur, scoreManager, spawner, sols);

        // Si on revient d'un barrage réussi, la progression repart de 0
        // car SpawnObstacleV2.RestaurerProgressionDepuisSession a déjà tiré un nouveau seuil.
        if (barreProgression != null)
            barreProgression.RéinitialiserPourNouveauCycle();
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

        SessionManager.Instance.AllerAuBarrage(joueur, scoreManager, spawner, sols);
    }
}
