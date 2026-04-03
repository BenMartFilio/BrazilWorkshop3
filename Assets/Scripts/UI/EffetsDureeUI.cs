using UnityEngine;
using ObjetsSpeciaux;

/// <summary>
/// Pilote l'anneau radial de timer directement sur le slot d'inventaire de l'objet utilisé.
/// Quand l'effet se termine et que la quantité était 0 (dernière utilisation),
/// le slot grisé est détruit via <see cref="InventaireObjetsUI.SupprimerSlot"/>.
/// Toute la logique de dessin de l'anneau vit dans <see cref="ItemSlotUI"/>.
/// </summary>
[DefaultExecutionOrder(10)] // After InventaireObjetsUI (order 0) has run PopulerSlots
public class EffetsDureeUI : MonoBehaviour
{
    // ── État ──────────────────────────────────────────────────────────────────
    private struct TimerActif
    {
        public string id;
        public Color  couleur;
    }

    private EffetsObjetsSpeciaux _effets;
    private InventaireObjetsUI   _inventaire;

    private TimerActif _timerActuel;
    private bool       _timerValide;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        _effets     = FindFirstObjectByType<EffetsObjetsSpeciaux>();
        _inventaire = FindFirstObjectByType<InventaireObjetsUI>();

        if (_effets == null)
        {
            Debug.LogError("[EffetsDureeUI] EffetsObjetsSpeciaux introuvable dans la scène.");
            return;
        }

        if (_inventaire == null)
        {
            Debug.LogError("[EffetsDureeUI] InventaireObjetsUI introuvable dans la scène.");
            return;
        }

        _effets.OnEffetDemarre += OnEffetDemarre;
        _effets.OnEffetTermine += OnEffetTermine;
        _effets.OnEffetAnnule  += OnEffetAnnule;
    }

    private void OnDestroy()
    {
        if (_effets == null) return;
        _effets.OnEffetDemarre -= OnEffetDemarre;
        _effets.OnEffetTermine -= OnEffetTermine;
        _effets.OnEffetAnnule  -= OnEffetAnnule;
    }

    // ── Événements ────────────────────────────────────────────────────────────

    private void OnEffetDemarre(string id, float duree, Color couleur, string nomAffichage)
    {
        // Arrêter l'anneau précédent sur son slot sans supprimer le slot.
        // Guard: if OnEffetAnnule already cleared _timerValide, skip — the flash coroutine owns cleanup.
        if (_timerValide)
            StopperAnneau(_timerActuel.id, supprimerSlot: false);

        _timerActuel = new TimerActif { id = id, couleur = couleur };
        _timerValide = true;

        ItemSlotUI slot = _inventaire?.ObtenirSlot(id);
        if (slot != null)
            slot.DemarrerTimer(couleur, duree);
    }

    private void OnEffetTermine(string id)
    {
        if (!_timerValide || _timerActuel.id != id) return;

        _timerValide = false;

        // Supprimer le slot seulement si le bouton est non-interactable (quantité = 0).
        bool derniereUtilisation = EstEpuise(id);
        StopperAnneau(id, supprimerSlot: derniereUtilisation);
    }

    /// <summary>
    /// Called when an active effect is cut short by a new one.
    /// Hands arc ownership to a flash-cancel animation on the slot instead of removing it instantly.
    /// Sets _timerValide = false immediately so the subsequent OnEffetTermine and the incoming
    /// OnEffetDemarre both leave this slot alone while the animation plays.
    /// </summary>
    private void OnEffetAnnule(string id)
    {
        if (!_timerValide || _timerActuel.id != id) return;

        Color  couleur              = _timerActuel.couleur;
        bool   derniereUtilisation  = EstEpuise(id);
        _timerValide = false; // OnEffetTermine and next OnEffetDemarre will now skip this slot.

        ItemSlotUI slot = _inventaire?.ObtenirSlot(id);
        if (slot != null)
            slot.FlasherAnnulation(couleur, () => StopperAnneau(id, supprimerSlot: derniereUtilisation));
        else
            StopperAnneau(id, supprimerSlot: derniereUtilisation);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Retire l'anneau du slot et, si demandé, supprime le slot de la barre.</summary>
    private void StopperAnneau(string id, bool supprimerSlot)
    {
        if (_inventaire == null) return;

        ItemSlotUI slot = _inventaire.ObtenirSlot(id);

        if (slot != null)
            slot.ArreterTimer(supprimerSlot: false);  // retire uniquement l'anneau

        if (supprimerSlot)
            _inventaire.SupprimerSlot(id);            // détruit le GameObject du slot
    }

    /// <summary>
    /// Retourne true si le bouton du slot est non-interactable, ce qui indique
    /// que la quantité est tombée à 0 lors de la dernière utilisation.
    /// </summary>
    private bool EstEpuise(string id)
    {
        if (_inventaire == null) return false;
        ItemSlotUI slot = _inventaire.ObtenirSlot(id);
        return slot != null && slot.EstEpuise;
    }
}
