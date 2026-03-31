using UnityEngine;
using Barrage.Formulaires;

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

    public int[] documentsPosseded = new int[0];

    [Header("Progression barrage")]
    [Tooltip("Signaux TimeManager écoulés depuis le dernier tirage.")]
    public int signauxEcoules = 0;

    [Tooltip("Seuil tiré aléatoirement pour le prochain barrage.")]
    public int prochainBarrageA = 0;

    [Header("Prochaine demande barrage")]
    [Tooltip("Liste des formulaires demandés au prochain barrage. " +
             "Vide = le barrage génère une demande aléatoire.")]
    public FormulaireType[] prochaineDemandeBarrage = new FormulaireType[0];

    /// <summary>True si une demande spécifique a été sauvegardée pour le prochain barrage.</summary>
    public bool AUneDemandeSauvegardée => prochaineDemandeBarrage != null
                                          && prochaineDemandeBarrage.Length > 0;


    public int revive = 0;

    /// <summary>Remet toutes les valeurs à leur état initial.</summary>
    public void Reinitialiser()
    {
        indexLane                 = 1;
        pièces                    = 0;
        score                     = 0;
        vitesseScore              = 39f;
        vitesseGénérale           = 0f;
        vitesseSol                = 5f;
        sessionValide             = false;
        documentsPosseded         = new int[0];
        signauxEcoules            = 0;
        prochainBarrageA          = 0;
        prochaineDemandeBarrage   = new FormulaireType[0];
    }
}
