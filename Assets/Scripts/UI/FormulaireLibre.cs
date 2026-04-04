using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Barrage.Formulaires;

namespace Barrage.UI
{
    /// <summary>
    /// Carte formulaire libre dans la PartieBasse.
    /// - Taille fixe, soumise à une gravité faible permanente.
    /// - Pendant le drag : re-parentée dans CoucheGlissement (Canvas root) → drag libre
    ///   sur tout l'écran, y compris la PartieHaute et la MainDuGarde.
    /// - À la relâche : retour dans PartieBasse avec inertie horizontale + gravité.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FormulaireLibre : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // ── Physique ──────────────────────────────────────────────────────
        private const float GRAVITE            = 80f;    // px/s² vers le bas
        private const float VITESSE_CHUTE_MAX  = 250f;   // px/s — vitesse terminale
        private const float DEMI_VIE_DERIVE    = 0.22f;  // s — décroissance horizontale
        private const float VITESSE_MAX_DERIVE = 1200f;  // px/s — plafond inertie au lâcher
        private const float VITESSE_ARRET      = 6f;     // px/s — seuil d'arrêt horizontal
        private const float IMPULSION_REBOND   = 180f;   // px/s — vitesse de rebond au premier contact

        // ── Orientation ───────────────────────────────────────────────────────
        private const float ROTATION_SNAP_DUREE = 0.25f; // s — durée du retour vers l'angle le plus proche à 0°/180°
        private const float ROTATION_SNAP_SEUIL = 1f;    // ° — en dessous, considéré comme déjà aligné

        // ── Secousse des cartes ───────────────────────────────────────────────
        private const float FREQUENCE_SECOUSSE = 22f;    // identique à SecousseEcran

        public FormulaireType Type { get; private set; }

        /// <summary>Expose le RectTransform pour la résolution de collisions externe.</summary>
        public RectTransform Rt => _rectTransform;

        /// <summary>True quand la carte est en cours de drag (dans CoucheGlissement).</summary>
        public bool EstEnDrag => _enDrag;

        private RectTransform _rectTransform;
        private RawImage      _rawImage;
        private FormulaireLibreManager _manager;
        private AudioEventDispatcher _audioEventDispatcher;
        private RectTransform _partieBasse;
        private RectTransform _coucheGlissement;
        private Vector2       _tailleFixe;
        

        private Vector2 _velocity;        // px/s dans l'espace local de _partieBasse (hors drag)
        private Vector2 _lastAnchoredPos;
        private bool    _enDrag;
        private bool    _enDerive;        // dérive horizontale active
        private Vector2 _offsetGlissement;
        private bool    _premierContactPossible; // true juste après un drop, en attente du premier contact
        private bool    _grisée;           // true quand la carte est grisée (barrage terminé)
        private bool    _animéeGameOver;   // true quand la carte converge vers "GAME OVER"
        private Coroutine _rotationSnap;  // coroutine d'alignement vers côté le plus long
        private Coroutine _secousse;      // coroutine de secousse synchronisée avec SecousseEcran

        // ── Initialisation ────────────────────────────────────────────────────

        /// <summary>Initialise la carte avec sa taille fixe et ses dépendances.</summary>
        public void Initialiser(FormulaireType type, FormulaireLibreManager manager,
                                RectTransform partieBasse, RectTransform coucheGlissement,
                                Vector2 tailleFixe, AudioEventDispatcher audioEventDispatcher)
        {
            Type              = type;
            _manager          = manager;
            _partieBasse      = partieBasse;
            _coucheGlissement = coucheGlissement;
            _tailleFixe       = tailleFixe;

            _audioEventDispatcher = audioEventDispatcher;

            _rectTransform = GetComponent<RectTransform>();
            _rawImage      = GetComponent<RawImage>();

            // Taille fixe, ancrée au centre de la PartieBasse
            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rectTransform.pivot     = new Vector2(0.5f, 0.5f);
            _rectTransform.sizeDelta = tailleFixe;
        }

        // ── Physique ──────────────────────────────────────────────────────────

        private void Update()
        {
            // Pendant le drag ou l'animation Game Over, la physique est désactivée
            if (_enDrag || _animéeGameOver) return;

            // Gravité : accélération vers le bas, plafonnée
            _velocity.y = Mathf.Max(_velocity.y - GRAVITE * Time.deltaTime, -VITESSE_CHUTE_MAX);

            // Dérive horizontale : décroissance exponentielle
            if (_enDerive)
            {
                float decay = Mathf.Pow(0.5f, Time.deltaTime / DEMI_VIE_DERIVE);
                _velocity.x *= decay;

                if (Mathf.Abs(_velocity.x) < VITESSE_ARRET)
                {
                    _velocity.x = 0f;
                    _enDerive   = false;
                }
            }

            // Appliquer le mouvement et contraindre dans PartieBasse
            Vector2 newPos = _rectTransform.anchoredPosition + _velocity * Time.deltaTime;
            _rectTransform.anchoredPosition = ClampAvecFriction(newPos);
        }

        // ── Drag & Drop ───────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Bloquer le drag si la carte est grisée (barrage terminé)
            if (_grisée) return;
            _audioEventDispatcher?.PlayAudio(AudioType.PaperGet);
            _enDrag   = true;
            _enDerive = false;
            _velocity = Vector2.zero;

            if (_rawImage != null) _rawImage.raycastTarget = false;

            // Mémoriser la position écran avant re-parentage (SSO : world pos = screen pos)
            Vector3 screenPos = _rectTransform.position;

            // Monter dans CoucheGlissement pour un rendu au-dessus de tout et un drag libre
            transform.SetParent(_coucheGlissement, false);
            transform.SetAsLastSibling();

            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rectTransform.pivot     = new Vector2(0.5f, 0.5f);
            _rectTransform.sizeDelta = _tailleFixe;
            _rectTransform.position  = screenPos; // restaurer la position visuelle

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _coucheGlissement, eventData.position, eventData.pressEventCamera,
                out Vector2 localPos);
            _offsetGlissement = _rectTransform.anchoredPosition - localPos;
            _lastAnchoredPos  = _rectTransform.anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_enDrag) return;

            // Mouvement libre sans contrainte — la carte peut traverser toutes les zones
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _coucheGlissement, eventData.position, eventData.pressEventCamera,
                out Vector2 localPos);

            Vector2 newPos = localPos + _offsetGlissement;

            if (Time.deltaTime > 0f)
                _velocity = (newPos - _lastAnchoredPos) / Time.deltaTime;

            _lastAnchoredPos            = newPos;
            _rectTransform.anchoredPosition = newPos;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_enDrag) return;
            _audioEventDispatcher?.PlayAudio(AudioType.PaperSet);
            _enDrag = false;

            if (_rawImage != null) _rawImage.raycastTarget = true;

            // Priorité : dépôt sur la main du garde.
            // On utilise Camera.main plutôt que eventData.pressEventCamera car
            // pressEventCamera peut être null en Screen Space - Camera si l'EventCamera
            // du Canvas n'est pas configuré — RectangleContainsScreenPoint retombe alors
            // en comportement Screen Space - Overlay et le hit-test échoue silencieusement.
            Camera cam = Camera.main;
            if (_manager.EstSurMainDuGarde(eventData.position, cam))
            {
                _manager.EnvoyerAMainDuGarde(this);
                return;
            }

            // Retour dans PartieBasse en conservant la position visuelle
            RetournerDansPartieBasse();

            // Conserver uniquement la composante horizontale de l'inertie ;
            // la gravité reprend immédiatement en Update
            _velocity.x = Mathf.Clamp(_velocity.x, -VITESSE_MAX_DERIVE, VITESSE_MAX_DERIVE);
            _velocity.y = 0f;

            if (Mathf.Abs(_velocity.x) > VITESSE_ARRET)
                _enDerive = true;

            // Activer la détection du premier contact inter-carte post-drop
            _premierContactPossible = true;

            // Aligner vers le côté le plus long (landscape → 0°/180°, portrait → aucune rotation)
            DémarrerSnapRotation();
        }

        // ── Orientation ───────────────────────────────────────────────────────

        /// <summary>
        /// Lance une coroutine qui ramène la carte vers l'angle 0° ou 180° (côté le plus long horizontal)
        /// si la carte est en mode paysage (largeur > hauteur), sans intervenir si elle est portrait.
        /// </summary>
        private void DémarrerSnapRotation()
        {
            // Seulement si la carte est plus large que haute (côté le plus long = côté horizontal)
            if (_tailleFixe.x <= _tailleFixe.y) return;

            if (_rotationSnap != null) StopCoroutine(_rotationSnap);
            _rotationSnap = StartCoroutine(SnapRotation());
        }

        private IEnumerator SnapRotation()
        {
            float angleDepart = _rectTransform.localEulerAngles.z;
            // Normaliser dans [-180, 180]
            if (angleDepart > 180f) angleDepart -= 360f;

            // L'angle cible le plus proche parmi 0° et ±180°
            float cible = Mathf.Abs(angleDepart) <= 90f ? 0f : (angleDepart > 0f ? 180f : -180f);

            float t = 0f;
            while (t < ROTATION_SNAP_DUREE)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / ROTATION_SNAP_DUREE);
                float angle = Mathf.LerpAngle(angleDepart, cible, p);
                _rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
                yield return null;
            }

            _rectTransform.localEulerAngles = new Vector3(0f, 0f, cible);
            _rotationSnap = null;
        }

        // ── Secousse synchronisée avec SecousseEcran ──────────────────────────

        /// <summary>
        /// Déclenche une secousse de position sur la carte au rythme du camerashake.
        /// Les paramètres doivent correspondre à ceux de SecousseEcran pour rester synchrones.
        /// </summary>
        public void Secouer(float duréeS, float intensitéPx)
        {
            if (_enDrag) return;

            if (_secousse != null)
                StopCoroutine(_secousse);

            _secousse = StartCoroutine(CoroutineSecousse(duréeS, intensitéPx));
        }

        private IEnumerator CoroutineSecousse(float duréeS, float intensitéPx)
        {
            float offsetX = Random.Range(0f, 100f);
            float offsetY = Random.Range(0f, 100f);

            float t = 0f;
            while (t < duréeS)
            {
                // Attendre la fin de Update/LateUpdate pour surcharger la position sans conflit
                yield return new WaitForEndOfFrame();

                // Interrompre si la carte a été saisie pendant la secousse
                if (_enDrag) break;

                t += Time.deltaTime;

                float décroiss = 1f - Mathf.SmoothStep(0f, 1f, t / duréeS);
                float dx = (Mathf.PerlinNoise(offsetX + t * FREQUENCE_SECOUSSE, 0f) - 0.5f) * 2f;
                float dy = (Mathf.PerlinNoise(0f, offsetY + t * FREQUENCE_SECOUSSE) - 0.5f) * 2f;

                // Décalage additif sur la position physique courante
                _rectTransform.anchoredPosition += new Vector2(dx, dy) * intensitéPx * décroiss;
            }

            _secousse = null;
        }

        // ── Animation Game Over ───────────────────────────────────────────────

        /// <summary>
        /// Fige immédiatement la physique de la carte (gravité, inertie) sans déclencher
        /// d'animation. Appelé après la mise en place Game Over pour que les cartes
        /// restent en position.
        /// </summary>
        public void FigerPourGameOver()
        {
            _animéeGameOver = true;
            _enDrag         = false;
            _velocity       = Vector2.zero;

            if (_rawImage != null) _rawImage.raycastTarget = false;
        }

        /// <summary>True si la carte est figée pour l'animation de game over.
        /// Utilisé par FormulaireLibreManager pour exclure ces cartes de la résolution de collisions.</summary>
        public bool EstFigéePourGameOver => _animéeGameOver;

        /// <summary>
        /// Phase 1 : explosé — applique une impulsion violente dans une direction aléatoire
        /// et désactive la physique de contrainte de bords.
        /// </summary>
        public void ExplosionGameOver(Vector2 impulsion)
        {
            _animéeGameOver   = true;
            _enDrag           = false;
            _velocity         = Vector2.zero;

            if (_rawImage != null) _rawImage.raycastTarget = false;

            // Remonter dans la couche de glissement pour ne plus être contrainte par PartieBasse
            transform.SetParent(_coucheGlissement, true);

            // Appliquer l'impulsion manuellement via une coroutine libre
            StartCoroutine(CoroutineExplosion(impulsion));
        }

        private IEnumerator CoroutineExplosion(Vector2 vitesse)
        {
            float duréeExplosion = 0.55f;
            float t = 0f;
            while (t < duréeExplosion)
            {
                t += Time.deltaTime;
                // Ralentissement naturel
                vitesse = Vector2.Lerp(vitesse, Vector2.zero, Time.deltaTime * 2.5f);
                _rectTransform.anchoredPosition += vitesse * Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// Phase 2 : convergence — interpole position, taille et rotation vers les valeurs cibles.
        /// Tinte la carte en blanc pur pour que seule la lettre "GAME OVER" soit visible.
        /// </summary>
        public IEnumerator AnimerVers(Vector2 positionCible, Vector2 tailleCible,
                                      float rotationCible, Color couleurCible, float durée)
        {
            // S'assurer que la carte est bien dans la couche de glissement
            if (transform.parent != _coucheGlissement)
                transform.SetParent(_coucheGlissement, true);

            Vector2 posDepart     = _rectTransform.anchoredPosition;
            Vector2 tailleDepart  = _rectTransform.sizeDelta;
            float   rotDepart     = _rectTransform.localEulerAngles.z;
            if (rotDepart > 180f) rotDepart -= 360f;

            Color   couleurDepart = _rawImage != null ? _rawImage.color : Color.white;

            float t = 0f;
            while (t < durée)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / durée);

                _rectTransform.anchoredPosition = Vector2.Lerp(posDepart, positionCible, p);
                _rectTransform.sizeDelta        = Vector2.Lerp(tailleDepart, tailleCible, p);
                _rectTransform.localEulerAngles = new Vector3(0f, 0f,
                    Mathf.LerpAngle(rotDepart, rotationCible, p));

                if (_rawImage != null)
                    _rawImage.color = Color.Lerp(couleurDepart, couleurCible, p);

                yield return null;
            }

            _rectTransform.anchoredPosition = positionCible;
            _rectTransform.sizeDelta        = tailleCible;
            _rectTransform.localEulerAngles = new Vector3(0f, 0f, rotationCible);
            if (_rawImage != null) _rawImage.color = couleurCible;
        }

        // ── Grisage (barrage validé) ──────────────────────────────────────────

        private static readonly Color COULEUR_GRISÉE = new Color(0.45f, 0.45f, 0.45f, 0.7f);

        /// <summary>
        /// Grise visuellement la carte et désactive son interaction drag.
        /// Appelé quand le joueur valide le barrage courant.
        /// </summary>
        public void Griser()
        {
            _grisée = true;

            if (_rawImage != null)
            {
                _rawImage.color         = COULEUR_GRISÉE;
                _rawImage.raycastTarget = false;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Re-parente la carte dans PartieBasse en conservant la position écran.</summary>
        private void RetournerDansPartieBasse()
        {
            Vector3 screenPos = _rectTransform.position;

            transform.SetParent(_partieBasse, false);

            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rectTransform.pivot     = new Vector2(0.5f, 0.5f);
            _rectTransform.sizeDelta = _tailleFixe;
            _rectTransform.position  = screenPos;

            // Clamp immédiat si relâchée hors de PartieBasse
            _rectTransform.anchoredPosition =
                ClampAvecFriction(_rectTransform.anchoredPosition);
        }

        /// <summary>
        /// Applique une correction de position issue de la résolution de collision inter-cartes.
        /// Appelée par FormulaireLibreManager.LateUpdate — jamais pendant le drag.
        /// Déclenche un rebond élastique au premier contact post-drop.
        /// </summary>
        public void AppliquerCorrectionCollision(Vector2 correction)
        {
            if (_enDrag || _animéeGameOver) return;

            // Premier contact après un drop : impulsion de rebond dans la direction opposée au push
            if (_premierContactPossible && correction.sqrMagnitude > 0f)
            {
                _premierContactPossible = false;
                Vector2 direction = correction.normalized;
                AjouterImpulsion(direction * IMPULSION_REBOND);
            }

            Rect  r  = _partieBasse.rect;
            float hw = _tailleFixe.x * 0.5f;
            float hh = _tailleFixe.y * 0.5f;
            Vector2 p = _rectTransform.anchoredPosition + correction;

            _rectTransform.anchoredPosition = new Vector2(
                Mathf.Clamp(p.x, r.xMin + hw, r.xMax - hw),
                Mathf.Clamp(p.y, r.yMin + hh, r.yMax - hh));
        }

        /// <summary>Ajoute une impulsion instantanée à la vitesse physique de la carte.</summary>
        public void AjouterImpulsion(Vector2 impulsion)
        {
            _velocity += impulsion;
            _velocity.x = Mathf.Clamp(_velocity.x, -VITESSE_MAX_DERIVE, VITESSE_MAX_DERIVE);
            _velocity.y = Mathf.Max(_velocity.y, -VITESSE_CHUTE_MAX);

            if (Mathf.Abs(_velocity.x) > VITESSE_ARRET)
                _enDerive = true;
        }

        /// <summary>
        /// Contraint la position dans les bounds de PartieBasse.
        /// Annule la composante de vitesse correspondante en cas de collision avec un bord.
        /// </summary>
        private Vector2 ClampAvecFriction(Vector2 pos)
        {
            Rect  r  = _partieBasse.rect;
            float hw = _tailleFixe.x * 0.5f;
            float hh = _tailleFixe.y * 0.5f;

            float cx = Mathf.Clamp(pos.x, r.xMin + hw, r.xMax - hw);
            float cy = Mathf.Clamp(pos.y, r.yMin + hh, r.yMax - hh);

            if (!Mathf.Approximately(cx, pos.x)) _velocity.x = 0f;
            if (!Mathf.Approximately(cy, pos.y)) _velocity.y = 0f;

            return new Vector2(cx, cy);
        }
    }
}
