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

    private void Awake()
    {
        if (donnéesSession == null)
        {
            Debug.LogError("[SessionReinitialiseur] donnéesSession non assignée dans l'Inspector — " +
                           "la réinitialisation de session n'a pas eu lieu !");
            return;
        }

        donnéesSession.Reinitialiser();
        Debug.Log("[SessionReinitialiseur] DonnéesSession réinitialisée au chargement du MainMenu.");
    }
}
