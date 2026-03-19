using UnityEngine;

namespace Barrage.Effets
{
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

        // ── Couleurs ──────────────────────────────────────────────────────────
        [Header("Palette")]
        [Tooltip("Teinte principale de la poussière (sable chaud).")]
        [SerializeField] private Color couleurSable   = new Color(0.82f, 0.65f, 0.44f, 1f);
        [Tooltip("Teinte secondaire (cendre froide).")]
        [SerializeField] private Color couleurCendre  = new Color(0.68f, 0.64f, 0.56f, 1f);
        [Tooltip("Teinte des débris (graviers sombres).")]
        [SerializeField] private Color couleurDebris  = new Color(0.35f, 0.27f, 0.19f, 1f);

        // ── Rendu ─────────────────────────────────────────────────────────────
        [Header("Rendu")]
        [Tooltip("Sorting layer appliqué à tous les renderers de particules.\n" +
                 "Doit correspondre exactement à un nom de sorting layer du projet\n" +
                 "(Project Settings → Tags and Layers → Sorting Layers).")]
        [SerializeField] private string coucheTri = "Player";

        [Tooltip("Ordre de rendu au sein de la couche de tri.\n" +
                 "Plus la valeur est élevée, plus les particules apparaissent en avant-plan.\n" +
                 "Modifiable en temps réel.")]
        [SerializeField] private int ordreTri = 0;

        // ── État interne ──────────────────────────────────────────────────────
        private ParticleSystem _psNuage;
        private ParticleSystem _psSable;
        private ParticleSystem _psDebris;
        private ParticleSystem _psTrainee;
        private ParticleSystem _psSol;

        // Renderers stockés pour AppliquerTri() — évite GetComponent à chaque OnValidate.
        private ParticleSystemRenderer[] _renderers;

        // Tailles de base capturées après la configuration initiale (en unités monde * echelle).
        // AppliquerTaille() les multiplie par tailleMultiplicateur pour un ajustement temps réel.
        private float _baseTailleNuage;
        private float _baseTailleSable;
        private float _baseTailleDebris;
        private float _baseTailleTrainee;
        private float _baseTailleSol;

        // Multiplicateurs de base de la courbe SizeOverLifetime (= 1f pour une courbe normalisée).
        // AppliquerCroissance() les multiplie par facteurCroissanceParticules.
        private float _baseCroissanceNuage;
        private float _baseCroissanceSable;
        private float _baseCroissanceDebris;
        private float _baseCroissanceTrainee;
        private float _baseCroissanceSol;

        // Multiplicateurs de vitesse de base (startSpeedMultiplier issu de la config).
        private float _baseVitesseNuage;
        private float _baseVitesseSable;
        private float _baseVitesseDebris;
        private float _baseVitesseTrainee;
        private float _baseVitesseSol;

        // Multiplicateurs de durée de vie de base (startLifetimeMultiplier issu de la config).
        private float _baseDuréeNuage;
        private float _baseDuréeSable;
        private float _baseDuréeDebris;
        private float _baseDuréeTrainee;
        private float _baseDuréeSol;

        // Vélocité Y de base définie dans velocityOverLifetime (midpoint entre min et max).
        // AppliquerDirection() ajoute directionVerticale en world space par-dessus.
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

            // Capturer les tailles de base issues de la configuration (avec echelle appliquée)
            _baseTailleNuage   = _psNuage.main.startSizeMultiplier;
            _baseTailleSable   = _psSable.main.startSizeMultiplier;
            _baseTailleDebris  = _psDebris.main.startSizeMultiplier;
            _baseTailleTrainee = _psTrainee.main.startSizeMultiplier;
            _baseTailleSol     = _psSol.main.startSizeMultiplier;

            // Capturer les multiplicateurs SizeOverLifetime de base
            _baseCroissanceNuage   = _psNuage.sizeOverLifetime.sizeMultiplier;
            _baseCroissanceSable   = _psSable.sizeOverLifetime.sizeMultiplier;
            _baseCroissanceDebris  = _psDebris.sizeOverLifetime.sizeMultiplier;
            _baseCroissanceTrainee = _psTrainee.sizeOverLifetime.sizeMultiplier;
            _baseCroissanceSol     = _psSol.sizeOverLifetime.sizeMultiplier;

            // Capturer les vitesses d'éjection de base
            _baseVitesseNuage   = _psNuage.main.startSpeedMultiplier;
            _baseVitesseSable   = _psSable.main.startSpeedMultiplier;
            _baseVitesseDebris  = _psDebris.main.startSpeedMultiplier;
            _baseVitesseTrainee = _psTrainee.main.startSpeedMultiplier;
            _baseVitesseSol     = _psSol.main.startSpeedMultiplier;

            // Capturer les durées de vie de base
            _baseDuréeNuage   = _psNuage.main.startLifetimeMultiplier;
            _baseDuréeSable   = _psSable.main.startLifetimeMultiplier;
            _baseDuréeDebris  = _psDebris.main.startLifetimeMultiplier;
            _baseDuréeTrainee = _psTrainee.main.startLifetimeMultiplier;
            _baseDuréeSol     = _psSol.main.startLifetimeMultiplier;

            // Capturer la vélocité Y de base du nuage (seul système avec velocityOverLifetime.y != 0)
            _baseVelYNuage = _psNuage.velocityOverLifetime.yMultiplier;

            AppliquerVitesse();
            AppliquerTaille();
            AppliquerCroissance();
            AppliquerDirection();
            AppliquerVitesseDiffusion();
            AppliquerDuréeVie();
            AppliquerTri();
        }

        private void OnValidate()
        {
            AppliquerVitesse();
            AppliquerTaille();
            AppliquerCroissance();
            AppliquerDirection();
            AppliquerVitesseDiffusion();
            AppliquerDuréeVie();
            AppliquerTri();
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
            SetRate(_psNuage,   v * 22f);
            SetRate(_psSable,   v * 65f);
            SetRate(_psDebris,  v *  0f);   // débris uniquement par bursts
            SetRate(_psTrainee, v *  6f);
            SetRate(_psSol,     v * 45f);
        }

        /// <summary>Affecte le taux d'émission continu d'un système.</summary>
        private static void SetRate(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            var em = ps.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(rate);
        }

        // ── Modulation de la taille des particules ────────────────────────────

        /// <summary>
        /// Applique <see cref="tailleMultiplicateur"/> sur les 5 systèmes via startSizeMultiplier.
        /// Modifiable en temps réel depuis l'Inspector ou par code via <see cref="TailleMultiplicateur"/>.
        /// </summary>
        private void AppliquerTaille()
        {
            if (_psNuage == null) return;
            AppliquerTaillePS(_psNuage,   _baseTailleNuage);
            AppliquerTaillePS(_psSable,   _baseTailleSable);
            AppliquerTaillePS(_psDebris,  _baseTailleDebris);
            AppliquerTaillePS(_psTrainee, _baseTailleTrainee);
            AppliquerTaillePS(_psSol,     _baseTailleSol);
        }

        private void AppliquerTaillePS(ParticleSystem ps, float baseTaille)
        {
            if (ps == null) return;
            var m = ps.main;
            m.startSizeMultiplier = baseTaille * tailleMultiplicateur;
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

        // ── Direction verticale ───────────────────────────────────────────────

        /// <summary>
        /// Injecte <see cref="directionVerticale"/> dans velocityOverLifetime.y de chaque système.
        /// Le nuage conserve sa montée organique de base à laquelle la valeur s'additionne ;
        /// les autres systèmes partent de 0 et sont entièrement pilotés par ce paramètre.
        /// </summary>
        private void AppliquerDirection()
        {
            if (_psNuage == null) return;

            // Nuage : la vélocité Y de base (montée organique) + directionVerticale
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

        // ── Vitesse de diffusion ──────────────────────────────────────────────

        /// <summary>
        /// Applique <see cref="vitesseDiffusion"/> sur startSpeedMultiplier des 5 systèmes.
        /// </summary>
        private void AppliquerVitesseDiffusion()
        {
            if (_psNuage == null) return;
            AppliquerVitesseDiffusionPS(_psNuage,   _baseVitesseNuage);
            AppliquerVitesseDiffusionPS(_psSable,   _baseVitesseSable);
            AppliquerVitesseDiffusionPS(_psDebris,  _baseVitesseDebris);
            AppliquerVitesseDiffusionPS(_psTrainee, _baseVitesseTrainee);
            AppliquerVitesseDiffusionPS(_psSol,     _baseVitesseSol);
        }

        private void AppliquerVitesseDiffusionPS(ParticleSystem ps, float baseVitesse)
        {
            if (ps == null) return;
            var m = ps.main;
            m.startSpeedMultiplier = baseVitesse * vitesseDiffusion;
        }

        // ── Durée de vie ──────────────────────────────────────────────────────

        /// <summary>
        /// Applique <see cref="duréeVieParticules"/> sur startLifetimeMultiplier des 5 systèmes.
        /// </summary>
        private void AppliquerDuréeVie()
        {
            if (_psNuage == null) return;
            AppliquerDuréeViePS(_psNuage,   _baseDuréeNuage);
            AppliquerDuréeViePS(_psSable,   _baseDuréeSable);
            AppliquerDuréeViePS(_psDebris,  _baseDuréeDebris);
            AppliquerDuréeViePS(_psTrainee, _baseDuréeTrainee);
            AppliquerDuréeViePS(_psSol,     _baseDuréeSol);
        }

        private void AppliquerDuréeViePS(ParticleSystem ps, float baseDurée)
        {
            if (ps == null) return;
            var m = ps.main;
            m.startLifetimeMultiplier = baseDurée * duréeVieParticules;
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
        //     Grand volume billowing, bruit Perlin, s'étend et monte doucement.
        private void ConfigNuage(ParticleSystem ps)
        {
            float e = echelle;

            var main = ps.main;
            main.loop             = true;
            main.simulationSpace  = ParticleSystemSimulationSpace.World;
            main.startLifetime    = new ParticleSystem.MinMaxCurve(2.2f, 4.0f);
            main.startSpeed       = new ParticleSystem.MinMaxCurve(0.7f * e, 2.0f * e);
            main.startSize        = new ParticleSystem.MinMaxCurve(0.12f * e, 0.28f * e);
            main.startRotation    = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.startColor       = new ParticleSystem.MinMaxGradient(couleurSable, couleurCendre);
            main.gravityModifier  = new ParticleSystem.MinMaxCurve(0.025f);
            main.maxParticles     = 300;

            var em = ps.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(22f);

            // Cône large ouvert vers -Z (derrière le véhicule)
            var shape = ps.shape;
            shape.enabled           = true;
            shape.shapeType         = ParticleSystemShapeType.Cone;
            shape.angle             = 35f;
            shape.radius            = 0.10f * e;
            shape.radiusThickness   = 1f;
            shape.rotation          = new Vector3(90f, 0f, 0f); // +Y local → -Z local

            // Couleur : sable chaud → gris cendre → vapeur blanche → transparent
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
                    new GradientAlphaKey(0.62f, 0.090f),
                    new GradientAlphaKey(0.48f, 0.420f),
                    new GradientAlphaKey(0.20f, 0.750f),
                    new GradientAlphaKey(0.00f, 1.000f),
                }
            );
            colL.color = new ParticleSystem.MinMaxGradient(g);

            // Taille : micro au spawn → large nuage billowing
            var szL = ps.sizeOverLifetime;
            szL.enabled = true;
            szL.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0.00f, 0.15f, 2.5f,  2.5f),
                new Keyframe(0.25f, 1.00f, 1.2f,  1.2f),
                new Keyframe(1.00f, 3.40f, 0.3f,  0.3f)
            ));

            // Rotation propre des particules (tumble)
            var rotL = ps.rotationOverLifetime;
            rotL.enabled = true;
            rotL.z = new ParticleSystem.MinMaxCurve(-65f * Mathf.Deg2Rad, 65f * Mathf.Deg2Rad);

            // Bruit de Perlin — mouvement organique non-uniforme
            var noise = ps.noise;
            noise.enabled     = true;
            noise.strength    = new ParticleSystem.MinMaxCurve(0.45f * e);
            noise.frequency   = 0.55f;
            noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.20f);
            noise.octaveCount = 3;
            noise.quality     = ParticleSystemNoiseQuality.Medium;

            // Résistance de l'air — ralentit les particules avec le temps
            var limV = ps.limitVelocityOverLifetime;
            limV.enabled  = true;
            limV.space    = ParticleSystemSimulationSpace.World;
            limV.limit    = new ParticleSystem.MinMaxCurve(2.0f * e);
            limV.dampen   = 0.12f;

            // Montée lente en World space — la poussière s'élève
            // Toutes les courbes doivent être dans le même mode (TwoConstants).
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
        //     Nappe de poussière très légère qui persiste au sol après le passage.
        private void ConfigTrainée(ParticleSystem ps)
        {
            float e = echelle;

            var main = ps.main;
            main.loop             = true;
            main.simulationSpace  = ParticleSystemSimulationSpace.World;
            main.startLifetime    = new ParticleSystem.MinMaxCurve(5.0f, 8.0f);
            main.startSpeed       = new ParticleSystem.MinMaxCurve(0.0f, 0.22f * e);
            main.startSize        = new ParticleSystem.MinMaxCurve(0.35f * e, 0.75f * e);
            main.startRotation    = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            // Très transparente dès la création
            main.startColor       = new ParticleSystem.MinMaxGradient(
                new Color(0.93f, 0.86f, 0.70f, 0.16f),
                new Color(0.87f, 0.80f, 0.66f, 0.22f)
            );
            main.gravityModifier  = new ParticleSystem.MinMaxCurve(0.0f);
            main.maxParticles     = 180;

            var em = ps.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(6f);

            // Boîte plate au ras du sol (couche de poussière)
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale     = new Vector3(0.24f * e, 0.02f, 0.20f * e);

            // Couleur : beige chaud → blanc vapeur → invisible
            var colL = ps.colorOverLifetime;
            colL.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.92f, 0.83f, 0.67f), 0.00f),
                    new GradientColorKey(new Color(0.95f, 0.91f, 0.83f), 0.40f),
                    new GradientColorKey(new Color(0.97f, 0.95f, 0.93f), 1.00f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.00f, 0.000f),
                    new GradientAlphaKey(0.22f, 0.055f),
                    new GradientAlphaKey(0.17f, 0.500f),
                    new GradientAlphaKey(0.00f, 1.000f),
                }
            );
            colL.color = new ParticleSystem.MinMaxGradient(g);

            // S'étale massivement en s'éloignant
            var szL = ps.sizeOverLifetime;
            szL.enabled = true;
            szL.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0.00f, 0.22f),
                new Keyframe(0.18f, 1.00f),
                new Keyframe(1.00f, 6.00f)
            ));

            // Bruit lent = dispersion organique et naturelle
            var noise = ps.noise;
            noise.enabled     = true;
            noise.strength    = new ParticleSystem.MinMaxCurve(0.16f * e);
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
