using UnityEngine;

namespace Barrage.Effets
{
    /// <summary>Palette de couleurs prédéfinie pour l'effet de poussière.</summary>
    public enum PalettePoussière
    {
        /// <summary>Sable chaud — tons beiges et bruns. Convient aux pistes en terre.</summary>
        Sable,
        /// <summary>Goudron — gris foncés et cendres. Convient aux routes asphaltées.</summary>
        Goudron,
        /// <summary>Couleurs définies manuellement via les champs ci-dessous.</summary>
        Personnalisée,
    }

    /// <summary>
    /// Paramètres ajustables depuis l'Inspector pour un seul système de particules.
    /// Tous les champs sont en unités brutes — les multiplicateurs globaux s'appliquent par-dessus.
    /// </summary>
    [System.Serializable]
    public sealed class ParamètresPS
    {
        [Tooltip("Active ou désactive ce système. Un système désactivé n'émet aucune particule.")]
        public bool actif = true;

        [Tooltip("Particules émises par seconde (ignoré pour les Débris qui utilisent des bursts).")]
        [Min(0f)] public float tauxEmission = 10f;

        [Tooltip("Taille minimale à la naissance (unités monde × échelle globale).")]
        [Min(0.001f)] public float tailleMin = 0.10f;

        [Tooltip("Taille maximale à la naissance (unités monde × échelle globale).")]
        [Min(0.001f)] public float tailleMax = 0.22f;

        [Tooltip("Durée de vie minimale en secondes.")]
        [Min(0.01f)] public float duréeMin = 0.5f;

        [Tooltip("Durée de vie maximale en secondes.")]
        [Min(0.01f)] public float duréeMax = 1.0f;

        [Tooltip("Vitesse d'éjection minimale (unités monde/s × échelle globale).")]
        [Min(0f)] public float vitesseMin = 0.5f;

        [Tooltip("Vitesse d'éjection maximale (unités monde/s × échelle globale).")]
        [Min(0f)] public float vitesseMax = 2.0f;
    }

    /// <summary>
    /// Effet procédural de poussière de pneu — 5 systèmes de particules superposés :
    ///   1. <b>Nuage principal</b>    — grand volume de poussière billowing, bruit Perlin.
    ///   2. <b>Grains de sable</b>   — éjection rapide de grains fins à la sortie du pneu.
    ///   3. <b>Débris / graviers</b> — projections balistiques avec arc gravitationnel.
    ///   4. <b>Traînée longue</b>    — nappe de poussière très transparente qui persiste au sol.
    ///   5. <b>Contact sol</b>       — puff radial instantané au point de contact pneu/sol.
    ///
    /// Placement : axe local +Z de ce GameObject doit pointer vers l'avant du véhicule.
    /// La poussière est éjectée vers -Z (derrière) et se simule en World Space.
    /// Pilotez <see cref="VitesseVehicule"/> depuis votre contrôleur de véhicule.
    /// </summary>
    [AddComponentMenu("FX/Poussière Voiture")]
    public class PoussiereVoiture : MonoBehaviour
    {
        // ── Paramètres véhicule ───────────────────────────────────────────────
        [Header("Véhicule")]
        [Tooltip("Vitesse normalisée [0-1]. 0 = arrêt, 1 = pleine vitesse.\n" +
                 "Modifiable en temps réel via la propriété publique VitesseVehicule.")]
        [SerializeField, Range(0f, 1f)] private float vitesseVehicule = 1f;

        [Tooltip("Échelle globale de l'effet. 1 = voiture de taille standard (~1.5 m de large).\n" +
                 "Affecte les rayons d'émission, les vitesses et la dispersion. Redémarrer le Play pour prendre effet.")]
        [SerializeField, Min(0.05f)] private float echelle = 1f;

        [Tooltip("Multiplicateur de taille des particules à la naissance, indépendant de l'échelle.\n" +
                 "Modifiable en temps réel : 0.5 = moitié, 2 = double.")]
        [SerializeField, Min(0.01f)] private float tailleMultiplicateur = 1f;

        [Tooltip("Contrôle jusqu'où les particules grandissent pendant leur durée de vie.\n" +
                 "1 = comportement par défaut  /  0.3 = croissance très contenue  /  2 = croissance exagérée.\n" +
                 "Agit sur le multiplicateur de la courbe SizeOverLifetime de chaque système.")]
        [SerializeField, Min(0.01f)] private float facteurCroissanceParticules = 1f;

        [Tooltip("Direction verticale des particules en world space.\n" +
                 "  > 0  → les particules montent   (ex : poussière qui s'élève)\n" +
                 "  = 0  → comportement par défaut (direction neutre)\n" +
                 "  < 0  → les particules descendent (ex : gravats qui retombent)\n" +
                 "Modifiable en temps réel.")]
        [SerializeField, Range(-10f, 10f)] private float directionVerticale = 0f;

        [Tooltip("Multiplicateur de vitesse d'éjection initiale des particules.\n" +
                 "1 = vitesse par défaut  /  0.5 = diffusion lente  /  2 = diffusion rapide.\n" +
                 "Modifiable en temps réel.")]
        [SerializeField, Min(0.01f)] private float vitesseDiffusion = 1f;

        [Tooltip("Multiplicateur de durée de vie des particules.\n" +
                 "1 = durée par défaut  /  0.5 = durée deux fois plus courte  /  2 = durée deux fois plus longue.\n" +
                 "Modifiable en temps réel.")]
        [SerializeField, Min(0.01f)] private float duréeVieParticules = 1f;

        [Tooltip("Multiplicateur de taille appliqué uniquement à la traînée longue (PS_TrainéeLongue).\n" +
                 "S'ajoute au Taille Multiplicateur global : taille effective = tailleMultiplicateur × tailleTrainee.\n" +
                 "0.5 = traînée deux fois plus fine  /  3 = nappe très large.\n" +
                 "Modifiable en temps réel.")]
        [SerializeField, Min(0.01f)] private float tailleTrainee = 1f;

        [Tooltip("Cocher pour les véhicules 'inversés' dont la direction visuelle est opposée au sens normal.\n" +
                 "Les particules spawneront à distance du point d'émission et convergeront VERS lui,\n" +
                 "démarrant petites et grossissant en approchant — donnant l'illusion d'une traînée\n" +
                 "qui remonte vers les roues.  Modifiable en temps réel.")]
        [SerializeField] private bool estInversé = false;

        [Tooltip("Rayon de spawn (unités monde × échelle) : distance à laquelle les particules\n" +
                 "apparaissent autour du point d'émission quand estInversé est actif.\n" +
                 "Modifiable en temps réel.")]
        [SerializeField, Min(0.01f)] private float distanceSpawnInversé = 0.35f;

        [Tooltip("Vitesse de convergence radiale vers le point d'émission (unités monde/s).\n" +
                 "Plus la valeur est élevée, plus les particules arrivent vite au centre.\n" +
                 "Modifiable en temps réel.")]
        [SerializeField, Min(0.01f)] private float vitesseAttraction = 1.5f;

        [Tooltip("GameObject cible : les particules convergeront vers sa position en world space.\n" +
                 "Mettre un GameObject vide dans le prefab du véhicule et l'assigner ici.\n" +
                 "La direction est recalculée chaque frame — aucun décalage quelle que soit la vitesse du jeu.\n" +
                 "Si laissé vide, fallback sur la convergence radiale locale.")]
        [SerializeField] private Transform cible;

        // ── Couleurs ──────────────────────────────────────────────────────────
        [Header("Palette")]
        [Tooltip("Sable     → beiges et bruns chauds (piste en terre).\n" +
                 "Goudron   → gris foncés et cendres (route asphaltée).\n" +
                 "Personnalisée → utilise les trois champs couleur ci-dessous.")]
        [SerializeField] private PalettePoussière palette = PalettePoussière.Sable;

        [Tooltip("Teinte principale (nuage, grains, sol).\n" +
                 "Ignorée si la palette n'est pas 'Personnalisée'.")]
        [SerializeField] private Color couleurSable   = new Color(0.82f, 0.65f, 0.44f, 1f);

        [Tooltip("Teinte secondaire (cendre, traînée).\n" +
                 "Ignorée si la palette n'est pas 'Personnalisée'.")]
        [SerializeField] private Color couleurCendre  = new Color(0.68f, 0.64f, 0.56f, 1f);

        [Tooltip("Teinte des débris / graviers.\n" +
                 "Ignorée si la palette n'est pas 'Personnalisée'.")]
        [SerializeField] private Color couleurDebris  = new Color(0.35f, 0.27f, 0.19f, 1f);

        // ── Rendu ─────────────────────────────────────────────────────────────
        [Header("Rendu")]
        [Tooltip("Sorting layer appliqué à tous les renderers de particules.\n" +
                 "Doit correspondre exactement à un nom de sorting layer du projet\n" +
                 "(Project Settings → Tags and Layers → Sorting Layers).")]
        [SerializeField] private string coucheTri = "Décor";

        [Tooltip("Ordre de rendu au sein de la couche de tri.\n" +
                 "Plus la valeur est élevée, plus les particules apparaissent en avant-plan.\n" +
                 "Modifiable en temps réel.")]
        [SerializeField] private int ordreTri = 0;

        // ── Systèmes de particules individuels ───────────────────────────────
        [Header("1 · Nuage Principal")]
        [SerializeField] private ParamètresPS nuage = new ParamètresPS
        {
            actif = true, tauxEmission = 18f,
            tailleMin = 0.10f, tailleMax = 0.22f,
            duréeMin = 0.5f,  duréeMax = 1.0f,
            vitesseMin = 0.7f, vitesseMax = 2.0f,
        };

        [Header("2 · Grains de Sable")]
        [SerializeField] private ParamètresPS sable = new ParamètresPS
        {
            actif = true, tauxEmission = 65f,
            tailleMin = 0.014f, tailleMax = 0.052f,
            duréeMin = 0.20f,   duréeMax = 0.65f,
            vitesseMin = 2.8f,  vitesseMax = 6.5f,
        };

        [Header("3 · Débris / Graviers")]
        [SerializeField] private ParamètresPS débris = new ParamètresPS
        {
            actif = true, tauxEmission = 0f,
            tailleMin = 0.036f, tailleMax = 0.105f,
            duréeMin = 0.55f,   duréeMax = 1.40f,
            vitesseMin = 1.8f,  vitesseMax = 5.0f,
        };

        [Tooltip("Nombre minimal de graviers par burst.")]
        [SerializeField, Min(1f)] private float débrisBurstMin = 1f;

        [Tooltip("Nombre maximal de graviers par burst.")]
        [SerializeField, Min(1f)] private float débrisBurstMax = 4f;

        [Tooltip("Intervalle entre chaque burst (secondes).")]
        [SerializeField, Min(0.05f)] private float débrisBurstIntervalle = 0.35f;

        [Header("4 · Traînée Longue")]
        [SerializeField] private ParamètresPS traînée = new ParamètresPS
        {
            actif = true, tauxEmission = 6f,
            tailleMin = 0.20f, tailleMax = 0.45f,
            duréeMin = 0.6f,  duréeMax = 1.2f,
            vitesseMin = 0.0f, vitesseMax = 0.18f,
        };

        [Header("5 · Contact Sol")]
        [SerializeField] private ParamètresPS sol = new ParamètresPS
        {
            actif = true, tauxEmission = 45f,
            tailleMin = 0.05f, tailleMax = 0.18f,
            duréeMin = 0.12f,  duréeMax = 0.45f,
            vitesseMin = 0.9f, vitesseMax = 2.8f,
        };

        // ── État interne ──────────────────────────────────────────────────────
        private ParticleSystem _psNuage;
        private ParticleSystem _psSable;
        private ParticleSystem _psDebris;
        private ParticleSystem _psTrainee;
        private ParticleSystem _psSol;

        // Renderers stockés pour AppliquerTri() — évite GetComponent à chaque OnValidate.
        private ParticleSystemRenderer[] _renderers;

        // Multiplicateurs de base de la courbe SizeOverLifetime (capturés après Config).
        // AppliquerCroissance() les multiplie par facteurCroissanceParticules.
        private float _baseCroissanceNuage;
        private float _baseCroissanceSable;
        private float _baseCroissanceDebris;
        private float _baseCroissanceTrainee;
        private float _baseCroissanceSol;

        // Vélocité Y de base du nuage (montée organique issue de ConfigNuage).
        // AppliquerDirection() y additionne directionVerticale.
        private float _baseVelYNuage;

        private Material _matPoussiere;
        private Material _matDebris;

        private static readonly int ID_Surface   = Shader.PropertyToID("_Surface");
        private static readonly int ID_Blend     = Shader.PropertyToID("_Blend");
        private static readonly int ID_ZWrite    = Shader.PropertyToID("_ZWrite");
        private static readonly int ID_BaseColor = Shader.PropertyToID("_BaseColor");

        private const string SHADER_URP_PARTICLES = "Universal Render Pipeline/Particles/Unlit";

        // ── API publique ──────────────────────────────────────────────────────

        /// <summary>
        /// Vitesse normalisée du véhicule [0-1].
        /// Moduler cette valeur depuis le contrôleur de véhicule selon la vitesse réelle.
        /// </summary>
        public float VitesseVehicule
        {
            get => vitesseVehicule;
            set
            {
                vitesseVehicule = Mathf.Clamp01(value);
                AppliquerVitesse();
            }
        }

        /// <summary>
        /// Multiplicateur de taille des particules, modifiable à la volée.
        /// 1 = taille par défaut, 0.5 = moitié, 2 = double.
        /// </summary>
        public float TailleMultiplicateur
        {
            get => tailleMultiplicateur;
            set
            {
                tailleMultiplicateur = Mathf.Max(0.01f, value);
                AppliquerTaille();
            }
        }

        /// <summary>
        /// Facteur de croissance des particules sur leur durée de vie, modifiable à la volée.
        /// 1 = comportement par défaut, valeurs inférieures limitent l'expansion.
        /// </summary>
        public float FacteurCroissanceParticules
        {
            get => facteurCroissanceParticules;
            set
            {
                facteurCroissanceParticules = Mathf.Max(0.01f, value);
                AppliquerCroissance();
            }
        }

        /// <summary>
        /// Direction verticale des particules en world space.
        /// Positif = vers le haut, négatif = vers le bas.
        /// </summary>
        public float DirectionVerticale
        {
            get => directionVerticale;
            set
            {
                directionVerticale = value;
                AppliquerDirection();
            }
        }

        /// <summary>
        /// Multiplicateur de vitesse d'éjection initiale [0-N].
        /// 1 = vitesse par défaut, 0.5 = lent, 2 = rapide.
        /// </summary>
        public float VitesseDiffusion
        {
            get => vitesseDiffusion;
            set
            {
                vitesseDiffusion = Mathf.Max(0.01f, value);
                AppliquerVitesseDiffusion();
            }
        }

        /// <summary>
        /// Multiplicateur de durée de vie des particules [0-N].
        /// 1 = durée par défaut, 0.5 = deux fois plus courte.
        /// </summary>
        public float DuréeVieParticules
        {
            get => duréeVieParticules;
            set
            {
                duréeVieParticules = Mathf.Max(0.01f, value);
                AppliquerDuréeVie();
            }
        }

        /// <summary>
        /// Multiplicateur de taille de la traînée longue uniquement.
        /// S'applique en plus de <see cref="TailleMultiplicateur"/>.
        /// </summary>
        public float TailleTrainee
        {
            get => tailleTrainee;
            set
            {
                tailleTrainee = Mathf.Max(0.01f, value);
                AppliquerTailleTrainee();
            }
        }

        /// <summary>
        /// Active le mode inversé : les particules convergent vers <see cref="cible"/>
        /// (ou vers le pivot local si cible est null) depuis une sphère de rayon
        /// <see cref="distanceSpawnInversé"/>, en grossissant.
        /// </summary>
        public bool EstInversé
        {
            get => estInversé;
            set
            {
                estInversé = value;
                AppliquerDirection();
                AppliquerModeAttiré();
            }
        }

        /// <summary>
        /// Cible vers laquelle les particules convergent quand <see cref="estInversé"/> est actif.
        /// La direction est recalculée chaque frame dans Update — synchronisée avec la vitesse du jeu.
        /// </summary>
        public Transform Cible
        {
            get => cible;
            set => cible = value;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            BuildMaterials();
            _psNuage   = BuildSous("PS_NuagePrincipal", ConfigNuage,   _matPoussiere);
            _psSable   = BuildSous("PS_GrainsSable",    ConfigSable,   _matPoussiere);
            _psDebris  = BuildSous("PS_Débris",         ConfigDébris,  _matDebris);
            _psTrainee = BuildSous("PS_TrainéeLongue",  ConfigTrainée, _matPoussiere);
            _psSol     = BuildSous("PS_ContactSol",     ConfigSol,     _matPoussiere);

            _renderers = new ParticleSystemRenderer[]
            {
                _psNuage.GetComponent<ParticleSystemRenderer>(),
                _psSable.GetComponent<ParticleSystemRenderer>(),
                _psDebris.GetComponent<ParticleSystemRenderer>(),
                _psTrainee.GetComponent<ParticleSystemRenderer>(),
                _psSol.GetComponent<ParticleSystemRenderer>(),
            };

            // Capturer les multiplicateurs SizeOverLifetime de base (pour facteurCroissanceParticules)
            _baseCroissanceNuage   = _psNuage.sizeOverLifetime.sizeMultiplier;
            _baseCroissanceSable   = _psSable.sizeOverLifetime.sizeMultiplier;
            _baseCroissanceDebris  = _psDebris.sizeOverLifetime.sizeMultiplier;
            _baseCroissanceTrainee = _psTrainee.sizeOverLifetime.sizeMultiplier;
            _baseCroissanceSol     = _psSol.sizeOverLifetime.sizeMultiplier;

            // Capturer la vélocité Y de base du nuage (montée organique définie dans ConfigNuage)
            _baseVelYNuage = _psNuage.velocityOverLifetime.yMultiplier;

            AppliquerActif();
            AppliquerVitesse();
            AppliquerTaille();
            AppliquerCroissance();
            AppliquerDirection();
            AppliquerVitesseDiffusion();
            AppliquerDuréeVie();
            AppliquerTailleTrainee();
            AppliquerEmission();
            AppliquerTri();
            AppliquerCouleurs();
            AppliquerModeAttiré(); // doit rester en dernier — écrase forme et vélocité si estInversé
        }

        private void OnValidate()
        {
            AppliquerActif();
            AppliquerVitesse();
            AppliquerTaille();
            AppliquerCroissance();
            AppliquerDirection();
            AppliquerVitesseDiffusion();
            AppliquerDuréeVie();
            AppliquerTailleTrainee();
            AppliquerEmission();
            AppliquerTri();
            AppliquerCouleurs();
            AppliquerModeAttiré(); // doit rester en dernier
        }

        private void Update()
        {
            if (!estInversé || cible == null || _psNuage == null) return;

            // Direction world-space recalculée chaque frame — suit la cible quelles que soient
            // les variations de vitesse du jeu ou le mouvement du véhicule.
            Vector3 delta = cible.position - transform.position;
            float   dist  = delta.magnitude;
            if (dist < 0.001f) return;

            Vector3 dir = delta / dist; // normalisation sans allocation
            float   v   = vitesseAttraction;

            SetVelocitéVersCible(_psNuage,   dir, v * 1.0f);
            SetVelocitéVersCible(_psSable,   dir, v * 1.4f);
            SetVelocitéVersCible(_psDebris,  dir, v * 1.0f);
            SetVelocitéVersCible(_psTrainee, dir, v * 0.7f);
            SetVelocitéVersCible(_psSol,     dir, v * 1.6f);
        }

        private static void SetVelocitéVersCible(ParticleSystem ps, Vector3 dir, float vitesse)
        {
            if (ps == null) return;
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space   = ParticleSystemSimulationSpace.World;
            vel.radial  = new ParticleSystem.MinMaxCurve(0f);
            vel.x       = new ParticleSystem.MinMaxCurve(dir.x * vitesse);
            vel.y       = new ParticleSystem.MinMaxCurve(dir.y * vitesse);
            vel.z       = new ParticleSystem.MinMaxCurve(dir.z * vitesse);
        }

        // ── Matériaux URP Particles/Unlit transparents ────────────────────────

        private void BuildMaterials()
        {
            _matPoussiere = CréerMatTransparent(couleurSable,  "FX_Mat_Poussiere");
            _matDebris    = CréerMatTransparent(couleurDebris, "FX_Mat_Debris");
        }

        private static Material CréerMatTransparent(Color couleur, string nom)
        {
            var shader = Shader.Find(SHADER_URP_PARTICLES);
            if (shader == null)
            {
                Debug.LogError("[PoussiereVoiture] Shader URP Particles/Unlit introuvable. " +
                               "Vérifiez que le projet est bien configuré en URP.");
                shader = Shader.Find("Particles/Standard Unlit");
            }

            var mat = new Material(shader) { name = nom };
            mat.SetFloat(ID_Surface, 1f);   // 1 = Transparent
            mat.SetFloat(ID_Blend,   0f);   // 0 = Alpha blend
            mat.SetFloat(ID_ZWrite,  0f);
            mat.SetColor(ID_BaseColor, couleur);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            return mat;
        }

        // ── Factory de sous-systèmes ───────────────────────────────────────────

        private ParticleSystem BuildSous(string nom, System.Action<ParticleSystem> config, Material mat)
        {
            var go  = new GameObject(nom);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;

            var ps  = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial  = mat;
            psr.renderMode      = ParticleSystemRenderMode.Billboard;
            psr.maxParticleSize = 8f;   // évite le clipping de grosses particules à l'écran
            psr.sortingFudge    = 0f;

            config(ps);
            return ps;
        }

        // ── Modulation de l'émission par la vitesse ───────────────────────────

        private void AppliquerVitesse()
        {
            if (_psNuage == null) return;
            float v = vitesseVehicule;
            SetRate(_psNuage,   v * nuage.tauxEmission);
            SetRate(_psSable,   v * sable.tauxEmission);
            SetRate(_psDebris,  0f);                      // débris = bursts uniquement
            SetRate(_psTrainee, v * traînée.tauxEmission);
            SetRate(_psSol,     v * sol.tauxEmission);
        }

        /// <summary>Affecte le taux d'émission continu d'un système.</summary>
        private static void SetRate(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            var em = ps.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(rate);
        }

        // ── Palette de couleurs ───────────────────────────────────────────────

        // Presets Sable
        private static readonly Color K_SABLE_PRINCIPALE = new Color(0.84f, 0.67f, 0.44f);
        private static readonly Color K_SABLE_SECONDAIRE = new Color(0.76f, 0.72f, 0.63f);
        private static readonly Color K_SABLE_DEBRIS     = new Color(0.35f, 0.27f, 0.19f);
        private static readonly Color K_SABLE_VAPEUR     = new Color(0.92f, 0.89f, 0.85f);

        // Presets Goudron — asphalte quasi-noir, sous-ton brun chaud caractéristique du bitume
        private static readonly Color K_GOUDRON_PRINCIPALE = new Color(0.08f, 0.07f, 0.06f); // bitume frais — quasi-noir brun
        private static readonly Color K_GOUDRON_SECONDAIRE = new Color(0.24f, 0.21f, 0.17f); // poussière d'asphalte usé — brun foncé
        private static readonly Color K_GOUDRON_DEBRIS     = new Color(0.05f, 0.04f, 0.03f); // gravillon goudronné — noir profond
        private static readonly Color K_GOUDRON_VAPEUR     = new Color(0.32f, 0.28f, 0.23f); // fumée/vapeur chaude de bitume — gris brun

        /// <summary>
        /// Résout les couleurs actives selon la <see cref="palette"/> sélectionnée,
        /// puis les applique à <c>main.startColor</c> et <c>colorOverLifetime</c>
        /// sur chacun des 5 systèmes de particules.
        /// </summary>
        private void AppliquerCouleurs()
        {
            // ── 1. Résolution de la palette ───────────────────────────────────
            // Exécutée en Edit Mode comme en Play Mode (pas de dépendance aux PS).
            Color principale, secondaire, debrisCol, vapeur;

            switch (palette)
            {
                case PalettePoussière.Goudron:
                    principale = K_GOUDRON_PRINCIPALE;
                    secondaire = K_GOUDRON_SECONDAIRE;
                    debrisCol  = K_GOUDRON_DEBRIS;
                    vapeur     = K_GOUDRON_VAPEUR;
                    break;

                case PalettePoussière.Sable:
                    principale = K_SABLE_PRINCIPALE;
                    secondaire = K_SABLE_SECONDAIRE;
                    debrisCol  = K_SABLE_DEBRIS;
                    vapeur     = K_SABLE_VAPEUR;
                    break;

                default: // Personnalisée
                    principale = couleurSable;
                    secondaire = couleurCendre;
                    debrisCol  = couleurDebris;
                    vapeur     = Color.Lerp(secondaire, Color.white, 0.55f);
                    break;
            }

            // ── 2. Synchronisation des champs Inspector ───────────────────────
            // Visible immédiatement dans l'Inspector, même hors Play Mode.
            if (palette != PalettePoussière.Personnalisée)
            {
                couleurSable  = principale;
                couleurCendre = secondaire;
                couleurDebris = debrisCol;
            }

            // ── 3. Application aux systèmes de particules ─────────────────────
            // Les PS n'existent qu'au runtime (créés dans Awake) : sortie anticipée en Edit Mode.
            if (_psNuage == null) return;

            // Mettre à jour la teinte de base des matériaux partagés
            if (_matPoussiere != null)
                _matPoussiere.color = new Color(principale.r, principale.g, principale.b, 1f);
            if (_matDebris != null)
                _matDebris.color = new Color(debrisCol.r, debrisCol.g, debrisCol.b, 1f);

            // startColor
            SetStartColor(_psNuage,   principale, secondaire);
            SetStartColor(_psSable,   principale, Color.Lerp(principale, secondaire, 0.5f));
            SetStartColor(_psDebris,  debrisCol,  Color.Lerp(debrisCol, Color.black, 0.3f));
            SetStartColor(_psTrainee, Color.Lerp(principale, vapeur, 0.3f), Color.Lerp(secondaire, vapeur, 0.3f));
            SetStartColor(_psSol,     principale, Color.Lerp(principale, secondaire, 0.55f));

            // colorOverLifetime
            SetColorOverLifetime(_psNuage,
                new GradientColorKey[]
                {
                    new GradientColorKey(principale,                                0.00f),
                    new GradientColorKey(Color.Lerp(principale, secondaire, 0.55f), 0.38f),
                    new GradientColorKey(vapeur,                                    1.00f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.00f, 0.000f),
                    new GradientAlphaKey(0.62f, 0.090f),
                    new GradientAlphaKey(0.48f, 0.420f),
                    new GradientAlphaKey(0.20f, 0.750f),
                    new GradientAlphaKey(0.00f, 1.000f),
                });

            SetColorOverLifetime(_psSable,
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.Lerp(principale, Color.white, 0.18f), 0.0f),
                    new GradientColorKey(secondaire,                                  1.0f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.90f, 0.00f),
                    new GradientAlphaKey(0.55f, 0.40f),
                    new GradientAlphaKey(0.00f, 1.00f),
                });

            SetColorOverLifetime(_psDebris,
                new GradientColorKey[]
                {
                    new GradientColorKey(debrisCol,                                  0.00f),
                    new GradientColorKey(debrisCol,                                  0.70f),
                    new GradientColorKey(Color.Lerp(debrisCol, Color.black, 0.4f),   1.00f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1.00f, 0.00f),
                    new GradientAlphaKey(0.92f, 0.65f),
                    new GradientAlphaKey(0.00f, 1.00f),
                });

            SetColorOverLifetime(_psTrainee,
                new GradientColorKey[]
                {
                    new GradientColorKey(principale,                             0.00f),
                    new GradientColorKey(Color.Lerp(principale, vapeur, 0.45f),  0.40f),
                    new GradientColorKey(vapeur,                                 1.00f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.00f, 0.000f),
                    new GradientAlphaKey(0.22f, 0.055f),
                    new GradientAlphaKey(0.17f, 0.500f),
                    new GradientAlphaKey(0.00f, 1.000f),
                });

            SetColorOverLifetime(_psSol,
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.Lerp(principale, Color.white, 0.12f), 0.0f),
                    new GradientColorKey(Color.Lerp(principale, secondaire, 0.65f),  1.0f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.78f, 0.00f),
                    new GradientAlphaKey(0.35f, 0.45f),
                    new GradientAlphaKey(0.00f, 1.00f),
                });
        }

        private static void SetStartColor(ParticleSystem ps, Color min, Color max)
        {
            if (ps == null) return;
            var m = ps.main;
            m.startColor = new ParticleSystem.MinMaxGradient(min, max);
        }

        private static void SetColorOverLifetime(
            ParticleSystem   ps,
            GradientColorKey[] colorKeys,
            GradientAlphaKey[] alphaKeys)
        {
            if (ps == null) return;
            var g = new Gradient();
            g.SetKeys(colorKeys, alphaKeys);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color   = new ParticleSystem.MinMaxGradient(g);
        }

        // ── Modulation de la taille des particules ────────────────────────────

        /// <summary>
        /// Applique <see cref="tailleMultiplicateur"/> × les valeurs min/max de chaque struct
        /// sur startSize des 5 systèmes. Les structs sont la source de vérité pour la taille de base.
        /// </summary>
        private void AppliquerTaille()
        {
            if (_psNuage == null) return;
            SetTaillePS(_psNuage,   nuage,   tailleMultiplicateur);
            SetTaillePS(_psSable,   sable,   tailleMultiplicateur);
            SetTaillePS(_psDebris,  débris,  tailleMultiplicateur);
            SetTaillePS(_psTrainee, traînée, tailleMultiplicateur * tailleTrainee);
            SetTaillePS(_psSol,     sol,     tailleMultiplicateur);
        }

        private void SetTaillePS(ParticleSystem ps, ParamètresPS p, float mult)
        {
            if (ps == null) return;
            var m = ps.main;
            m.startSize = new ParticleSystem.MinMaxCurve(
                p.tailleMin * echelle * mult,
                p.tailleMax * echelle * mult
            );
        }

        // ── Taille individuelle de la traînée ─────────────────────────────────

        /// <summary>
        /// Applique <see cref="tailleTrainee"/> uniquement sur PS_TrainéeLongue.
        /// Résultat : traînée.tailleMin/Max × échelle × tailleMultiplicateur × tailleTrainee.
        /// </summary>
        private void AppliquerTailleTrainee()
        {
            if (_psTrainee == null) return;
            SetTaillePS(_psTrainee, traînée, tailleMultiplicateur * tailleTrainee);
        }

        // ── Modulation de la croissance des particules ────────────────────────

        /// <summary>
        /// Applique <see cref="facteurCroissanceParticules"/> sur la courbe SizeOverLifetime
        /// des 5 systèmes. Un facteur de 0.3 contient très fortement l'expansion des sprites ;
        /// 1 = comportement par défaut issu des courbes de configuration.
        /// </summary>
        private void AppliquerCroissance()
        {
            if (_psNuage == null) return;
            AppliquerCroissancePS(_psNuage,   _baseCroissanceNuage);
            AppliquerCroissancePS(_psSable,   _baseCroissanceSable);
            AppliquerCroissancePS(_psDebris,  _baseCroissanceDebris);
            AppliquerCroissancePS(_psTrainee, _baseCroissanceTrainee);
            AppliquerCroissancePS(_psSol,     _baseCroissanceSol);
        }

        private void AppliquerCroissancePS(ParticleSystem ps, float baseCroissance)
        {
            if (ps == null) return;
            var sol = ps.sizeOverLifetime;
            sol.sizeMultiplier = baseCroissance * facteurCroissanceParticules;
        }

        // ── Mode inversé : attraction radiale vers le point d'émission ─────────

        /// <summary>
        /// Quand <see cref="estInversé"/> est actif, transforme tous les systèmes en mode "attiré" :
        /// les particules naissent en sphère autour du pivot et convergent vers lui en grossissant.
        /// Appelé EN DERNIER dans Awake et OnValidate pour écraser forme et vélocité.
        /// </summary>
        private void AppliquerModeAttiré()
        {
            if (_psNuage == null || !estInversé) return;

            float rayon = distanceSpawnInversé * echelle;
            float attr  = vitesseAttraction;

            AppliquerAttiréPS(_psNuage,   rayon,         attr * 1.0f);
            AppliquerAttiréPS(_psSable,   rayon * 0.65f, attr * 1.4f);
            AppliquerAttiréPS(_psDebris,  rayon * 0.80f, attr * 1.0f);
            AppliquerAttiréPS(_psTrainee, rayon * 1.30f, attr * 0.7f);
            AppliquerAttiréPS(_psSol,     rayon * 0.45f, attr * 1.6f);
        }

        private void AppliquerAttiréPS(ParticleSystem ps, float rayon, float attraction)
        {
            if (ps == null) return;

            // Spawn en coquille sphérique autour du point d'émission
            var shape = ps.shape;
            shape.shapeType       = ParticleSystemShapeType.Sphere;
            shape.radius          = rayon;
            shape.radiusThickness = 0f;   // surface uniquement
            shape.rotation        = Vector3.zero;

            // Vitesse initiale nulle — la vélocité (radiale ou directionnelle) prend le relais
            var m = ps.main;
            m.startSpeed = new ParticleSystem.MinMaxCurve(0f);

            // ── Vélocité ──────────────────────────────────────────────────────
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;

            if (cible != null)
            {
                // Cible assignée : Update() recalcule la direction chaque frame en world space.
                // On initialise à zéro — Update prend le relais immédiatement.
                vel.space  = ParticleSystemSimulationSpace.World;
                vel.radial = new ParticleSystem.MinMaxCurve(0f);
                vel.x      = new ParticleSystem.MinMaxCurve(0f);
                vel.y      = new ParticleSystem.MinMaxCurve(0f);
                vel.z      = new ParticleSystem.MinMaxCurve(0f);
            }
            else
            {
                // Fallback : convergence radiale locale (comportement sans cible)
                vel.space  = ParticleSystemSimulationSpace.Local;
                vel.radial = new ParticleSystem.MinMaxCurve(-attraction);
                vel.x      = new ParticleSystem.MinMaxCurve(0f);
                vel.y      = new ParticleSystem.MinMaxCurve(0f);
                vel.z      = new ParticleSystem.MinMaxCurve(0f);
            }

            // Taille : naît très petite, grossit en approchant la cible
            var szL = ps.sizeOverLifetime;
            szL.enabled = true;
            szL.size    = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0.00f, 0.05f, 3.0f, 3.0f),
                new Keyframe(0.40f, 0.55f, 1.0f, 1.0f),
                new Keyframe(1.00f, 1.00f, 0.0f, 0.0f)
            ));
        }

        // ── Direction verticale ───────────────────────────────────────────────

        /// <summary>
        /// Injecte <see cref="directionVerticale"/> dans velocityOverLifetime.y de chaque système.
        /// Le nuage conserve sa montée organique de base à laquelle la valeur s'additionne.
        /// Ignoré quand <see cref="estInversé"/> est actif (AppliquerModeAttiré prend le relais).
        /// </summary>
        private void AppliquerDirection()
        {
            if (_psNuage == null || estInversé) return;

            SetVelocityY(_psNuage,   _baseVelYNuage + directionVerticale);
            SetVelocityY(_psSable,   directionVerticale);
            SetVelocityY(_psDebris,  directionVerticale);
            SetVelocityY(_psTrainee, directionVerticale);
            SetVelocityY(_psSol,     directionVerticale);
        }

        private static void SetVelocityY(ParticleSystem ps, float y)
        {
            if (ps == null) return;
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space   = ParticleSystemSimulationSpace.World;
            // Conserver X et Z existants, ne modifier que Y
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(y,  y);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        }

        // ── Vitesse d'éjection ────────────────────────────────────────────────

        /// <summary>
        /// Applique <see cref="vitesseDiffusion"/> × les vitesses min/max de chaque struct
        /// sur startSpeed des 5 systèmes.
        /// </summary>
        private void AppliquerVitesseDiffusion()
        {
            if (_psNuage == null) return;
            SetVitessePS(_psNuage,   nuage,   vitesseDiffusion);
            SetVitessePS(_psSable,   sable,   vitesseDiffusion);
            SetVitessePS(_psDebris,  débris,  vitesseDiffusion);
            SetVitessePS(_psTrainee, traînée, vitesseDiffusion);
            SetVitessePS(_psSol,     sol,     vitesseDiffusion);
        }

        private void SetVitessePS(ParticleSystem ps, ParamètresPS p, float mult)
        {
            if (ps == null) return;
            var m = ps.main;
            m.startSpeed = new ParticleSystem.MinMaxCurve(
                p.vitesseMin * echelle * mult,
                p.vitesseMax * echelle * mult
            );
        }

        // ── Durée de vie ──────────────────────────────────────────────────────

        /// <summary>
        /// Applique <see cref="duréeVieParticules"/> × les durées min/max de chaque struct
        /// sur startLifetime des 5 systèmes.
        /// </summary>
        private void AppliquerDuréeVie()
        {
            if (_psNuage == null) return;
            SetDuréePS(_psNuage,   nuage,   duréeVieParticules);
            SetDuréePS(_psSable,   sable,   duréeVieParticules);
            SetDuréePS(_psDebris,  débris,  duréeVieParticules);
            SetDuréePS(_psTrainee, traînée, duréeVieParticules);
            SetDuréePS(_psSol,     sol,     duréeVieParticules);
        }

        private static void SetDuréePS(ParticleSystem ps, ParamètresPS p, float mult)
        {
            if (ps == null) return;
            var m = ps.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(
                p.duréeMin * mult,
                p.duréeMax * mult
            );
        }

        // ── Activation / désactivation ────────────────────────────────────────

        /// <summary>
        /// Active ou désactive chaque système de particules selon le champ <c>actif</c> de son struct.
        /// </summary>
        private void AppliquerActif()
        {
            SetActifPS(_psNuage,   nuage.actif);
            SetActifPS(_psSable,   sable.actif);
            SetActifPS(_psDebris,  débris.actif);
            SetActifPS(_psTrainee, traînée.actif);
            SetActifPS(_psSol,     sol.actif);
        }

        private static void SetActifPS(ParticleSystem ps, bool actif)
        {
            if (ps == null) return;
            if (ps.gameObject.activeSelf == actif) return;
            ps.gameObject.SetActive(actif);
            if (actif) ps.Play();
        }

        // ── Emission / bursts ─────────────────────────────────────────────────

        /// <summary>
        /// Applique les paramètres de burst des débris (min/max/intervalle) depuis l'Inspector.
        /// </summary>
        private void AppliquerEmission()
        {
            if (_psDebris == null) return;
            var em = _psDebris.emission;
            em.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(
                    0f,
                    new ParticleSystem.MinMaxCurve(débrisBurstMin, débrisBurstMax),
                    0,
                    débrisBurstIntervalle)
            });
        }

        // ── Couche et ordre de rendu ──────────────────────────────────────────

        /// <summary>
        /// Applique <see cref="coucheTri"/> et <see cref="ordreTri"/> à tous les renderers.
        /// Modifiable en temps réel depuis l'Inspector.
        /// </summary>
        private void AppliquerTri()
        {
            if (_renderers == null) return;
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                r.sortingLayerName = coucheTri;
                r.sortingOrder     = ordreTri;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CONFIGURATIONS DÉTAILLÉES PAR SYSTÈME
        // ══════════════════════════════════════════════════════════════════════

        // ── 1. NUAGE PRINCIPAL ────────────────────────────────────────────────
        //     Petit puff compact derrière le pneu, s'estompe rapidement.
        private void ConfigNuage(ParticleSystem ps)
        {
            float e = echelle;

            var main = ps.main;
            main.loop             = true;
            main.simulationSpace  = ParticleSystemSimulationSpace.World;
            main.startLifetime    = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
            main.startSpeed       = new ParticleSystem.MinMaxCurve(0.7f * e, 2.0f * e);
            main.startSize        = new ParticleSystem.MinMaxCurve(0.10f * e, 0.22f * e);
            main.startRotation    = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.startColor       = new ParticleSystem.MinMaxGradient(couleurSable, couleurCendre);
            main.gravityModifier  = new ParticleSystem.MinMaxCurve(0.025f);
            main.maxParticles     = 80;

            var em = ps.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(18f);

            // Cône large ouvert vers -Z (derrière le véhicule)
            var shape = ps.shape;
            shape.enabled           = true;
            shape.shapeType         = ParticleSystemShapeType.Cone;
            shape.angle             = 35f;
            shape.radius            = 0.10f * e;
            shape.radiusThickness   = 1f;
            shape.rotation          = new Vector3(90f, 0f, 0f); // +Y local → -Z local

            // Apparaît vite, s'efface très rapidement
            var colL = ps.colorOverLifetime;
            colL.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.84f, 0.67f, 0.44f), 0.00f),
                    new GradientColorKey(new Color(0.76f, 0.72f, 0.63f), 0.38f),
                    new GradientColorKey(new Color(0.92f, 0.89f, 0.85f), 1.00f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.00f, 0.000f),
                    new GradientAlphaKey(0.55f, 0.060f),  // pic rapide
                    new GradientAlphaKey(0.20f, 0.350f),  // chute brusque
                    new GradientAlphaKey(0.00f, 0.650f),  // transparent à 65 % de la vie
                }
            );
            colL.color = new ParticleSystem.MinMaxGradient(g);

            // Croissance modérée — reste compact
            var szL = ps.sizeOverLifetime;
            szL.enabled = true;
            szL.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0.00f, 0.20f, 2.5f,  2.5f),
                new Keyframe(0.30f, 1.00f, 0.6f,  0.6f),
                new Keyframe(1.00f, 1.50f, 0.0f,  0.0f)   // ×1.5 max au lieu de ×3.4
            ));

            // Rotation propre des particules (tumble)
            var rotL = ps.rotationOverLifetime;
            rotL.enabled = true;
            rotL.z = new ParticleSystem.MinMaxCurve(-65f * Mathf.Deg2Rad, 65f * Mathf.Deg2Rad);

            // Bruit de Perlin — mouvement organique non-uniforme
            var noise = ps.noise;
            noise.enabled     = true;
            noise.strength    = new ParticleSystem.MinMaxCurve(0.30f * e);
            noise.frequency   = 0.55f;
            noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.20f);
            noise.octaveCount = 2;
            noise.quality     = ParticleSystemNoiseQuality.Low;

            // Résistance de l'air — ralentit les particules avec le temps
            var limV = ps.limitVelocityOverLifetime;
            limV.enabled  = true;
            limV.space    = ParticleSystemSimulationSpace.World;
            limV.limit    = new ParticleSystem.MinMaxCurve(2.0f * e);
            limV.dampen   = 0.12f;

            // Montée lente en World space — la poussière s'élève
            var velL = ps.velocityOverLifetime;
            velL.enabled = true;
            velL.space   = ParticleSystemSimulationSpace.World;
            velL.x       = new ParticleSystem.MinMaxCurve(0f, 0f);
            velL.y       = new ParticleSystem.MinMaxCurve(0.08f, 0.30f);
            velL.z       = new ParticleSystem.MinMaxCurve(0f, 0f);
        }

        // ── 2. GRAINS DE SABLE ────────────────────────────────────────────────
        //     Éjection rapide et dense, courte durée, arc balistique.
        private void ConfigSable(ParticleSystem ps)
        {
            float e = echelle;

            var main = ps.main;
            main.loop             = true;
            main.simulationSpace  = ParticleSystemSimulationSpace.World;
            main.startLifetime    = new ParticleSystem.MinMaxCurve(0.20f, 0.65f);
            main.startSpeed       = new ParticleSystem.MinMaxCurve(2.8f * e, 6.5f * e);
            main.startSize        = new ParticleSystem.MinMaxCurve(0.014f * e, 0.052f * e);
            main.startRotation    = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.startColor       = new ParticleSystem.MinMaxGradient(
                new Color(0.92f, 0.78f, 0.54f),
                couleurSable
            );
            main.gravityModifier  = new ParticleSystem.MinMaxCurve(0.42f);
            main.maxParticles     = 700;

            var em = ps.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(65f);

            // Cône étroit = éjection concentrée vers l'arrière
            var shape = ps.shape;
            shape.enabled         = true;
            shape.shapeType       = ParticleSystemShapeType.Cone;
            shape.angle           = 28f;
            shape.radius          = 0.13f * e;  // ~largeur du pneu
            shape.radiusThickness = 0f;         // depuis la surface externe (bord du pneu)
            shape.rotation        = new Vector3(90f, 0f, 0f);

            var colL = ps.colorOverLifetime;
            colL.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.93f, 0.77f, 0.52f), 0.0f),
                    new GradientColorKey(new Color(0.84f, 0.77f, 0.63f), 1.0f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.90f, 0.00f),
                    new GradientAlphaKey(0.55f, 0.40f),
                    new GradientAlphaKey(0.00f, 1.00f),
                }
            );
            colL.color = new ParticleSystem.MinMaxGradient(g);

            // Rétrécit rapidement (illusion de grains qui s'éloignent)
            var szL = ps.sizeOverLifetime;
            szL.enabled = true;
            szL.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0.0f, 1.0f),
                new Keyframe(0.6f, 0.45f),
                new Keyframe(1.0f, 0.0f)
            ));

            // Léger bruit directionnel pour dispersion non-uniforme
            var noise = ps.noise;
            noise.enabled   = true;
            noise.strength  = new ParticleSystem.MinMaxCurve(0.20f * e);
            noise.frequency = 3.0f;
            noise.quality   = ParticleSystemNoiseQuality.Low;
        }

        // ── 3. DÉBRIS / GRAVIERS ──────────────────────────────────────────────
        //     Projections balistiques, arc gravitationnel prononcé, bursts aléatoires.
        private void ConfigDébris(ParticleSystem ps)
        {
            float e = echelle;

            var main = ps.main;
            main.loop             = true;
            main.simulationSpace  = ParticleSystemSimulationSpace.World;
            main.startLifetime    = new ParticleSystem.MinMaxCurve(0.55f, 1.40f);
            main.startSpeed       = new ParticleSystem.MinMaxCurve(1.8f * e, 5.0f * e);
            main.startSize        = new ParticleSystem.MinMaxCurve(0.036f * e, 0.105f * e);
            main.startRotation    = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.startColor       = new ParticleSystem.MinMaxGradient(
                couleurDebris,
                new Color(0.26f, 0.20f, 0.14f)
            );
            main.gravityModifier  = new ParticleSystem.MinMaxCurve(1.40f); // arc balistique prononcé
            main.maxParticles     = 100;

            var em = ps.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
            // Burst répété : 1-4 cailloux éjectés toutes les 0.35 s
            em.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, new ParticleSystem.MinMaxCurve(1f, 4f), 0, 0.35f)
            });

            // Cône étroit incliné vers haut-arrière = trajectoire qui remonte puis retombe
            var shape = ps.shape;
            shape.enabled         = true;
            shape.shapeType       = ParticleSystemShapeType.Cone;
            shape.angle           = 18f;
            shape.radius          = 0.065f * e;
            shape.radiusThickness = 1f;
            shape.rotation        = new Vector3(55f, 0f, 0f); // vers haut-arrière

            var colL = ps.colorOverLifetime;
            colL.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(couleurDebris,                       0.00f),
                    new GradientColorKey(couleurDebris,                       0.70f),
                    new GradientColorKey(new Color(0.17f, 0.13f, 0.09f), 1.00f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1.00f, 0.00f),
                    new GradientAlphaKey(0.92f, 0.65f),
                    new GradientAlphaKey(0.00f, 1.00f),
                }
            );
            colL.color = new ParticleSystem.MinMaxGradient(g);

            // Conserve sa taille puis disparaît d'un coup (caillou qui tombe)
            var szL = ps.sizeOverLifetime;
            szL.enabled = true;
            szL.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0.00f, 1.0f),
                new Keyframe(0.80f, 0.9f),
                new Keyframe(1.00f, 0.0f)
            ));

            // Rotation rapide (cailloux qui tournent en vol)
            var rotL = ps.rotationOverLifetime;
            rotL.enabled = true;
            rotL.z = new ParticleSystem.MinMaxCurve(-320f * Mathf.Deg2Rad, 320f * Mathf.Deg2Rad);

            // Assigner le matériau sombre
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = _matDebris;
        }

        // ── 4. TRAÎNÉE LONGUE ─────────────────────────────────────────────────
        //     Fine nappe rase, courte durée, s'estompe vite.
        private void ConfigTrainée(ParticleSystem ps)
        {
            float e = echelle;

            var main = ps.main;
            main.loop             = true;
            main.simulationSpace  = ParticleSystemSimulationSpace.World;
            main.startLifetime    = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);  // 5-8 s → 0.6-1.2 s
            main.startSpeed       = new ParticleSystem.MinMaxCurve(0.0f, 0.18f * e);
            main.startSize        = new ParticleSystem.MinMaxCurve(0.20f * e, 0.45f * e);
            main.startRotation    = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.startColor       = new ParticleSystem.MinMaxGradient(
                new Color(0.93f, 0.86f, 0.70f, 0.18f),
                new Color(0.87f, 0.80f, 0.66f, 0.25f)
            );
            main.gravityModifier  = new ParticleSystem.MinMaxCurve(0.0f);
            main.maxParticles     = 40;  // 180 → 40

            var em = ps.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(6f);

            // Boîte plate au ras du sol (couche de poussière)
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale     = new Vector3(0.24f * e, 0.02f, 0.20f * e);

            // Apparaît brièvement puis s'évanouit rapidement
            var colL = ps.colorOverLifetime;
            colL.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.92f, 0.83f, 0.67f), 0.00f),
                    new GradientColorKey(new Color(0.95f, 0.91f, 0.83f), 0.35f),
                    new GradientColorKey(new Color(0.97f, 0.95f, 0.93f), 1.00f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.00f, 0.000f),
                    new GradientAlphaKey(0.20f, 0.050f),  // pic d'opacité bas dès le départ
                    new GradientAlphaKey(0.08f, 0.350f),  // chute rapide
                    new GradientAlphaKey(0.00f, 0.600f),  // invisible à 60 % de la vie
                }
            );
            colL.color = new ParticleSystem.MinMaxGradient(g);

            // Légère expansion — reste petite, ne grandit pas démesurément
            var szL = ps.sizeOverLifetime;
            szL.enabled = true;
            szL.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0.00f, 0.25f),
                new Keyframe(0.25f, 1.00f),
                new Keyframe(1.00f, 2.00f)   // ×2 max au lieu de ×6
            ));

            // Bruit lent = dispersion organique et naturelle
            var noise = ps.noise;
            noise.enabled     = true;
            noise.strength    = new ParticleSystem.MinMaxCurve(0.10f * e);
            noise.frequency   = 0.20f;
            noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.035f);
            noise.octaveCount = 2;
            noise.quality     = ParticleSystemNoiseQuality.Low;

            // Très légère rotation (traînée qui se dissout dans l'air)
            var rotL = ps.rotationOverLifetime;
            rotL.enabled = true;
            rotL.z = new ParticleSystem.MinMaxCurve(-16f * Mathf.Deg2Rad, 16f * Mathf.Deg2Rad);
        }

        // ── 5. CONTACT SOL ────────────────────────────────────────────────────
        //     Puff radial court à l'interface pneu/sol, éjection centrifuge.
        private void ConfigSol(ParticleSystem ps)
        {
            float e = echelle;

            var main = ps.main;
            main.loop             = true;
            main.simulationSpace  = ParticleSystemSimulationSpace.World;
            main.startLifetime    = new ParticleSystem.MinMaxCurve(0.12f, 0.45f);
            main.startSpeed       = new ParticleSystem.MinMaxCurve(0.9f * e, 2.8f * e);
            main.startSize        = new ParticleSystem.MinMaxCurve(0.05f * e, 0.18f * e);
            main.startRotation    = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.startColor       = new ParticleSystem.MinMaxGradient(
                couleurSable,
                new Color(0.70f, 0.62f, 0.47f)
            );
            main.gravityModifier  = new ParticleSystem.MinMaxCurve(0.08f);
            main.maxParticles     = 450;

            var em = ps.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(45f);

            // Disque horizontal au sol — éjection depuis le bord du cercle
            var shape = ps.shape;
            shape.enabled          = true;
            shape.shapeType        = ParticleSystemShapeType.Circle;
            shape.radius           = 0.14f * e;
            shape.radiusThickness  = 0f;        // émettre depuis le bord = effet centrifuge
            shape.rotation         = new Vector3(90f, 0f, 0f); // disque à plat au sol

            var colL = ps.colorOverLifetime;
            colL.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.90f, 0.74f, 0.50f), 0.0f),
                    new GradientColorKey(new Color(0.82f, 0.76f, 0.63f), 1.0f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.78f, 0.00f),
                    new GradientAlphaKey(0.35f, 0.45f),
                    new GradientAlphaKey(0.00f, 1.00f),
                }
            );
            colL.color = new ParticleSystem.MinMaxGradient(g);

            // Grossit puis disparaît (puff qui s'évapore)
            var szL = ps.sizeOverLifetime;
            szL.enabled = true;
            szL.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0.0f, 0.35f),
                new Keyframe(0.5f, 1.00f),
                new Keyframe(1.0f, 0.00f)
            ));
        }
    }
}
