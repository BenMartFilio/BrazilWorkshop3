using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Barrage.Formulaires;

namespace Barrage.UI
{
    /// <summary>
    /// Carte formulaire libre dans la PartieBasse.
    /// Taille fixe, draggable partout dans la zone, avec inertie (dérive) à la relâche.
    /// Si déposée sur la MainDuGarde, elle est remise au garde.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FormulaireLibre : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float DEMI_VIE_DERIVE   = 0.22f;  // la vitesse est divisée par 2 toutes les 0.22s
        private const float VITESSE_MAX_DERIVE = 1200f;  // px/s — plafond d'inertie
        private const float VITESSE_ARRET      = 8f;    // px/s — seuil d'arrêt

        public FormulaireType Type { get; private set; }

        private RectTransform _rectTransform;
        private RawImage _rawImage;
        private FormulaireLibreManager _manager;
        private RectTransform _partieBasse;
        private Vector2 _tailleFixe;

        private Vector2 _velocity;
        private Vector2 _lastAnchoredPos;
        private bool _enDerive;
        private bool _dragActif;
        private Vector2 _offsetGlissement;

        /// <summary>Initialise la carte avec sa taille fixe et ses dépendances.</summary>
        public void Initialiser(FormulaireType type, FormulaireLibreManager manager,
                                RectTransform partieBasse, Vector2 tailleFixe)
        {
            Type         = type;
            _manager     = manager;
            _partieBasse = partieBasse;
            _tailleFixe  = tailleFixe;

            _rectTransform = GetComponent<RectTransform>();
            _rawImage      = GetComponent<RawImage>();

            // Ancre centrée, taille fixe
            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rectTransform.pivot     = new Vector2(0.5f, 0.5f);
            _rectTransform.sizeDelta = tailleFixe;
        }

        private void Update()
        {
            if (!_enDerive) return;

            // Décroissance exponentielle de la vitesse
            float decay = Mathf.Pow(0.5f, Time.deltaTime / DEMI_VIE_DERIVE);
            _velocity *= decay;

            Vector2 newPos = _rectTransform.anchoredPosition + _velocity * Time.deltaTime;
            _rectTransform.anchoredPosition = ClampAvecFriction(newPos);

            if (_velocity.sqrMagnitude < VITESSE_ARRET * VITESSE_ARRET)
            {
                _velocity  = Vector2.zero;
                _enDerive  = false;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _enDerive  = false;
            _dragActif = true;
            _velocity  = Vector2.zero;

            if (_rawImage != null) _rawImage.raycastTarget = false;

            // Amener au premier plan dans la PartieBasse
            transform.SetAsLastSibling();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _partieBasse, eventData.position, eventData.pressEventCamera, out Vector2 localPos);

            _offsetGlissement = _rectTransform.anchoredPosition - localPos;
            _lastAnchoredPos  = _rectTransform.anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragActif) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _partieBasse, eventData.position, eventData.pressEventCamera, out Vector2 localPos);

            Vector2 newPos = ClampAvecFriction(localPos + _offsetGlissement);

            // Calculer la vitesse pour l'inertie
            if (Time.deltaTime > 0f)
                _velocity = (newPos - _lastAnchoredPos) / Time.deltaTime;

            _lastAnchoredPos            = newPos;
            _rectTransform.anchoredPosition = newPos;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragActif) return;
            _dragActif = false;

            if (_rawImage != null) _rawImage.raycastTarget = true;

            // Priorité : dépôt sur la main du garde
            if (_manager.EstSurMainDuGarde(eventData.position, eventData.pressEventCamera))
            {
                _manager.EnvoyerAMainDuGarde(this);
                return;
            }

            // Sinon : appliquer l'inertie
            _velocity = Vector2.ClampMagnitude(_velocity, VITESSE_MAX_DERIVE);
            if (_velocity.sqrMagnitude > VITESSE_ARRET * VITESSE_ARRET)
                _enDerive = true;
        }

        /// <summary>
        /// Clamp la position dans les bounds de la PartieBasse.
        /// Annule la composante de vitesse correspondante en cas de collision avec le bord.
        /// </summary>
        private Vector2 ClampAvecFriction(Vector2 pos)
        {
            Rect r  = _partieBasse.rect;
            float hw = _tailleFixe.x * 0.5f;
            float hh = _tailleFixe.y * 0.5f;

            float cx = Mathf.Clamp(pos.x, r.xMin + hw, r.xMax - hw);
            float cy = Mathf.Clamp(pos.y, r.yMin + hh, r.yMax - hh);

            // Stopper la dérive sur le bord touché
            if (!Mathf.Approximately(cx, pos.x)) _velocity.x = 0f;
            if (!Mathf.Approximately(cy, pos.y)) _velocity.y = 0f;

            return new Vector2(cx, cy);
        }
    }
}
