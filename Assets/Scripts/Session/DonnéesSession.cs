using UnityEngine;

/// <summary>
/// ScriptableObject portant toutes les données de session persistantes
/// entre la scène MapRoad et la scène Barrage.
/// Crée une instance via Assets/Create/Session/Données de Session.
/// </summary>
[CreateAssetMenu(menuName = "Session/Données de Session", fileName = "DonnéesSession")]
public class DonnéesSession : ScriptableObject
{
    [Header("Joueur")]
    [Tooltip("Index de la lane du joueur (0=gauche, 1=centre, 2=droite).")]
    public int indexLane = 1;

    [Tooltip("Nombre de pièces collectées.")]
    public int pièces = 0;

    [Header("Score")]
    [Tooltip("Score actuel du joueur.")]
    public int score = 0;

    [Tooltip("Vitesse actuelle du compteur de score.")]
    public float vitesseScore = 39f;

    [Header("Vitesse générale")]
    [Tooltip("Vitesse générale des obstacles et du sol (généralSpeed dans SpawnObstacleV2).")]
    public float vitesseGénérale = 0f;

    [Tooltip("Vitesse actuelle du sol (GoundMouvement.speed).")]
    public float vitesseSol = 5f;

    [Header("État")]
    [Tooltip("Vrai si une session MapRoad a déjà été sauvegardée.")]
    public bool sessionValide = false;

    /// <summary>Remet toutes les valeurs à leur état initial.</summary>
    public void Réinitialiser()
    {
        indexLane      = 1;
        pièces         = 0;
        score          = 0;
        vitesseScore   = 39f;
        vitesseGénérale = 0f;
        vitesseSol     = 5f;
        sessionValide  = false;
    }
}
