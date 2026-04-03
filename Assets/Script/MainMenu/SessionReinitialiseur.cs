using Barrage.Formulaires;
using UnityEngine;

/// <summary>
/// À placer sur n'importe quel GameObject de la scène MainMenu.
/// Réinitialise les données de session dès que le menu charge,
/// garantissant un état propre pour chaque nouvelle partie
/// quel que soit le chemin d'entrée (game over, éditeur, retour menu, etc.).
/// </summary>
public class SessionReinitialiseur : MonoBehaviour
{
    [Tooltip("ScriptableObject partagé contenant toutes les données de session.")]
    [SerializeField] private DonnéesSession donnéesSession;

    [Tooltip("Inventaire de formulaires à remettre à zéro pour chaque nouvelle partie.")]
    [SerializeField] private FormulaireInventaire formulaireInventaire;
    [SerializeField] private AudioClip _MainMenuMusic;

    

    private void Awake()
    {
        SoundManager.Instance.PlayMusicWithLowPass(_MainMenuMusic);
        if (donnéesSession == null)
        {
            Debug.LogError("[SessionReinitialiseur] donnéesSession non assignée dans l'Inspector — " +
                           "la réinitialisation de session n'a pas eu lieu !");
            return;
        }

        donnéesSession.Reinitialiser();
        Debug.Log("[SessionReinitialiseur] DonnéesSession réinitialisée au chargement du MainMenu.");

        if (formulaireInventaire != null)
        {
            formulaireInventaire.ResetInventaire();
            Debug.Log("[SessionReinitialiseur] FormulaireInventaire réinitialisé au chargement du MainMenu.");
        }
        else
        {
            Debug.LogWarning("[SessionReinitialiseur] FormulaireInventaire non assigné — l'inventaire ne sera pas remis à zéro.");
        }
    }
}
