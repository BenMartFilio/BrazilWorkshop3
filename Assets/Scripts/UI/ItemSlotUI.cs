using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Contrôleur d'un slot d'objet spécial dans le panneau d'inventaire.
/// Configuré via <see cref="Initialiser"/> lors de la création dynamique du slot.
/// </summary>
public class ItemSlotUI : MonoBehaviour
{
    private const float ALPHA_GRISE = 0.40f;

    [SerializeField] private Image imageFond;
    [SerializeField] private Image imageSprite;
    [SerializeField] private TextMeshProUGUI texteQuantite;
    [SerializeField] private Button bouton;

    /// <summary>Configure le slot avec les données de l'objet et enregistre le callback de clic.</summary>
    public void Initialiser(DefinitionObjetSpecial definition, int quantite, System.Action<string> onClic)
    {
        if (definition == null)
        {
            Debug.LogError("[ItemSlotUI] Définition nulle passée à Initialiser.");
            return;
        }

        imageSprite.sprite = definition.sprite;
        texteQuantite.text = "x" + quantite;

        bouton.onClick.RemoveAllListeners();

        if (!definition.estPassif)
            bouton.onClick.AddListener(() => onClic(definition.identifiant));

        AppliquerEtatQuantite(quantite, definition.estPassif);
    }

    /// <summary>Met à jour uniquement le label de quantité et l'état interactif du bouton.</summary>
    public void MettreAJourQuantite(int quantite, bool estPassif = false)
    {
        texteQuantite.text = "x" + quantite;
        AppliquerEtatQuantite(quantite, estPassif);
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void AppliquerEtatQuantite(int quantite, bool estPassif = false)
    {
        bool disponible = quantite > 0 && !estPassif;
        bouton.interactable = disponible;

        if (imageFond != null)
        {
            Color couleur = imageFond.color;
            couleur.a = disponible ? 1f : ALPHA_GRISE;
            imageFond.color = couleur;
        }
    }
}
