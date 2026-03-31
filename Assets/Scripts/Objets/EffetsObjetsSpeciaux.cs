using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Barrage.Formulaires;
using Barrage.UI;

namespace ObjetsSpeciaux
{
    /// <summary>
    /// Classe centrale gerant les effets des 8 objets speciaux.
    /// Chaque objet expose une methode publique de declenchement.
    /// Les effets temporels sont geres par des coroutines independantes.
    /// A placer sur un GameObject present dans la scene MapRoad (effets actifs)
    /// et referencant aussi les composants de la scene Barrage (effets passifs).
    /// </summary>
    public class EffetsObjetsSpeciaux : MonoBehaviour
    {
        // ── Constantes : Liasse de Billets ────────────────────────────────────
        private const float LIASSE_RATIO_PATIENCE = 0.50f;

        // ── Constantes : Tirelire Cochon ──────────────────────────────────────
        private const float TIRELIRE_DUREE = 20f;
        private const int   TIRELIRE_MULTIPLICATEUR_PIECES = 2;

        // ── Constantes : Gateau Chinois ───────────────────────────────────────
        private const float GATEAU_DUREE          = 20f;
        private const float GATEAU_CHANCE_ESQUIVE = 0.50f;
        private const float GATEAU_BLINK_DUREE    = 0.35f;
        private const float GATEAU_BLINK_INTERVALLE = 0.05f;

        // ── Constantes : Radar a Obstacles ────────────────────────────────────
        private const float RADAR_DUREE             = 20f;
        private const float RADAR_FLASH_DUREE       = 1.0f;
        private const float RADAR_FLASH_INTERVALLE  = 0.18f;
        private const float RADAR_ANTICIPATION      = 0.5f;
        private const float RADAR_ONDE_INTERVALLE   = 1.2f;
        private const float RADAR_ONDE_DUREE_SCALE  = 0.5f;
        private const float RADAR_ONDE_SCALE_MAX    = 3f;
        private const string RADAR_SORTING_LAYER    = "Player";
        private const int    RADAR_SORTING_ORDER    = 10;

        // ── Constantes : Aspirateur ───────────────────────────────────────────
        private const float ASPIRATEUR_DUREE                 = 20f;
        private const float ASPIRATEUR_MULTIPLICATEUR_TIMING = 2f;

        // ── Constantes : Montre a Gousset ─────────────────────────────────────
        private const float MONTRE_DUREE          = 20f;
        private const float MONTRE_FACTEUR_VITESSE = 0.50f;
        private const float MONTRE_SATURATION     = -100f;

        // ── Dependances : Barrage (objets passifs) ────────────────────────────
        [Header("Barrage -- Objets Passifs")]
        [SerializeField] private BarrePatience barrePatience;
        [SerializeField] private MainDuGardeUI mainDuGarde;
        [SerializeField] private ListeAttenteGarde listeAttenteGarde;

        // ── Dependances : MapRoad (objets actifs) ─────────────────────────────
        [Header("MapRoad -- Objets Actifs")]
        [SerializeField] private PlayerMovement joueur;
        [SerializeField] private SpriteRenderer spriteVoiture;
        [SerializeField] private SpawnObstacleV2 spawner;
        [SerializeField] private Aspiration aspiration;
        [SerializeField] private GoundMouvement[] grounds;

        [Header("Visuels Voiture")]
        [SerializeField] private GameObject haloDoree;
        [SerializeField] private GameObject haloRouge;
        [SerializeField] private GameObject ondeRadar;
        [SerializeField] private Sprite spriteAspirateurVoiture;

        [Header("Radar")]
        [SerializeField] private Sprite spriteExclamation;

        [Header("Post-Process")]
        [SerializeField] private Volume volumePostProcess;

        // ── Etats internes ────────────────────────────────────────────────────
        private bool _tirelireActive;
        private int  _tirelireMultiplicateur = 1;
        private Coroutine _coroutineTirelire;

        private bool _gateauActif;
        private Coroutine _coroutineGateau;

        private bool _radarActif;
        private Coroutine _coroutineRadar;
        private Coroutine _coroutineOnde;

        private bool _aspirateurActif;
        private Sprite _spriteOriginalVoiture;
        private Coroutine _coroutineAspirateur;

        private bool _montreActive;
        private float[] _vitessesOriginalesSols;
        private Coroutine _coroutineMontre;

        private ColorAdjustments _colorAdjustments;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (volumePostProcess != null && volumePostProcess.profile != null)
                volumePostProcess.profile.TryGet(out _colorAdjustments);
            else if (volumePostProcess != null)
                Debug.LogWarning("[EffetsObjetsSpeciaux] Volume assigne mais aucun VolumeProfile -- effet Montre (desaturation) desactive.");
        }

        private void Start()
        {
            // Avertissements pour les assets visuels optionnels non assignes.
            if (spriteAspirateurVoiture == null)
                Debug.LogWarning("[EffetsObjetsSpeciaux] spriteAspirateurVoiture non assigne -- l'effet Aspirateur ne changera pas le sprite de la voiture.");
            if (spriteExclamation == null)
                Debug.LogWarning("[EffetsObjetsSpeciaux] spriteExclamation non assigne -- le Radar affichera un carre rouge de fallback.");
            if (ondeRadar == null)
                Debug.LogWarning("[EffetsObjetsSpeciaux] ondeRadar non assigne -- l'animation d'onde du Radar est desactivee.");
            if (haloDoree == null)
                Debug.LogWarning("[EffetsObjetsSpeciaux] haloDoree non assigne -- le visuel de la Tirelire Cochon est desactive.");
            if (haloRouge == null)
                Debug.LogWarning("[EffetsObjetsSpeciaux] haloRouge non assigne -- le visuel du Gateau Chinois est desactive.");
        }

        private void OnEnable()
        {
            if (spawner != null)
                spawner.OnObstacleSpawne += SignalerNouvelObstacle;
        }

        private void OnDisable()
        {
            if (spawner != null)
                spawner.OnObstacleSpawne -= SignalerNouvelObstacle;
        }

        // ── Objet 1 : Liasse de Billets (passif, barrage) ─────────────────────

        /// <summary>
        /// A appeler depuis le systeme de drag-drop en lieu et place de
        /// FormulaireLibreManager.EnvoyerAMainDuGarde() lorsqu'une liasse de billets
        /// est deposee sur la main du garde.
        /// </summary>
        public void UtiliserLiasseDeBillets()
        {
            if (barrePatience == null)
            {
                Debug.LogError("[EffetsObjetsSpeciaux] barrePatience non assigne.");
                return;
            }

            barrePatience.AjouterPatience(BarrePatience.PATIENCE_MAX * LIASSE_RATIO_PATIENCE);
            Debug.Log("[EffetsObjetsSpeciaux] Liasse de billets -- patience remontee de 50 %.");
        }

        // ── Objet 2 : Formulaire Passe Partout (passif, barrage) ──────────────

        /// <summary>
        /// A appeler depuis le systeme de drag-drop a la place de
        /// FormulaireLibreManager.EnvoyerAMainDuGarde() lorsqu'un formulaire
        /// passe-partout est depose sur la main du garde.
        /// </summary>
        public void UtiliserFormulairePasePartout()
        {
            if (mainDuGarde == null || listeAttenteGarde == null)
            {
                Debug.LogError("[EffetsObjetsSpeciaux] mainDuGarde ou listeAttenteGarde non assigne.");
                return;
            }

            if (listeAttenteGarde.EstTerminée)
            {
                Debug.LogWarning("[EffetsObjetsSpeciaux] Formulaire passe-partout -- barrage deja termine, action ignoree.");
                return;
            }

            mainDuGarde.ValiderAvecPassePartout();
            Debug.Log("[EffetsObjetsSpeciaux] Formulaire passe-partout -- prochain formulaire valide.");
        }

        // ── Objet 3 : Badge du Gouvernement (passif, barrage) ─────────────────

        /// <summary>
        /// A appeler depuis le systeme de drag-drop lorsqu'un badge du gouvernement
        /// est depose n'importe ou pendant un barrage.
        /// Valide l'integralite de la sequence en cours.
        /// </summary>
        public void UtiliserBadgeDuGouvernement()
        {
            if (mainDuGarde == null || listeAttenteGarde == null)
            {
                Debug.LogError("[EffetsObjetsSpeciaux] mainDuGarde ou listeAttenteGarde non assigne.");
                return;
            }

            // Garde-fou : nombre max d'iterations = nombre max raisonnable de formulaires par barrage.
            const int MAX_ITERATIONS = 20;
            int iterations = 0;
            while (!listeAttenteGarde.EstTerminée && iterations < MAX_ITERATIONS)
            {
                mainDuGarde.ValiderAvecPassePartout();
                iterations++;
            }

            if (iterations >= MAX_ITERATIONS)
                Debug.LogWarning("[EffetsObjetsSpeciaux] Badge du gouvernement -- limite d'iterations atteinte, barrage potentiellement non termine.");
            else
                Debug.Log("[EffetsObjetsSpeciaux] Badge du gouvernement -- barrage valide instantanement.");
        }

        // ── Objet 4 : Tirelire Cochon (actif, route) ──────────────────────────

        /// <summary>Declenche l'effet Tirelire Cochon pendant TIRELIRE_DUREE secondes.</summary>
        public void UtiliserTirelireCochon()
        {
            if (_coroutineTirelire != null)
                StopCoroutine(_coroutineTirelire);

            _tirelireActive        = true;
            _tirelireMultiplicateur = TIRELIRE_MULTIPLICATEUR_PIECES;

            if (haloDoree != null)
                haloDoree.SetActive(true);

            _coroutineTirelire = StartCoroutine(EffetTirelire());
        }

        private IEnumerator EffetTirelire()
        {
            yield return new WaitForSeconds(TIRELIRE_DUREE);
            TerminerTirelire();
        }

        private void TerminerTirelire()
        {
            _tirelireActive        = false;
            _tirelireMultiplicateur = 1;

            if (haloDoree != null)
                haloDoree.SetActive(false);

            _coroutineTirelire = null;
        }

        /// <summary>Retourne le multiplicateur de pieces actif (1 par defaut, 2 si Tirelire active).</summary>
        public int ObtenirMultiplicateurPieces()
        {
            return _tirelireMultiplicateur;
        }

        // ── Objet 5 : Gateau Chinois (actif, route) ───────────────────────────

        /// <summary>Declenche l'effet Gateau Chinois pendant GATEAU_DUREE secondes.</summary>
        public void UtiliserGateauChinois()
        {
            if (_coroutineGateau != null)
                StopCoroutine(_coroutineGateau);

            _gateauActif = true;

            if (haloRouge != null)
                haloRouge.SetActive(true);

            _coroutineGateau = StartCoroutine(EffetGateau());
        }

        private IEnumerator EffetGateau()
        {
            yield return new WaitForSeconds(GATEAU_DUREE);
            TerminerGateau();
        }

        private void TerminerGateau()
        {
            _gateauActif = false;

            if (haloRouge != null)
                haloRouge.SetActive(false);

            _coroutineGateau = null;
        }

        /// <summary>
        /// Tente d'esquiver l'obstacle via l'effet Gateau Chinois.
        /// Retourne true si l'obstacle a ete annule (le caller doit ignorer la collision).
        /// L'obstacle clignote brievement avant d'etre desactive.
        /// </summary>
        public bool TenterEsquiveGateau(CollisionObstacle obstacle, Vector3 positionCollision)
        {
            if (!_gateauActif || obstacle == null)
                return false;

            if (Random.value < GATEAU_CHANCE_ESQUIVE)
            {
                obstacle.DeclencherExplosion(positionCollision);
                StartCoroutine(BlinkEtDesactiverObstacle(obstacle.gameObject));
                return true;
            }

            return false;
        }

        private IEnumerator BlinkEtDesactiverObstacle(GameObject obstacle)
        {
            SpriteRenderer sr = obstacle != null ? obstacle.GetComponent<SpriteRenderer>() : null;
            float elapsed = 0f;

            while (elapsed < GATEAU_BLINK_DUREE && obstacle != null)
            {
                elapsed += GATEAU_BLINK_INTERVALLE;
                if (sr != null) sr.enabled = !sr.enabled;
                yield return new WaitForSeconds(GATEAU_BLINK_INTERVALLE);
            }

            if (obstacle != null)
            {
                if (sr != null) sr.enabled = true;
                obstacle.SetActive(false);
            }
        }

        // ── Objet 6 : Radar a Obstacles (actif, route) ────────────────────────

        /// <summary>Declenche l'effet Radar a Obstacles pendant RADAR_DUREE secondes.</summary>
        public void UtiliserRadarObstacles()
        {
            if (_coroutineRadar != null)
                StopCoroutine(_coroutineRadar);

            _radarActif = true;

            if (ondeRadar != null)
            {
                ondeRadar.SetActive(true);
                if (_coroutineOnde != null)
                    StopCoroutine(_coroutineOnde);
                _coroutineOnde = StartCoroutine(AnimerOndeRadar());
            }

            _coroutineRadar = StartCoroutine(EffetRadar());
        }

        private IEnumerator EffetRadar()
        {
            yield return new WaitForSeconds(RADAR_DUREE);
            TerminerRadar();
        }

        private void TerminerRadar()
        {
            _radarActif = false;

            if (_coroutineOnde != null)
            {
                StopCoroutine(_coroutineOnde);
                _coroutineOnde = null;
            }

            if (ondeRadar != null)
                ondeRadar.SetActive(false);

            _coroutineRadar = null;
        }

        private IEnumerator AnimerOndeRadar()
        {
            SpriteRenderer sr = ondeRadar != null ? ondeRadar.GetComponent<SpriteRenderer>() : null;
            Vector3 scaleBase = Vector3.one;

            while (_radarActif)
            {
                // Expansion + fondu
                float t = 0f;
                while (t < RADAR_ONDE_DUREE_SCALE)
                {
                    t += Time.deltaTime;
                    float ratio = Mathf.Clamp01(t / RADAR_ONDE_DUREE_SCALE);
                    float scale = Mathf.Lerp(0f, RADAR_ONDE_SCALE_MAX, ratio);
                    float alpha = Mathf.Lerp(0.4f, 0f, ratio);

                    if (ondeRadar != null)
                        ondeRadar.transform.localScale = scaleBase * scale;

                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = alpha;
                        sr.color = c;
                    }

                    yield return null;
                }

                yield return new WaitForSeconds(RADAR_ONDE_INTERVALLE);
            }
        }

        /// <summary>A appeler par SpawnObstacleV2 lors de chaque spawn si le radar est actif.</summary>
        public void SignalerNouvelObstacle(Vector3 positionObstacle, GameObject obstacle)
        {
            if (!_radarActif)
                return;

            // Ne pas signaler les pièces.
            if (obstacle != null && obstacle.GetComponent<CoinsScript>() != null)
                return;

            StartCoroutine(AfficherIndicateurObstacle(positionObstacle));
        }

        private IEnumerator AfficherIndicateurObstacle(Vector3 positionObstacle)
        {
            // Attendre un court instant avant d'afficher : l'obstacle vient de spawner
            // hors écran, le blink apparaît légèrement en avance sur son arrivée visible.
            yield return new WaitForSeconds(RADAR_ANTICIPATION);

            Camera cam = Camera.main;
            float bordHaut = cam != null
                ? cam.transform.position.y + cam.orthographicSize - 0.5f
                : 5f;

            Vector3 posIndicateur = new Vector3(positionObstacle.x, bordHaut, positionObstacle.z);

            // Creer l'indicateur directement depuis le sprite Exclamation assigne en Inspector.
            GameObject indicateur = new GameObject("IndicateurRadar");
            indicateur.transform.position = posIndicateur;
            indicateur.transform.localScale = Vector3.one * 0.5f;

            SpriteRenderer sr = indicateur.AddComponent<SpriteRenderer>();
            sr.sprite           = spriteExclamation; // null => carre blanc de fallback Unity
            sr.color            = spriteExclamation != null ? Color.white : Color.red;
            sr.sortingLayerName = RADAR_SORTING_LAYER;
            sr.sortingOrder     = RADAR_SORTING_ORDER;

            float t = 0f;
            while (t < RADAR_FLASH_DUREE && indicateur != null)
            {
                t += Time.deltaTime;
                bool visible = (Mathf.FloorToInt(t / RADAR_FLASH_INTERVALLE) % 2) == 0;
                if (sr != null)
                    sr.enabled = visible;

                yield return null;
            }

            if (indicateur != null)
                Destroy(indicateur);
        }

        // ── Objet 7 : Aspirateur (actif, route) ───────────────────────────────

        /// <summary>Declenche l'effet Aspirateur pendant ASPIRATEUR_DUREE secondes.</summary>
        public void UtiliserAspirateur()
        {
            if (_coroutineAspirateur != null)
                StopCoroutine(_coroutineAspirateur);

            _aspirateurActif = true;

            if (spriteVoiture != null)
            {
                _spriteOriginalVoiture = spriteVoiture.sprite;

                if (spriteAspirateurVoiture != null)
                    spriteVoiture.sprite = spriteAspirateurVoiture;
                else
                    Debug.LogWarning("[EffetsObjetsSpeciaux] Aspirateur actif mais spriteAspirateurVoiture non assigne -- assigner le sprite dans l'Inspector.");
            }
            else
            {
                Debug.LogWarning("[EffetsObjetsSpeciaux] Aspirateur actif mais spriteVoiture non assigne.");
            }

            _coroutineAspirateur = StartCoroutine(EffetAspirateur());
        }

        private IEnumerator EffetAspirateur()
        {
            yield return new WaitForSeconds(ASPIRATEUR_DUREE);
            TerminerAspirateur();
        }

        private void TerminerAspirateur()
        {
            _aspirateurActif = false;

            if (spriteVoiture != null && _spriteOriginalVoiture != null)
                spriteVoiture.sprite = _spriteOriginalVoiture;

            _coroutineAspirateur = null;
        }

        /// <summary>
        /// Retourne le multiplicateur de duree d'aspiration (1f par defaut, 2f si Aspirateur actif).
        /// </summary>
        public float ObtenirMultiplicateurTimingAspiration()
        {
            return _aspirateurActif ? ASPIRATEUR_MULTIPLICATEUR_TIMING : 1f;
        }

        // ── Objet 8 : Montre a Gousset (actif, route) ─────────────────────────

        /// <summary>Declenche l'effet Montre a Gousset pendant MONTRE_DUREE secondes.</summary>
        public void UtiliserMontreAGousset()
        {
            // Restaurer d'abord si deja actif pour eviter un double-ralentissement.
            if (_coroutineMontre != null)
            {
                StopCoroutine(_coroutineMontre);
                TerminerMontre();
            }

            _montreActive = true;

            // Sauvegarder les vitesses des sols avant de les modifier.
            if (grounds != null)
            {
                _vitessesOriginalesSols = new float[grounds.Length];
                for (int i = 0; i < grounds.Length; i++)
                    if (grounds[i] != null)
                        _vitessesOriginalesSols[i] = grounds[i].speed;
            }

            // Appliquer le facteur global : tous les UpdateSpeed futurs (spawns, OnTimePassed)
            // utiliseront automatiquement ce facteur -- aucun snapshot necessaire.
            ScrollingElement.FacteurVitesseGlobal = MONTRE_FACTEUR_VITESSE;
            GoundMouvement.FacteurVitesseGlobal   = MONTRE_FACTEUR_VITESSE;

            // Forcer le recalcul immediat sur tous les objets deja actifs.
            spawner?.RefreshVitesses();

            // Desaturation URP
            if (_colorAdjustments != null)
                _colorAdjustments.saturation.Override(MONTRE_SATURATION);

            _coroutineMontre = StartCoroutine(EffetMontre());
        }

        private IEnumerator EffetMontre()
        {
            yield return new WaitForSeconds(MONTRE_DUREE);
            TerminerMontre();
        }

        private void TerminerMontre()
        {
            _montreActive = false;

            // Retirer le facteur global : tous les UpdateSpeed futurs reviennent a la normale.
            ScrollingElement.FacteurVitesseGlobal = 1f;
            GoundMouvement.FacteurVitesseGlobal   = 1f;

            // Forcer le recalcul immediat sur tous les objets actifs.
            spawner?.RefreshVitesses();

            // Restaurer les vitesses des sols depuis la sauvegarde.
            if (grounds != null && _vitessesOriginalesSols != null)
            {
                for (int i = 0; i < grounds.Length && i < _vitessesOriginalesSols.Length; i++)
                    if (grounds[i] != null)
                        grounds[i].speed = _vitessesOriginalesSols[i];
            }

            // Restaurer la saturation
            if (_colorAdjustments != null)
                _colorAdjustments.saturation.Override(0f);

            _coroutineMontre = null;
        }

        /// <summary>Retourne true si l'effet Montre a Gousset est actuellement actif.</summary>
        public bool EstMontreActive() => _montreActive;

        // ── Arret d'urgence ───────────────────────────────────────────────────

        /// <summary>
        /// Annule tous les effets actifs de type route. A appeler depuis SegmentBarrage
        /// ou SessionManager avant la transition vers la scene Barrage.
        /// </summary>
        public void AnnulerTousLesEffetsRoute()
        {
            if (_coroutineTirelire  != null) { StopCoroutine(_coroutineTirelire);  _coroutineTirelire  = null; }
            if (_coroutineGateau    != null) { StopCoroutine(_coroutineGateau);    _coroutineGateau    = null; }
            if (_coroutineRadar     != null) { StopCoroutine(_coroutineRadar);     _coroutineRadar     = null; }
            if (_coroutineOnde      != null) { StopCoroutine(_coroutineOnde);      _coroutineOnde      = null; }
            if (_coroutineAspirateur!= null) { StopCoroutine(_coroutineAspirateur);_coroutineAspirateur= null; }
            if (_coroutineMontre    != null) { StopCoroutine(_coroutineMontre);    _coroutineMontre    = null; }

            TerminerTirelire();
            TerminerGateau();
            TerminerRadar();
            TerminerAspirateur();
            TerminerMontre();

            Debug.Log("[EffetsObjetsSpeciaux] Tous les effets de route annules (transition barrage).");
        }
    }
}
