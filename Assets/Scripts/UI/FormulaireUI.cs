using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Barrage.Formulaires;

namespace Barrage.UI
{
    /// <summary>
    /// Comportement drag-and-drop d'une carte formulaire dans l'UI.
    /// Au repos, les cartes s'étirent pour remplir leur poche (ancres 0→1).
    /// Pendant le drag elles passent dans la CoucheGlissement avec une taille fixe,
    /// puis animent leur retour en stretch à la fin du drag.
    /// Seul le formulaire au sommet de sa poche peut être attrapé.
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
            if (_coroutineChute != null) return;
            if (_pocheActuelle != null && !_pocheActuelle.EstAuSommet(this)) return;

            _dragActif = true;
            _pocheActuelle?.RetirerFormulaire(this);

            if (_rawImage != null) _rawImage.raycastTarget = false;

            // Mémoriser taille et position monde avant le re-parentage
            float largeur = _rectTransform.rect.width  * _rectTransform.lossyScale.x;
            float hauteur = _rectTransform.rect.height * _rectTransform.lossyScale.y;
            Vector3 centre = _rectTransform.position;

            // Passer en mode taille fixe dans la CoucheGlissement
            transform.SetParent(_uiManager.CoucheGlissement, false);
            transform.SetAsLastSibling();

            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rectTransform.pivot     = new Vector2(0.5f, 0.5f);
            _rectTransform.sizeDelta = new Vector2(largeur, hauteur);
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

            Vector2 screenPos = eventData.position;
            Camera cam        = eventData.pressEventCamera;

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
            // Taille et position monde actuelles (carte en mode fixe dans CoucheGlissement)
            Vector3 positionMonde = _rectTransform.position;
            float largeurMonde    = _rectTransform.rect.width  * _rectTransform.lossyScale.x;
            float hauteurMonde    = _rectTransform.rect.height * _rectTransform.lossyScale.y;

            // Enregistrer dans la poche et basculer en mode stretch
            int index = poche.AjouterFormulaire(this);
            transform.SetParent(poche.transform, false);
            transform.SetAsLastSibling();

            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.one;
            _rectTransform.pivot     = new Vector2(0.5f, 0.5f);

            // Offsets cibles
            var (oMinCible, oMaxCible) = poche.ObtenirOffsetsPourIndex(index);

            // Calculer les offsets de départ depuis la position monde courante
            var pocheRT = poche.RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                pocheRT,
                RectTransformUtility.WorldToScreenPoint(null, positionMonde),
                null, out Vector2 localCenter);

            float dL = largeurMonde  * 0.5f;
            float dH = hauteurMonde  * 0.5f;
            float pw = pocheRT.rect.width;
            float ph = pocheRT.rect.height;

            Vector2 oMinDepart = new Vector2(localCenter.x - dL,      localCenter.y - dH);
            Vector2 oMaxDepart = new Vector2(localCenter.x + dL - pw, localCenter.y + dH - ph);

            _rectTransform.offsetMin = oMinDepart;
            _rectTransform.offsetMax = oMaxDepart;

            float elapsed = 0f;
            while (elapsed < DUREE_CHUTE)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / DUREE_CHUTE);
                _rectTransform.offsetMin = Vector2.Lerp(oMinDepart, oMinCible, t);
                _rectTransform.offsetMax = Vector2.Lerp(oMaxDepart, oMaxCible, t);
                yield return null;
            }

            _rectTransform.offsetMin = oMinCible;
            _rectTransform.offsetMax = oMaxCible;
            _coroutineChute = null;
        }
    }
}
