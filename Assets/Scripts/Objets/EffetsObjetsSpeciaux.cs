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
        // Points de patience accordés directement (indépendant de patienceDepart).
        private const float LIASSE_RATIO_PATIENCE = 0.50f;
        private const float LIASSE_PATIENCE_POINTS = 80f; // 50 % de la valeur par défaut (160)

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

        // ── Identifiants effets (clés des événements) ─────────────────────────
        private const string ID_TIRELIRE   = "TirelireCochon";
        private const string ID_GATEAU     = "GateauChinois";
        private const string ID_RADAR      = "RadarObstacles";
        private const string ID_ASPIRATEUR = "Aspirateur";
        private const string ID_MONTRE     = "MontreAGousset";

        // ── Couleurs par effet (partagées avec EffetsDureeUI) ─────────────────
        public static readonly Color CouleurTirelire   = new Color(1.00f, 0.82f, 0.10f, 1f);
        public static readonly Color CouleurGateau     = new Color(1.00f, 0.25f, 0.15f, 1f);
        public static readonly Color CouleurRadar      = new Color(0.20f, 0.85f, 1.00f, 1f);
        public static readonly Color CouleurAspirateur = new Color(0.30f, 1.00f, 0.45f, 1f);
        public static readonly Color CouleurMontre     = new Color(0.70f, 0.70f, 1.00f, 1f);

        // ── Événements (abonnés par EffetsDureeUI) ────────────────────────────
        /// <summary>Déclenché quand un effet temporel démarre. (id, durée, couleur, nomAffichage)</summary>
        public event System.Action<string, float, Color, string> OnEffetDemarre;
        /// <summary>Déclenché quand un effet arrive à son terme naturellement.</summary>
        public event System.Action<string> OnEffetTermine;
        /// <summary>Déclenché quand un effet est interrompu par l'activation d'un autre effet.</summary>
        public event System.Action<string> OnEffetAnnule;
        /// <summary>Déclenché quand le Gâteau Chinois esquive un obstacle.</summary>
        public event System.Action OnEsquiveDeclenchee;

        // ── Dependances : Barrage (objets passifs) ────────────────────────────
        [Header("Barrage -- Objets Passifs")]
        [SerializeField] private BarrePatience barrePatience;
        [SerializeField] private MainDuGardeUI mainDuGarde;
        [SerializeField] private ListeAttenteGarde listeAttenteGarde;

        [Tooltip("SO_PlayerDatas — consomme l'objet spécial (réduit sa quantité de 1) quand il est utilisé au barrage.")]
        [SerializeField] private SO_PlayerDatas donneesJoueur;

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
            if (volumePostProcess == null)
            {
                Debug.LogWarning("[EffetsObjetsSpeciaux] volumePostProcess non assigne -- effet Montre (desaturation) desactive.");
                return;
            }

            VolumeProfile profil = volumePostProcess.sharedProfile != null
                ? volumePostProcess.sharedProfile
                : volumePostProcess.profile;

            if (profil == null)
            {
                Debug.LogWarning("[EffetsObjetsSpeciaux] Volume sans profil -- effet Montre (desaturation) desactive.");
                return;
            }

            if (!profil.TryGet(out _colorAdjustments))
            {
                // The profile has no Color Adjustments override yet -- add one at runtime.
                _colorAdjustments = profil.Add<ColorAdjustments>(overrides: false);
                Debug.Log("[EffetsObjetsSpeciaux] Color Adjustments ajouté au profil PP_MontreDesaturation.");
            }

            // Always ensure the saturation override flag is on -- the asset may have it off.
            _colorAdjustments.saturation.overrideState = true;
            _colorAdjustments.saturation.value = 0f;
        }

        private void Start()
        {
            // Avertissements pour les assets visuels optionnels non assignes.
            if (spriteAspirateurVoiture == null)
                Debug.LogWarning("[EffetsObjetsSpeciaux] spriteAspirateurVoiture non assigne -- l'effet Aspirateur ne changera pas le sprite de la voiture.");
            if (spriteExclamation == null)
                Debug.LogWarning("[EffetsObjetsSpeciaux] spriteExclamation non assigne -- le Radar affichera un carre rouge de fallback.");

            CreerHalosSiAbsents();
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

            ConsommerObjet("LiasseDeBillets");
            barrePatience.AjouterPatience(LIASSE_PATIENCE_POINTS);
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

            ConsommerObjet("FormulairePasePartout");
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
            {
                ConsommerObjet("BadgeDuGouvernement");
                Debug.Log("[EffetsObjetsSpeciaux] Badge du gouvernement -- barrage valide instantanement.");
            }
        }

        // ── Consommation ──────────────────────────────────────────────────────

        /// <summary>
        /// Réduit de 1 la quantité de l'objet identifié dans SO_PlayerDatas et sauvegarde.
        /// Ne descend jamais en dessous de 0.
        /// </summary>
        private void ConsommerObjet(string identifiant)
        {
            if (donneesJoueur == null)
            {
                Debug.LogWarning($"[EffetsObjetsSpeciaux] donneesJoueur non assigné — '{identifiant}' non consommé.");
                return;
            }

            foreach (InventoryObject groupe in donneesJoueur.allObjectInInventory)
            {
                foreach (InventoryEntry entree in groupe.highScores)
                {
                    if (entree.objectName == identifiant && entree.quantity > 0)
                    {
                        entree.quantity--;
                        donneesJoueur.SaveDatas();
                        Debug.Log($"[EffetsObjetsSpeciaux] '{identifiant}' consommé. Restant : {entree.quantity}");
                        return;
                    }
                }
            }

            Debug.LogWarning($"[EffetsObjetsSpeciaux] ConsommerObjet — '{identifiant}' introuvable ou quantité déjà à 0.");
        }

        // ── Garde-fou route ───────────────────────────────────────────────────

        /// <summary>
        /// Returns true when the active scene is a road scene (MapRoad or MapTuto).
        /// Road-only effects must not run in barrage scenes, where spawner/grounds/joueur are null.
        /// Also ensures the global static speed factors are clean on barrage entry.
        /// </summary>
        private bool EstEnSceneRoute()
        {
            string nom = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            return nom == "MapRoad" || nom == "MapTuto";
        }

        // ── Objet 4 : Tirelire Cochon (actif, route) ──────────────────────────

        /// <summary>Declenche l'effet Tirelire Cochon pendant TIRELIRE_DUREE secondes.</summary>
        public void UtiliserTirelireCochon()
        {
            if (!EstEnSceneRoute())
            {
                Debug.LogWarning("[EffetsObjetsSpeciaux] TirelireCochon ignoré hors scène route.");
                return;
            }
            AnnulerEffetsActifsHormisCelui(ID_TIRELIRE);

            if (_coroutineTirelire != null)
                StopCoroutine(_coroutineTirelire);

            _tirelireActive        = true;
            _tirelireMultiplicateur = TIRELIRE_MULTIPLICATEUR_PIECES;

            if (haloDoree != null)
                haloDoree.SetActive(true);

            _coroutineTirelire = StartCoroutine(EffetTirelire());
            OnEffetDemarre?.Invoke(ID_TIRELIRE, TIRELIRE_DUREE, CouleurTirelire, "Tirelire");
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

            OnEffetTermine?.Invoke(ID_TIRELIRE);
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
            if (!EstEnSceneRoute())
            {
                Debug.LogWarning("[EffetsObjetsSpeciaux] GateauChinois ignoré hors scène route.");
                return;
            }
            AnnulerEffetsActifsHormisCelui(ID_GATEAU);

            if (_coroutineGateau != null)
                StopCoroutine(_coroutineGateau);

            _gateauActif = true;

            if (haloRouge != null)
                haloRouge.SetActive(true);

            _coroutineGateau = StartCoroutine(EffetGateau());
            OnEffetDemarre?.Invoke(ID_GATEAU, GATEAU_DUREE, CouleurGateau, "Gâteau");
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

            OnEffetTermine?.Invoke(ID_GATEAU);
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
                StartCoroutine(FlashEsquive());
                OnEsquiveDeclenchee?.Invoke();
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
                yield return new WaitForSecondsRealtime(GATEAU_BLINK_INTERVALLE);
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
            if (!EstEnSceneRoute())
            {
                Debug.LogWarning("[EffetsObjetsSpeciaux] RadarObstacles ignoré hors scène route.");
                return;
            }
            AnnulerEffetsActifsHormisCelui(ID_RADAR);

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
            OnEffetDemarre?.Invoke(ID_RADAR, RADAR_DUREE, CouleurRadar, "Radar");
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

            OnEffetTermine?.Invoke(ID_RADAR);
            _coroutineRadar = null;
        }

        private IEnumerator AnimerOndeRadar()
        {
            SpriteRenderer sr = ondeRadar != null ? ondeRadar.GetComponent<SpriteRenderer>() : null;
            // La scale de base est (tailleUnites, tailleUnites, 1) définie à la création (1,1,1).
            // On anime de 0 → RADAR_ONDE_SCALE_MAX en local scale pour que l'anneau s'étende.
            float baseScale = ondeRadar != null ? ondeRadar.transform.localScale.x : 1f;

            while (_radarActif)
            {
                float t = 0f;
                while (t < RADAR_ONDE_DUREE_SCALE)
                {
                    t += Time.deltaTime;
                    float ratio = Mathf.Clamp01(t / RADAR_ONDE_DUREE_SCALE);
                    float scale = Mathf.Lerp(0f, RADAR_ONDE_SCALE_MAX * baseScale, ratio);
                    float alpha = Mathf.Lerp(0.6f, 0f, ratio);

                    if (ondeRadar != null)
                        ondeRadar.transform.localScale = new Vector3(scale, scale, 1f);

                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = alpha;
                        sr.color = c;
                    }

                    yield return null;
                }

                // Remettre à l'échelle de base entre deux pulses
                if (ondeRadar != null)
                    ondeRadar.transform.localScale = new Vector3(baseScale, baseScale, 1f);

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
            if (!EstEnSceneRoute())
            {
                Debug.LogWarning("[EffetsObjetsSpeciaux] Aspirateur ignoré hors scène route.");
                return;
            }
            AnnulerEffetsActifsHormisCelui(ID_ASPIRATEUR);

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
            OnEffetDemarre?.Invoke(ID_ASPIRATEUR, ASPIRATEUR_DUREE, CouleurAspirateur, "Aspirateur");
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

            OnEffetTermine?.Invoke(ID_ASPIRATEUR);
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
            if (!EstEnSceneRoute())
            {
                Debug.LogWarning("[EffetsObjetsSpeciaux] MontreAGousset ignoré hors scène route.");
                return;
            }
            AnnulerEffetsActifsHormisCelui(ID_MONTRE);

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
            OnEffetDemarre?.Invoke(ID_MONTRE, MONTRE_DUREE, CouleurMontre, "Montre");
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

            OnEffetTermine?.Invoke(ID_MONTRE);
            _coroutineMontre = null;
        }

        /// <summary>Retourne true si l'effet Montre a Gousset est actuellement actif.</summary>
        public bool EstMontreActive() => _montreActive;

        // ── Arret d'urgence ───────────────────────────────────────────────────

        /// <summary>
        /// Annule tous les effets actifs à durée sauf celui identifié par <paramref name="idAConserver"/>.
        /// Appelé en tête de chaque méthode UtiliserXxx pour garantir qu'un seul effet
        /// temporel est actif à la fois, avec nettoyage propre (visuels + UI).
        /// </summary>
        private void AnnulerEffetsActifsHormisCelui(string idAConserver)
        {
            // OnEffetAnnule fires BEFORE TerminerXxx so EffetsDureeUI can set _timerValide = false
            // before the subsequent OnEffetTermine (fired inside TerminerXxx) is processed.
            // This lets the flash-cancel coroutine own the arc cleanup without interference.
            if (idAConserver != ID_TIRELIRE   && _coroutineTirelire   != null) { OnEffetAnnule?.Invoke(ID_TIRELIRE);   StopCoroutine(_coroutineTirelire);   TerminerTirelire(); }
            if (idAConserver != ID_GATEAU     && _coroutineGateau     != null) { OnEffetAnnule?.Invoke(ID_GATEAU);     StopCoroutine(_coroutineGateau);     TerminerGateau(); }
            if (idAConserver != ID_RADAR      && _coroutineRadar      != null) { OnEffetAnnule?.Invoke(ID_RADAR);      StopCoroutine(_coroutineRadar);      TerminerRadar(); }
            if (idAConserver != ID_ASPIRATEUR && _coroutineAspirateur != null) { OnEffetAnnule?.Invoke(ID_ASPIRATEUR); StopCoroutine(_coroutineAspirateur); TerminerAspirateur(); }
            if (idAConserver != ID_MONTRE     && _coroutineMontre     != null) { OnEffetAnnule?.Invoke(ID_MONTRE);     StopCoroutine(_coroutineMontre);     TerminerMontre(); }
        }

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

        // ── Génération des halos programmatiques ──────────────────────────────

        private void CreerHalosSiAbsents()
        {
            if (spriteVoiture == null) return;

            if (haloDoree == null)
                haloDoree = CreerObjetHalo("HaloDoree",
                    GenererTextureGlow(new Color(1f, 0.85f, 0.1f, 0.70f), 128), tailleUnites: 3.0f);

            if (haloRouge == null)
                haloRouge = CreerObjetHalo("HaloRouge",
                    GenererTextureGlow(new Color(1f, 0.20f, 0.10f, 0.65f), 128), tailleUnites: 3.0f);

            if (ondeRadar == null)
                ondeRadar = CreerObjetHalo("OndeRadar",
                    GenererTextureAnneau(new Color(0.2f, 0.85f, 1f, 0.85f), 128, epaisseur: 0.12f), tailleUnites: 1.0f);
        }

        /// <summary>Crée un GameObject avec SpriteRenderer parented à la RACINE du joueur (scale 1),
        /// désactivé. tailleUnites = diamètre voulu en unités monde.</summary>
        private GameObject CreerObjetHalo(string nom, Texture2D texture, float tailleUnites)
        {
            // pixelsPerUnit tel que le sprite fasse 1 unité monde,
            // puis on règle localScale pour atteindre tailleUnites.
            const float PPU = 100f;
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                PPU);

            // Parent = racine du joueur (scale 1,1,1) pour ne pas hériter de l'échelle de SpritePlayer.
            Transform parentTransform = spriteVoiture.transform.parent != null
                ? spriteVoiture.transform.parent
                : spriteVoiture.transform;

            GameObject go          = new GameObject(nom);
            go.transform.SetParent(parentTransform, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;

            // 1 unité monde = PPU pixels → tailleUnites unités monde.
            float s = tailleUnites;
            go.transform.localScale = new Vector3(s, s, 1f);

            SpriteRenderer sr      = go.AddComponent<SpriteRenderer>();
            sr.sprite              = sprite;
            sr.sortingLayerName    = spriteVoiture.sortingLayerName;
            sr.sortingOrder        = spriteVoiture.sortingOrder - 1;

            go.SetActive(false);
            return go;
        }

        private static Texture2D GenererTextureGlow(Color couleur, int resolution)
        {
            Texture2D tex    = new Texture2D(resolution, resolution, TextureFormat.RGBA32, mipChain: false);
            Vector2   centre = new Vector2(resolution * 0.5f, resolution * 0.5f);
            float     rayon  = resolution * 0.5f;

            for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
            {
                float t     = Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), centre) / rayon);
                float alpha = Mathf.Pow(1f - t, 2.2f) * couleur.a;
                tex.SetPixel(x, y, new Color(couleur.r, couleur.g, couleur.b, alpha));
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D GenererTextureAnneau(Color couleur, int resolution, float epaisseur)
        {
            Texture2D tex    = new Texture2D(resolution, resolution, TextureFormat.RGBA32, mipChain: false);
            Vector2   centre = new Vector2(resolution * 0.5f, resolution * 0.5f);
            float     rayon  = resolution * 0.5f;
            float     bande  = rayon * epaisseur;

            for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
            {
                float dist  = Vector2.Distance(new Vector2(x, y), centre);
                float inner = rayon - bande;
                float tIn   = Mathf.Clamp01((dist  - inner) / (bande * 0.35f));
                float tOut  = Mathf.Clamp01((rayon - dist)  / (bande * 0.35f));
                float alpha = Mathf.Min(tIn, tOut) * couleur.a;
                tex.SetPixel(x, y, new Color(couleur.r, couleur.g, couleur.b, alpha));
            }

            tex.Apply();
            return tex;
        }

        // ── Flash visuel esquive Gâteau Chinois ───────────────────────────────

        private IEnumerator FlashEsquive()
        {
            const float DUREE     = 0.28f;
            const float ALPHA_MAX = 0.55f;

            GameObject canvasGo   = new GameObject("FlashEsquive");
            Canvas canvas         = canvasGo.AddComponent<Canvas>();
            canvas.renderMode     = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder   = 99;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();

            GameObject imgGo              = new GameObject("Fond");
            imgGo.transform.SetParent(canvasGo.transform, false);
            UnityEngine.UI.Image img      = imgGo.AddComponent<UnityEngine.UI.Image>();
            img.color                     = new Color(1f, 0.92f, 0.1f, ALPHA_MAX);
            img.raycastTarget             = false;

            RectTransform rt  = imgGo.GetComponent<RectTransform>();
            rt.anchorMin      = Vector2.zero;
            rt.anchorMax      = Vector2.one;
            rt.sizeDelta      = Vector2.zero;

            float elapsed = 0f;
            while (elapsed < DUREE)
            {
                elapsed  += Time.unscaledDeltaTime;
                img.color = new Color(1f, 0.92f, 0.1f,
                    Mathf.Lerp(ALPHA_MAX, 0f, elapsed / DUREE));
                yield return null;
            }

            Destroy(canvasGo);
        }
    }
}
