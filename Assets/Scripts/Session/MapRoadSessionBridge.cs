using UnityEngine;

/// <summary>
/// Pont entre la scène MapRoad et le SessionManager.
/// Place ce composant sur un GameObject de la scène MapRoad.
/// Il restaure l'état au démarrage et expose la méthode pour partir au Barrage.
/// </summary>
public class MapRoadSessionBridge : MonoBehaviour
{
    [Header("Références scène MapRoad")]
    [SerializeField] private PlayerMovement    joueur;
    [SerializeField] private ScoreManager      scoreManager;
    [SerializeField] private SpawnObstacleV2   spawner;
    [SerializeField] private GoundMouvement[]  sols;

    private void Start()
    {
        if (SessionManager.Instance != null)
            SessionManager.Instance.RestaurerMapRoad(joueur, scoreManager, spawner, sols);
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
