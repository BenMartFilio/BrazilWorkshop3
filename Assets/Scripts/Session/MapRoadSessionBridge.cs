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
            SessionManager.Instance.RestaurerMapRoad(joueur, scoreManager, spawner, end, sols);

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

        SessionManager.Instance.AllerAuBarrage(joueur, scoreManager, spawner, end, sols);
    }
}
