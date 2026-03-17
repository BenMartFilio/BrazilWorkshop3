using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Barrage.Formulaires;

namespace Barrage.UI
{
    /// <summary>
    /// Comportement drag-and-drop d'une carte formulaire dans l'UI.
    /// Seul le formulaire au sommet de sa poche peut être attrapé.
    /// Si relâché hors d'une zone valide, la carte retombe dans la poche la plus proche.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FormulaireUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float DUREE_CHUTE = 0.35f;

        public FormulaireType Type { get; private set; }

        private RectTransform _rectTransform;
        private RawImage _rawImage;
        private FormulaireUIManager _uiManager;
        private PocheUI _pocheActuelle;
        private Vector2 _offsetGlissement;
        private bool _dragActif;
        private Coroutine _coroutineChute;

        /// <summary>Initialise la carte avec son type et le gestionnaire UI.</summary>
        public void Initialiser(FormulaireType type, FormulaireUIManager uiManager)
        {
            Type = type;
            _uiManager = uiManager;
            _rectTransform = GetComponent<RectTransform>();
            _rawImage = GetComponent<RawImage>();
        }

        /// <summary>Assigne ou retire la poche courante de cette carte.</summary>
        public void AssignerPoche(PocheUI poche) => _pocheActuelle = poche;

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Bloquer le drag pendant une animation de chute
            if (_coroutineChute != null) return;
            // Seul le sommet de la pile est attrapable
            if (_pocheActuelle != null && !_pocheActuelle.EstAuSommet(this)) return;

            _dragActif = true;
            _pocheActuelle?.RetirerFormulaire(this);

            // Désactiver le raycast pour ne pas bloquer la détection des zones sous la carte
            if (_rawImage != null) _rawImage.raycastTarget = false;

            // Déplacer vers la couche de glissement (rendu au-dessus de tout)
            transform.SetParent(_uiManager.CoucheGlissement, true);
            transform.SetAsLastSibling();

            // Calculer l'offset entre le curseur et le centre de la carte
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _uiManager.CoucheGlissement,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPos);
            _offsetGlissement = _rectTransform.anchoredPosition - localPos;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragActif) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _uiManager.CoucheGlissement,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPos);

            _rectTransform.anchoredPosition = localPos + _offsetGlissement;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragActif) return;
            _dragActif = false;

            if (_rawImage != null) _rawImage.raycastTarget = true;

            Vector2 screenPos = eventData.position;
            Camera cam = eventData.pressEventCamera;

            // Priorité : main du garde
            if (_uiManager.EstSurMainDuGarde(screenPos, cam))
            {
                _uiManager.EnvoyerAMainDuGarde(this);
                return;
            }

            // Sinon : poche sous le pointeur, ou la plus proche
            PocheUI cible = _uiManager.TrouverPocheSousPointeur(screenPos, cam)
                          ?? _uiManager.TrouverPocheProche(_rectTransform.position);

            _coroutineChute = StartCoroutine(TomberVersPoche(cible));
        }

        private IEnumerator TomberVersPoche(PocheUI poche)
        {
            int index = poche.AjouterFormulaire(this);

            // Re-parentage en conservant la position monde
            transform.SetParent(poche.transform, true);
            transform.SetAsLastSibling();

            Vector2 posDepart = _rectTransform.anchoredPosition;
            Vector2 posCible = poche.ObtenirPositionPourIndex(index);

            float elapsed = 0f;
            while (elapsed < DUREE_CHUTE)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / DUREE_CHUTE);
                _rectTransform.anchoredPosition = Vector2.Lerp(posDepart, posCible, t);
                yield return null;
            }

            _rectTransform.anchoredPosition = posCible;
            _coroutineChute = null;
        }
    }
}
