using System.Collections.Generic;
using UnityEngine;

namespace Barrage.Effets
{
    /// <summary>
    /// Génère un champ de petits bouts de papier qui virevolent.
    /// La zone de simulation est calculée automatiquement depuis le frustum de la caméra
    /// (orthographique ou perspective) avec une marge configurable — aucun réglage manuel
    /// de tailleZone n'est nécessaire.
    ///
    /// Le suivi de caméra est pilotable via facteurParallax :
    ///   0   → couche fixe en world space (arrière-plan immobile)
    ///   0.5 → suit la caméra à moitié de sa vitesse (couche intermédiaire)
    ///   1   → verrouillé à l'écran (couche de premier plan)
    ///
    /// Assigne une ou plusieurs Texture2D dans 'Textures Feuilles' depuis l'Inspector.
    /// </summary>
    public class VirevoltePapier : MonoBehaviour
    {
        // ── Caméra & Parallax ─────────────────────────────────────────────────
        [Header("Caméra & Parallax")]
        [Tooltip("Caméra de référence. Si vide, Camera.main est utilisé automatiquement.")]
        [SerializeField] private Camera cameraRef;

        [Tooltip("0 = couche fixe en world space  /  1 = verrouillé à l'écran.\n" +
                 "Valeurs typiques : 0.2 (fond), 0.5 (milieu), 0.8 (premier plan).")]
        [SerializeField, Range(0f, 1f)] private float facteurParallax = 0.5f;

        [Tooltip("Multiplicateur appliqué aux dimensions du frustum pour définir la zone de simulation. " +
                 "1.4 = 40 % de marge — évite le pop-in aux bords lors des déplacements de caméra.")]
        [SerializeField] private float margeZone = 1.4f;

        // ── Sprites ───────────────────────────────────────────────────────────
        [Header("Sprites")]
        [Tooltip("Une ou plusieurs Texture2D utilisées pour les bouts de papier. " +
                 "Accepte n'importe quelle texture du projet — pas besoin du mode Sprite.")]
        [SerializeField] private Texture2D[] texturesFeuilles;

        [Tooltip("Teintes optionnelles appliquées aléatoirement. Si vide, blanc pur.")]
        [SerializeField] private Color[] couleurs = { Color.white };

        [SerializeField, Range(0f, 1f)] private float opaciteMin = 0.55f;
        [SerializeField, Range(0f, 1f)] private float opaciteMax = 1.0f;

        // ── Rendu ─────────────────────────────────────────────────────────────
        [Header("Rendu")]
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int    sortingOrder     = 0;

        // ── Quantité ──────────────────────────────────────────────────────────
        [Header("Quantité")]
        [SerializeField] private int nombreFeuilles = 35;

        // ── Vent ──────────────────────────────────────────────────────────────
        [Header("Vent")]
        [Tooltip("Direction et intensité du courant d'air de base (unités/s).")]
        [SerializeField] private Vector2 directionVent = new Vector2(0.4f, -0.15f);

        [SerializeField] private float vitesseMin = 0.15f;
        [SerializeField] private float vitesseMax = 0.55f;

        // ── Oscillation latérale ──────────────────────────────────────────────
        [Header("Oscillation latérale")]
        [Tooltip("Amplitude du balancement perpendiculaire à la direction du vent (unités monde).")]
        [SerializeField] private float amplitudeOscillation    = 0.38f;
        [SerializeField] private float frequenceOscillationMin = 0.4f;
        [SerializeField] private float frequenceOscillationMax = 1.3f;

        // ── Rotation ─────────────────────────────────────────────────────────
        [Header("Rotation")]
        [SerializeField] private float rotationVitesseMin = 20f;
        [SerializeField] private float rotationVitesseMax = 165f;

        // ── Taille des feuilles ───────────────────────────────────────────────
        [Header("Taille")]
        [SerializeField] private float tailleMin = 0.22f;
        [SerializeField] private float tailleMax = 0.62f;

        // ── Retournement (flip scale X) ───────────────────────────────────────
        [Header("Retournement")]
        [SerializeField] private float flipFreqMin = 0.3f;
        [SerializeField] private float flipFreqMax = 1.0f;

        // ── État interne ──────────────────────────────────────────────────────

        private struct DonnéesFeuille
        {
            public Transform trans;
            public Vector2   positionBase;
            public Vector2   direction;
            public Vector2   perpendiculaire;
            public float     vitesse;
            public float     rotationVitesse;
            public float     phaseOscillation;
            public float     freqOscillation;
            public float     flipPhase;
            public float     flipFreq;
            public float     tailleBase;
        }

        private readonly List<DonnéesFeuille> _feuilles = new List<DonnéesFeuille>();
        private Sprite[] _sprites;
        private Camera   _camera;
        private Vector2  _tailleZone;
        private Vector3  _origineCamera;
        private Vector3  _origineEffet;
        private float    _tempsCumul;

        // ── Initialisation ────────────────────────────────────────────────────

        private void Start()
        {
            _camera = cameraRef != null ? cameraRef : Camera.main;
            if (_camera == null)
            {
                Debug.LogWarning($"[VirevoltePapier] ({gameObject.name}) : " +
                                 "Aucune caméra trouvée. Assigne 'Camera Ref' ou ajoute une MainCamera.");
                return;
            }

            if (texturesFeuilles == null || texturesFeuilles.Length == 0)
            {
                Debug.LogWarning($"[VirevoltePapier] ({gameObject.name}) : " +
                                 "Aucune texture assignée dans 'Textures Feuilles'.");
                return;
            }

            if (couleurs == null || couleurs.Length == 0)
                couleurs = new[] { Color.white };

            _sprites = new Sprite[texturesFeuilles.Length];
            for (int i = 0; i < texturesFeuilles.Length; i++)
            {
                Texture2D tex = texturesFeuilles[i];
                if (tex == null) continue;
                _sprites[i] = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }

            _origineCamera = _camera.transform.position;

            // Centrer l'effet sur la vue caméra au démarrage, en conservant uniquement le Z.
            // Cela garantit que les feuilles spawned sont toujours dans le champ visible,
            // quelle que soit la position du GameObject dans la scène.
            Vector3 centreVue = _camera.transform.position;
            transform.position = new Vector3(centreVue.x, centreVue.y, transform.position.z);
            _origineEffet  = transform.position;
            _tailleZone    = CalculerTailleZone();

            for (int i = 0; i < nombreFeuilles; i++)
                CréerFeuille(positionInitialeAléatoire: true);
        }

        // ── Update ────────────────────────────────────────────────────────────

        private void Update()
        {
            if (_camera == null || _feuilles.Count == 0) return;

            _tempsCumul += Time.deltaTime;
            float dt = Time.deltaTime;

            // Suivi caméra pondéré par le facteur parallax.
            // facteurParallax = 0 → l'effet reste à _origineEffet (world space fixe)
            // facteurParallax = 1 → l'effet suit la caméra pixel pour pixel (écran fixe)
            Vector3 deltaCamera  = _camera.transform.position - _origineCamera;
            Vector3 centreZone   = _origineEffet + new Vector3(
                deltaCamera.x * facteurParallax,
                deltaCamera.y * facteurParallax,
                0f);
            transform.position = new Vector3(centreZone.x, centreZone.y, _origineEffet.z);

            float hw = _tailleZone.x * 0.5f;
            float hh = _tailleZone.y * 0.5f;

            for (int i = 0; i < _feuilles.Count; i++)
            {
                DonnéesFeuille f = _feuilles[i];

                f.positionBase += f.direction * f.vitesse * dt;

                float osc      = Mathf.Sin(_tempsCumul * f.freqOscillation * Mathf.PI * 2f
                                          + f.phaseOscillation) * amplitudeOscillation;
                Vector2 finale = f.positionBase + f.perpendiculaire * osc;

                f.trans.position = new Vector3(
                    finale.x + transform.position.x,
                    finale.y + transform.position.y,
                    f.trans.position.z);

                f.trans.Rotate(0f, 0f, f.rotationVitesse * dt, Space.Self);

                float cosFlip  = Mathf.Cos(_tempsCumul * f.flipFreq * Mathf.PI * 2f + f.flipPhase);
                float signFlip = cosFlip >= 0f ? 1f : -1f;
                f.trans.localScale = new Vector3(f.tailleBase * signFlip, f.tailleBase, 1f);

                if (f.positionBase.x >  hw) f.positionBase.x -= _tailleZone.x;
                if (f.positionBase.x < -hw) f.positionBase.x += _tailleZone.x;
                if (f.positionBase.y >  hh) f.positionBase.y -= _tailleZone.y;
                if (f.positionBase.y < -hh) f.positionBase.y += _tailleZone.y;

                _feuilles[i] = f;
            }
        }

        // ── Frustum ───────────────────────────────────────────────────────────

        /// <summary>
        /// Calcule les dimensions visibles de la caméra à la profondeur Z de cet effet,
        /// multipliées par margeZone. Supporte orthographique et perspective.
        /// </summary>
        private Vector2 CalculerTailleZone()
        {
            float hauteur, largeur;

            if (_camera.orthographic)
            {
                hauteur = _camera.orthographicSize * 2f;
                largeur = hauteur * _camera.aspect;
            }
            else
            {
                float distZ = Mathf.Abs(_camera.transform.position.z - transform.position.z);
                if (distZ < 0.01f) distZ = 10f;
                hauteur = 2f * distZ * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                largeur = hauteur * _camera.aspect;
            }

            return new Vector2(largeur * margeZone, hauteur * margeZone);
        }

        // ── Création d'une feuille ────────────────────────────────────────────

        private void CréerFeuille(bool positionInitialeAléatoire)
        {
            Sprite sprite = _sprites[Random.Range(0, _sprites.Length)];
            if (sprite == null) return;

            Color couleur = couleurs[Random.Range(0, couleurs.Length)];
            couleur.a = Random.Range(opaciteMin, opaciteMax);

            var go = new GameObject("Feuille", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);

            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite           = sprite;
            sr.color            = couleur;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder     = sortingOrder;

            float taille = Random.Range(tailleMin, tailleMax);
            go.transform.localScale    = new Vector3(taille, taille, 1f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            Vector2 dir = directionVent
                        + new Vector2(Random.Range(-0.12f, 0.12f), Random.Range(-0.12f, 0.12f));
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right * 0.1f;
            dir.Normalize();

            Vector2 perp = new Vector2(-dir.y, dir.x);

            Vector2 pos = positionInitialeAléatoire
                ? new Vector2(Random.Range(-_tailleZone.x * 0.5f, _tailleZone.x * 0.5f),
                              Random.Range(-_tailleZone.y * 0.5f, _tailleZone.y * 0.5f))
                : BordEntree(dir);

            float rotVitesse = Random.Range(rotationVitesseMin, rotationVitesseMax);
            if (Random.value > 0.5f) rotVitesse = -rotVitesse;

            _feuilles.Add(new DonnéesFeuille
            {
                trans            = go.transform,
                positionBase     = pos,
                direction        = dir,
                perpendiculaire  = perp,
                vitesse          = Random.Range(vitesseMin, vitesseMax),
                rotationVitesse  = rotVitesse,
                phaseOscillation = Random.Range(0f, Mathf.PI * 2f),
                freqOscillation  = Random.Range(frequenceOscillationMin, frequenceOscillationMax),
                flipPhase        = Random.Range(0f, Mathf.PI * 2f),
                flipFreq         = Random.Range(flipFreqMin, flipFreqMax),
                tailleBase       = taille,
            });

            go.transform.position = new Vector3(
                pos.x + transform.position.x,
                pos.y + transform.position.y,
                transform.position.z);
        }

        private Vector2 BordEntree(Vector2 direction)
        {
            float hw = _tailleZone.x * 0.5f;
            float hh = _tailleZone.y * 0.5f;

            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                float x = direction.x > 0f ? -hw : hw;
                return new Vector2(x, Random.Range(-hh, hh));
            }
            else
            {
                float y = direction.y > 0f ? -hh : hh;
                return new Vector2(Random.Range(-hw, hw), y);
            }
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Camera cam = cameraRef != null ? cameraRef : Camera.main;
            Vector2 zone;

            if (cam != null)
            {
                float h, l;
                if (cam.orthographic)
                {
                    h = cam.orthographicSize * 2f * margeZone;
                    l = h * cam.aspect;
                }
                else
                {
                    float dist = Mathf.Abs(cam.transform.position.z - transform.position.z);
                    if (dist < 0.01f) dist = 10f;
                    h = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * margeZone;
                    l = h * cam.aspect;
                }
                zone = new Vector2(l, h);
            }
            else
            {
                zone = new Vector2(22f, 14f);
            }

            Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.35f);
            Gizmos.DrawWireCube(transform.position, new Vector3(zone.x, zone.y, 0f));

            if (directionVent.sqrMagnitude > 0.001f)
            {
                Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
                Vector3 from = transform.position;
                Vector3 to   = from + (Vector3)(directionVent.normalized * 1.5f);
                Gizmos.DrawLine(from, to);
                Gizmos.DrawSphere(to, 0.08f);
            }
        }
    }
}
