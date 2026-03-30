using System;
using UnityEngine;
using UnityEngine.UI;

namespace Barrage.UI
{
    /// <summary>
    /// Change le sprite du <see cref="UnityEngine.UI.Image"/> de VisuelGarde
    /// en fonction du niveau de patience restant dans <see cref="BarrePatience"/>.
    ///
    /// Trois états :
    ///   - Vert  (patience > seuilMoyenne) → <see cref="spriteVert"/>   — garde loin
    ///   - Orange (seuilBasse &lt; patience ≤ seuilMoyenne) → <see cref="spriteOrange"/> — garde tendu
    ///   - Rouge (patience ≤ seuilBasse)  → <see cref="spriteRouge"/>  — garde très proche
    ///
    /// Émet <see cref="OnEtatChange"/> à chaque transition pour que les bulles
    /// de dialogue puissent basculer selon l'état courant.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class VisuelGardeUI : MonoBehaviour
    {
        // ── Enum public ───────────────────────────────────────────────────────

        public enum EtatGarde { Vert, Orange, Rouge }

        // ── Événement ─────────────────────────────────────────────────────────

        /// <summary>Déclenché à chaque changement d'état du garde.</summary>
        public event Action<EtatGarde> OnEtatChange;

        // ── Champs sérialisés ─────────────────────────────────────────────────

        [Header("Sprites")]
        [Tooltip("Sprite affiché quand la patience est élevée (garde loin).")]
        [SerializeField] private Sprite spriteVert;

        [Tooltip("Sprite affiché quand la patience est moyenne (garde tendu).")]
        [SerializeField] private Sprite spriteOrange;

        [Tooltip("Sprite affiché quand la patience est basse (garde très proche).")]
        [SerializeField] private Sprite spriteRouge;

        [Header("Références")]
        [Tooltip("BarrePatience pilotant l'état du garde.")]
        [SerializeField] private BarrePatience barrePatience;

        // ── État interne ──────────────────────────────────────────────────────

        private Image     _image;
        private EtatGarde _etatActuel;

        // ── Propriété ─────────────────────────────────────────────────────────

        /// <summary>État courant du garde (mis à jour chaque frame).</summary>
        public EtatGarde EtatCourant => _etatActuel;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _image = GetComponent<Image>();

            // État initial : patience pleine → vert
            _etatActuel   = EtatGarde.Vert;
            _image.sprite = spriteVert;
        }

        private void Update()
        {
            if (barrePatience == null) return;

            EtatGarde nouvelEtat = DéterminerEtat(barrePatience.PatienceNormalisée);

            if (nouvelEtat == _etatActuel) return;

            _etatActuel   = nouvelEtat;
            _image.sprite = SpriteDeEtat(nouvelEtat);
            OnEtatChange?.Invoke(nouvelEtat);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private EtatGarde DéterminerEtat(float patience)
        {
            if (patience > barrePatience.SeuilMoyenne) return EtatGarde.Vert;
            if (patience > barrePatience.SeuilBasse)   return EtatGarde.Orange;
            return EtatGarde.Rouge;
        }

        private Sprite SpriteDeEtat(EtatGarde etat) => etat switch
        {
            EtatGarde.Vert   => spriteVert,
            EtatGarde.Orange => spriteOrange,
            EtatGarde.Rouge  => spriteRouge,
            _                => null
        };
    }
}
