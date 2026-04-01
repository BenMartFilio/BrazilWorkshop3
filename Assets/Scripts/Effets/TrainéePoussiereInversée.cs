using System.Collections.Generic;
using UnityEngine;

namespace Barrage.Effets
{
    /// <summary>
    /// Traînée de poussière pour les véhicules inversés.
    ///
    /// Contrairement à <see cref="PoussiereVoiture"/> (basé sur les ParticleSystem Unity),
    /// ce système génère des SpriteRenderer individuels qui :
    ///   1. Apparaissent aux positions des roues du véhicule.
    ///   2. Défilent vers le bas en world space au même rythme que la route
    ///      (<see cref="vitesseDéfilement"/>), donnant l'illusion que la poussière
    ///      est laissée sur place pendant que le véhicule monte à l'écran.
    ///   3. S'estompent et grandissent sur leur durée de vie.
    ///
    /// Les sprites sont des instances poolées placées à la racine de la scène —
    /// ils ne suivent pas le véhicule après leur spawn.
    /// Pilotez <see cref="VitesseVehicule"/> et <see cref="VitesseDéfilement"/>
    /// depuis le contrôleur de véhicule.
    /// </summary>
    [AddComponentMenu("FX/Traînée Poussière Inversée")]
    public class TrainéePoussiereInversée : MonoBehaviour
    {
        // ── Spawn ─────────────────────────────────────────────────────────────
        [Header("Spawn")]
        [Tooltip("Nombre de sprites spawné par seconde par roue (× VitesseVehicule).")]
        [SerializeField, Min(1f)] private float tauxSpawn = 12f;

        [Tooltip("Décalage local de la roue gauche par rapport au pivot du véhicule.")]
        [SerializeField] private Vector2 décalageRoueGauche = new Vector2(-0.28f, 0f);

        [Tooltip("Décalage local de la roue droite par rapport au pivot du véhicule.")]
        [SerializeField] private Vector2 décalageRoueDroite = new Vector2( 0.28f, 0f);

        // ── Mouvement ─────────────────────────────────────────────────────────
        [Header("Mouvement")]
        [Tooltip("Vitesse de défilement vertical (unités/s vers le bas) identique à celle de la route.\n" +
                 "Synchroniser avec le scrolling global de la scène.")]
        [SerializeField] private float vitesseDéfilement = 5f;

        [Tooltip("Vitesse normalisée du véhicule [0-1]. 0 = arrêt, 1 = pleine vitesse.\n" +
                 "Pilotée depuis le contrôleur de véhicule.")]
        [SerializeField, Range(0f, 1f)] private float vitesseVehicule = 1f;

        // ── Apparence ─────────────────────────────────────────────────────────
        [Header("Apparence")]
        [Tooltip("Taille minimale du sprite à la naissance (unités monde).")]
        [SerializeField, Min(0.01f)] private float tailleMin = 0.12f;

        [Tooltip("Taille maximale du sprite à la naissance (unités monde).")]
        [SerializeField, Min(0.01f)] private float tailleMax = 0.28f;

        [Tooltip("Facteur de croissance maximal atteint en fin de vie (×tailleNaissance).")]
        [SerializeField, Min(1f)] private float croissanceMax = 2.5f;

        [Tooltip("Durée de vie minimale d'un sprite (secondes).")]
        [SerializeField, Min(0.05f)] private float duréeMin = 0.25f;

        [Tooltip("Durée de vie maximale d'un sprite (secondes).")]
        [SerializeField, Min(0.05f)] private float duréeMax = 0.65f;

        [Tooltip("Vitesse de rotation aléatoire maximale (degrés/s).\n" +
                 "0 = pas de rotation.")]
        [SerializeField, Min(0f)] private float vitesseRotation = 45f;

        [Tooltip("Couleur / opacité à la naissance.")]
        [SerializeField] private Color couleurDébut = new Color(0.08f, 0.07f, 0.06f, 0.65f);

        [Tooltip("Couleur / opacité en fin de vie (généralement transparent).")]
        [SerializeField] private Color couleurFin = new Color(0.18f, 0.15f, 0.12f, 0f);

        // ── Rendu ─────────────────────────────────────────────────────────────
        [Header("Rendu")]
        [Tooltip("Sorting layer appliqué à chaque sprite spawné.")]
        [SerializeField] private string coucheTri = "Décor";

        [Tooltip("Ordre de rendu au sein du sorting layer.")]
        [SerializeField] private int ordreTri = 0;

        [Tooltip("Matériau partagé pour les sprites de traînée (Sprites/Default ou URP 2D Sprite-Unlit).\n" +
                 "Assigner l'asset FX_Mat_Trainee.mat depuis Assets/Materials/.\n" +
                 "Si non assigné, le matériau sera créé au runtime via Shader.Find() — non fiable sur mobile.")]
        [SerializeField] private Material matériauAsset;

        // ── État interne ──────────────────────────────────────────────────────
        private struct InstancePoussière
        {
            public GameObject     go;
            public SpriteRenderer sr;
            public float          tempsVie;
            public float          duréeTotale;
            public float          tailleNaissance;
            public float          vitesseRot;
        }

        private readonly List<InstancePoussière>              _actifs = new List<InstancePoussière>(64);
        private readonly Queue<(GameObject go, SpriteRenderer sr)> _pool = new Queue<(GameObject, SpriteRenderer)>(64);

        private Sprite   _sprite;
        private Material _matériau;
        private float    _timerSpawn;

        private const int TAILLE_TEXTURE = 64;
        private const int TAILLE_POOL    = 64;

        // ── API publique ──────────────────────────────────────────────────────

        /// <summary>Vitesse normalisée du véhicule [0-1]. 0 = arrêt, 1 = pleine vitesse.</summary>
        public float VitesseVehicule
        {
            get => vitesseVehicule;
            set => vitesseVehicule = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Vitesse de défilement vertical (unités/s).
        /// Synchroniser avec le scrolling de la scène pour que les sprites restent visuellement sur place.
        /// </summary>
        public float VitesseDéfilement
        {
            get => vitesseDéfilement;
            set => vitesseDéfilement = value;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            CréerRessources();
            PréchaufferPool();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // ── Spawn ──────────────────────────────────────────────────────────
            if (vitesseVehicule > 0.01f)
            {
                float interval = 1f / (tauxSpawn * vitesseVehicule);
                _timerSpawn += dt;
                while (_timerSpawn >= interval)
                {
                    _timerSpawn -= interval;
                    SpawnerSprite(décalageRoueGauche);
                    SpawnerSprite(décalageRoueDroite);
                }
            }
            else
            {
                _timerSpawn = 0f;
            }

            // ── Mise à jour des instances actives ─────────────────────────────
            for (int i = _actifs.Count - 1; i >= 0; i--)
            {
                var inst = _actifs[i];
                inst.tempsVie += dt;
                float t = inst.tempsVie / inst.duréeTotale;

                if (t >= 1f)
                {
                    RetournerPool(inst);
                    _actifs.RemoveAt(i);
                    continue;
                }

                // Défilement vers le bas — même vitesse que la route
                inst.go.transform.position += Vector3.down * vitesseDéfilement * dt;

                // Croissance progressive
                float taille = inst.tailleNaissance * Mathf.Lerp(1f, croissanceMax, t);
                inst.go.transform.localScale = new Vector3(taille, taille, 1f);

                // Rotation
                inst.go.transform.Rotate(0f, 0f, inst.vitesseRot * dt);

                // Couleur / alpha
                inst.sr.color = Color.Lerp(couleurDébut, couleurFin, t);

                _actifs[i] = inst;
            }
        }

        private void OnDestroy()
        {
            foreach (var inst in _actifs)
                if (inst.go != null) Destroy(inst.go);

            while (_pool.Count > 0)
            {
                var (go, _) = _pool.Dequeue();
                if (go != null) Destroy(go);
            }
        }

        // ── Spawn et pool ─────────────────────────────────────────────────────

        private void SpawnerSprite(Vector2 décalageLocal)
        {
            if (_pool.Count == 0)
                _pool.Enqueue(CréerInstance());

            var (go, sr) = _pool.Dequeue();

            // Position monde = pivot véhicule + décalage (espace monde 2D)
            Vector3 pos = transform.position + new Vector3(décalageLocal.x, décalageLocal.y, 0f);
            go.transform.position    = pos;
            go.transform.rotation    = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            float taille = Random.Range(tailleMin, tailleMax);
            go.transform.localScale  = new Vector3(taille, taille, 1f);

            float vitRot = Random.Range(-vitesseRotation, vitesseRotation);
            float durée  = Random.Range(duréeMin, duréeMax);

            sr.color            = couleurDébut;
            sr.sortingLayerName = coucheTri;
            sr.sortingOrder     = ordreTri;
            go.SetActive(true);

            _actifs.Add(new InstancePoussière
            {
                go              = go,
                sr              = sr,
                tempsVie        = 0f,
                duréeTotale     = durée,
                tailleNaissance = taille,
                vitesseRot      = vitRot,
            });
        }

        private void RetournerPool(InstancePoussière inst)
        {
            inst.go.SetActive(false);
            _pool.Enqueue((inst.go, inst.sr));
        }

        // ── Création des ressources ───────────────────────────────────────────

        private void CréerRessources()
        {
            _sprite   = CréerSpriteCircle(TAILLE_TEXTURE);
            _matériau = matériauAsset != null
                ? matériauAsset   // asset sur disque — garanti dans le build mobile
                : CréerMatériau(); // fallback runtime — non fiable sur mobile

            if (matériauAsset == null)
                Debug.LogWarning("[TrainéePoussiereInversée] matériauAsset non assigné — création runtime. " +
                                 "Assigner FX_Mat_Trainee.mat dans l'Inspector pour garantir le rendu sur mobile.");
        }

        private void PréchaufferPool()
        {
            for (int i = 0; i < TAILLE_POOL; i++)
                _pool.Enqueue(CréerInstance());
        }

        private (GameObject go, SpriteRenderer sr) CréerInstance()
        {
            // Spawner à la racine de la scène — les sprites ne doivent pas suivre le véhicule.
            var go = new GameObject("FX_TrainéeInversée_Sprite");
            go.transform.SetParent(null, false);
            go.SetActive(false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite           = _sprite;
            sr.sharedMaterial   = _matériau;
            sr.sortingLayerName = coucheTri;
            sr.sortingOrder     = ordreTri;

            return (go, sr);
        }

        // ── Génération procédurale du sprite ──────────────────────────────────

        /// <summary>
        /// Génère un sprite en mémoire : dégradé radial blanc → transparent.
        /// Smoothstep pour des bords doux sans artéfact.
        /// </summary>
        private static Sprite CréerSpriteCircle(int taille)
        {
            var tex = new Texture2D(taille, taille, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Clamp,
            };

            float r  = taille * 0.5f;
            float cx = r, cy = r;

            for (int y = 0; y < taille; y++)
            {
                for (int x = 0; x < taille; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float t    = Mathf.Clamp01(1f - dist / r);
                    // Smoothstep : bords très doux, centre opaque
                    t = t * t * (3f - 2f * t);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, t));
                }
            }

            tex.Apply();

            return Sprite.Create(
                tex,
                new Rect(0, 0, taille, taille),
                pivot: new Vector2(0.5f, 0.5f),
                pixelsPerUnit: taille
            );
        }

        private static Material CréerMatériau()
        {
            // Sprites/Default fonctionne en URP (shader de compatibilité 2D).
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");

            return new Material(shader) { name = "FX_Mat_TrainéeInversée" };
        }
    }
}
