using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Barrage.Formulaires;

namespace Barrage.UI
{
    /// <summary>
    /// Comportement drag-and-drop d'une carte formulaire dans l'UI.
    /// Les cartes ont une taille fixe héritée de leur prefab source.
    /// Pendant le drag elles passent dans la CoucheGlissement en conservant leur taille,
    /// puis animent leur retour centré dans la poche cible.
    /// Seul le formulaire au sommet de sa poche peut être attrapé.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FormulaireUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float DUREE_CHUTE = 0.35f;

        public FormulaireType Type { get; private set; }
        [SerializeField] private AudioEventDispatcher _audioEventDispatcher;
        [SerializeField] private AudioType _paperGet;
        [SerializeField] private AudioType _paperSet;
        private RectTransform _rectTransform;
        private RawImage _rawImage;
        private FormulaireUIManager _uiManager;
        private PocheUI _pocheActuelle;
        private Vector2 _tailleOriginale;
        private Vector2 _offsetGlissement;
        private bool _dragActif;
        private Coroutine _coroutineChute;

        /// <summary>Initialise la carte avec son type, sa taille fixe et le gestionnaire UI.</summary>
        public void Initialiser(FormulaireType type, Vector2 tailleOriginale, FormulaireUIManager uiManager)
        {
            Type = type;
            _tailleOriginale = tailleOriginale;
            _uiManager = uiManager;
            _rectTransform = GetComponent<RectTransform>();
            _rawImage = GetComponent<RawImage>();
        }

        /// <summary>Assigne ou retire la poche courante de cette carte.</summary>
        public void AssignerPoche(PocheUI poche) => _pocheActuelle = poche;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_coroutineChute != null) return;
            if (_pocheActuelle != null && !_pocheActuelle.EstAuSommet(this)) return;

            if (_audioEventDispatcher != null) _audioEventDispatcher.PlayAudio(_paperGet);

            _dragActif = true;
            _pocheActuelle?.RetirerFormulaire(this);

            if (_rawImage != null) _rawImage.raycastTarget = false;

            // Mémoriser position monde avant le re-parentage
            Vector3 centre = _rectTransform.position;

            // Passer dans la CoucheGlissement en taille fixe
            transform.SetParent(_uiManager.CoucheGlissement, false);
            transform.SetAsLastSibling();

            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rectTransform.pivot     = new Vector2(0.5f, 0.5f);
            _rectTransform.sizeDelta = _tailleOriginale;
            _rectTransform.position  = centre;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _uiManager.CoucheGlissement, eventData.position,
                eventData.pressEventCamera, out Vector2 localPos);
            _offsetGlissement = _rectTransform.anchoredPosition - localPos;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragActif) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _uiManager.CoucheGlissement, eventData.position,
                eventData.pressEventCamera, out Vector2 localPos);

            _rectTransform.anchoredPosition = localPos + _offsetGlissement;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragActif) return;
            _dragActif = false;

            if (_rawImage != null) _rawImage.raycastTarget = true;

            if (_audioEventDispatcher != null) _audioEventDispatcher.PlayAudio(_paperSet);

            Vector2 screenPos = eventData.position;
            // Camera.main est utilisée à la place de eventData.pressEventCamera qui peut être
            // null en Screen Space - Camera (EventCamera non configuré sur le Canvas),
            // ce qui ferait échouer silencieusement les hit-tests de RectangleContainsScreenPoint.
            Camera cam = Camera.main;

            if (_uiManager.EstSurMainDuGarde(screenPos, cam))
            {
                _uiManager.EnvoyerAMainDuGarde(this);
                return;
            }

            PocheUI cible = _uiManager.TrouverPocheSousPointeur(screenPos, cam)
                          ?? _uiManager.TrouverPocheProche(_rectTransform.position);

            _coroutineChute = StartCoroutine(TomberVersPoche(cible));
        }

        private IEnumerator TomberVersPoche(PocheUI poche)
        {
            // Position monde actuelle (carte dans CoucheGlissement)
            Vector3 positionMonde = _rectTransform.position;

            // Enregistrer dans la poche et reparenter
            int index = poche.AjouterFormulaire(this);
            transform.SetParent(poche.transform, false);
            transform.SetAsLastSibling();

            // Rester en taille fixe centrée
            _rectTransform.anchorMin    = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax    = new Vector2(0.5f, 0.5f);
            _rectTransform.pivot        = new Vector2(0.5f, 0.5f);
            _rectTransform.sizeDelta    = _tailleOriginale;

            // Position cible dans la poche
            Vector2 positionCible = poche.ObtenirPositionPourIndex(index);

            // Convertir la position monde en coordonnées locales de la poche.
            // On utilise Camera.main pour correspondre à la caméra du Canvas (Screen Space - Camera).
            // WorldToScreenPoint(null, ...) utilisait la première caméra trouvée par Unity,
            // ce qui pouvait produire une position de départ décalée.
            Camera cam = Camera.main;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                poche.RectTransform,
                RectTransformUtility.WorldToScreenPoint(cam, positionMonde),
                cam, out Vector2 positionDepart);

            _rectTransform.anchoredPosition = positionDepart;

            float elapsed = 0f;
            while (elapsed < DUREE_CHUTE)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / DUREE_CHUTE);
                _rectTransform.anchoredPosition = Vector2.Lerp(positionDepart, positionCible, t);
                yield return null;
            }

            _rectTransform.anchoredPosition = positionCible;
            _coroutineChute = null;
        }
    }
}
