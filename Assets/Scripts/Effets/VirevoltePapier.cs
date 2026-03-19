using System.Collections.Generic;
using UnityEngine;

namespace Barrage.Effets
{
    /// <summary>
    /// Génère un champ de petits bouts de papier qui virevolent.
    /// Chaque feuille dérive selon le vent, oscille latéralement (mouvement pendulaire),
    /// tourne sur elle-même et se retourne sur sa tranche (flip de scale X) pour
    /// simuler le comportement réaliste d'un papier dans l'air.
    ///
    /// Conçu pour servir de couche dans un système de parallax :
    /// place plusieurs instances à des profondeurs Z différentes et pilote-les
    /// avec un facteur de vitesse parallax croissant selon la profondeur.
    ///
    /// Assigne un ou plusieurs sprites dans 'Sprites Feuilles' depuis l'Inspector.
    /// </summary>
    public class VirevoltePapier : MonoBehaviour
    {
        // ── Sprites ───────────────────────────────────────────────────────────
        [Header("Sprites")]
        [Tooltip("Une ou plusieurs textures utilisées pour les bouts de papier. " +
                 "Accepte n'importe quelle Texture2D du projet — pas besoin de l'importer en mode Sprite.")]
        [SerializeField] private Texture2D[] texturesFeuilles;

        [Tooltip("Teintes optionnelles appliquées aléatoirement aux feuilles. Si vide, blanc pur.")]
        [SerializeField] private Color[] couleurs = { Color.white };

        [Tooltip("Opacité minimale d'une feuille.")]
        [SerializeField, Range(0f, 1f)] private float opaciteMin = 0.55f;

        [Tooltip("Opacité maximale d'une feuille.")]
        [SerializeField, Range(0f, 1f)] private float opaciteMax = 1.0f;

        // ── Sprites générés à l'exécution depuis les textures ─────────────────
        private Sprite[] _sprites;

        // ── Rendu ─────────────────────────────────────────────────────────────
        [Header("Rendu")]
        [Tooltip("Nom du Sorting Layer sur lequel les feuilles sont rendues.")]
        [SerializeField] private string sortingLayerName = "Default";

        [Tooltip("Ordre de rendu dans le Sorting Layer.")]
        [SerializeField] private int sortingOrder = 0;

        // ── Zone de simulation ────────────────────────────────────────────────
        [Header("Zone")]
        [Tooltip("Nombre de feuilles actives en même temps.")]
        [SerializeField] private int nombreFeuilles = 35;

        [Tooltip("Dimensions (largeur × hauteur) de la zone de simulation, centrée sur ce Transform.")]
        [SerializeField] private Vector2 tailleZone = new Vector2(22f, 14f);

        // ── Vent ──────────────────────────────────────────────────────────────
        [Header("Vent")]
        [Tooltip("Direction et intensité du courant d'air de base (unités/s). " +
                 "Chaque feuille ajoute une petite variation aléatoire à cette direction.")]
        [SerializeField] private Vector2 directionVent = new Vector2(0.4f, -0.15f);

        [SerializeField] private float vitesseMin = 0.15f;
        [SerializeField] private float vitesseMax = 0.55f;

        // ── Oscillation latérale (pendule) ────────────────────────────────────
        [Header("Oscillation latérale")]
        [Tooltip("Amplitude du balancement perpendiculaire à la direction du vent (unités).")]
        [SerializeField] private float amplitudeOscillation = 0.38f;

        [SerializeField] private float frequenceOscillationMin = 0.4f;
        [SerializeField] private float frequenceOscillationMax = 1.3f;

        // ── Rotation ─────────────────────────────────────────────────────────
        [Header("Rotation")]
        [SerializeField] private float rotationVitesseMin = 20f;
        [SerializeField] private float rotationVitesseMax = 165f;

        // ── Taille ────────────────────────────────────────────────────────────
        [Header("Taille")]
        [SerializeField] private float tailleMin = 0.22f;
        [SerializeField] private float tailleMax = 0.62f;

        // ── Retournement (flip scale X) ───────────────────────────────────────
        [Header("Retournement")]
        [Tooltip("Fréquence minimale du retournement sur la tranche (Hz).")]
        [SerializeField] private float flipFreqMin = 0.3f;

        [Tooltip("Fréquence maximale du retournement sur la tranche (Hz).")]
        [SerializeField] private float flipFreqMax = 1.0f;

        // ── Données internes par feuille ──────────────────────────────────────

        private struct DonnéesFeuille
        {
            public Transform      trans;
            public Vector2        positionBase;    // position de dérive (sans l'oscillation)
            public Vector2        direction;        // direction normalisée du déplacement
            public Vector2        perpendiculaire;  // perpendiculaire à direction, pour l'oscillation
            public float          vitesse;          // vitesse de dérive (unités/s)
            public float          rotationVitesse;  // degrés/s — positif ou négatif
            public float          phaseOscillation; // décalage de phase du pendule
            public float          freqOscillation;  // fréquence propre du pendule (Hz)
            public float          flipPhase;        // décalage de phase du retournement
            public float          flipFreq;         // fréquence du retournement (Hz)
            public float          tailleBase;       // échelle de base (positive)
        }

        private readonly List<DonnéesFeuille> _feuilles = new List<DonnéesFeuille>();
        private float _tempsCumul;

        // ── Cycle de vie ──────────────────────────────────────────────────────

        private void Start()
        {
            if (texturesFeuilles == null || texturesFeuilles.Length == 0)
            {
                Debug.LogWarning($"[VirevoltePapier] ({gameObject.name}) : " +
                                 "Aucune texture assignée dans 'Textures Feuilles'. L'effet ne sera pas visible.");
                return;
            }

            if (couleurs == null || couleurs.Length == 0)
                couleurs = new[] { Color.white };

            // Convertir chaque Texture2D en Sprite centré, couvrant toute la texture.
            // pixelsPerUnit = 100 est la valeur Unity standard — ajuste si tes sprites
            // apparaissent trop grands ou trop petits par rapport à ta scène.
            _sprites = new Sprite[texturesFeuilles.Length];
            for (int i = 0; i < texturesFeuilles.Length; i++)
            {
                Texture2D tex = texturesFeuilles[i];
                if (tex == null) continue;

                _sprites[i] = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), // pivot centré
                    100f);                   // pixels par unité
            }

            for (int i = 0; i < nombreFeuilles; i++)
                CréerFeuille(positionInitialeAléatoire: true);
        }

        private void Update()
        {
            _tempsCumul += Time.deltaTime;
            float dt    = Time.deltaTime;

            float hw = tailleZone.x * 0.5f;
            float hh = tailleZone.y * 0.5f;

            for (int i = 0; i < _feuilles.Count; i++)
            {
                DonnéesFeuille f = _feuilles[i];

                // ── Dérive selon le vent ─────────────────────────────────────
                f.positionBase += f.direction * f.vitesse * dt;

                // ── Oscillation latérale (balancement pendulaire) ────────────
                float oscOffset = Mathf.Sin(_tempsCumul * f.freqOscillation * Mathf.PI * 2f
                                           + f.phaseOscillation)
                                 * amplitudeOscillation;
                Vector2 posFinale = f.positionBase + f.perpendiculaire * oscOffset;

                f.trans.position = new Vector3(
                    posFinale.x + transform.position.x,
                    posFinale.y + transform.position.y,
                    f.trans.position.z);

                // ── Rotation sur elle-même ───────────────────────────────────
                f.trans.Rotate(0f, 0f, f.rotationVitesse * dt, Space.Self);

                // ── Retournement sur la tranche (flip scale X) ───────────────
                // Mathf.Cos oscille entre -1 et 1 — on extrait le signe pour un flip net,
                // légèrement adouci en interpolant avec la valeur brute pour éviter le pop.
                float cosFlip  = Mathf.Cos(_tempsCumul * f.flipFreq * Mathf.PI * 2f + f.flipPhase);
                float signFlip = cosFlip >= 0f ? 1f : -1f;
                f.trans.localScale = new Vector3(f.tailleBase * signFlip, f.tailleBase, 1f);

                // ── Wrap toroïdal dans la zone ───────────────────────────────
                if (f.positionBase.x >  hw) f.positionBase.x -= tailleZone.x;
                if (f.positionBase.x < -hw) f.positionBase.x += tailleZone.x;
                if (f.positionBase.y >  hh) f.positionBase.y -= tailleZone.y;
                if (f.positionBase.y < -hh) f.positionBase.y += tailleZone.y;

                _feuilles[i] = f;
            }
        }

        // ── Création d'une feuille ────────────────────────────────────────────

        private void CréerFeuille(bool positionInitialeAléatoire)
        {
            Sprite sprite  = _sprites[Random.Range(0, _sprites.Length)];
            if (sprite == null) return;
            Color  couleur = couleurs[Random.Range(0, couleurs.Length)];
            couleur.a = Random.Range(opaciteMin, opaciteMax);

            var go = new GameObject("Feuille", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(transform.position.x,
                                                transform.position.y,
                                                transform.position.z);

            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite           = sprite;
            sr.color            = couleur;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder     = sortingOrder;

            // Taille de base
            float taille = Random.Range(tailleMin, tailleMax);
            go.transform.localScale    = new Vector3(taille, taille, 1f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            // Direction du vent avec variation individuelle
            Vector2 dir = directionVent
                        + new Vector2(Random.Range(-0.12f, 0.12f), Random.Range(-0.12f, 0.12f));
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right * 0.1f;
            dir.Normalize();

            Vector2 perp = new Vector2(-dir.y, dir.x); // perpendiculaire à gauche

            // Position initiale
            Vector2 pos = positionInitialeAléatoire
                ? new Vector2(Random.Range(-tailleZone.x * 0.5f, tailleZone.x * 0.5f),
                              Random.Range(-tailleZone.y * 0.5f, tailleZone.y * 0.5f))
                : BordEntree(dir);

            // Vitesse de rotation — moitié des feuilles tournent dans le sens inverse
            float rotVitesse = Random.Range(rotationVitesseMin, rotationVitesseMax);
            if (Random.value > 0.5f) rotVitesse = -rotVitesse;

            _feuilles.Add(new DonnéesFeuille
            {
                trans             = go.transform,
                positionBase      = pos,
                direction         = dir,
                perpendiculaire   = perp,
                vitesse           = Random.Range(vitesseMin, vitesseMax),
                rotationVitesse   = rotVitesse,
                phaseOscillation  = Random.Range(0f, Mathf.PI * 2f),
                freqOscillation   = Random.Range(frequenceOscillationMin, frequenceOscillationMax),
                flipPhase         = Random.Range(0f, Mathf.PI * 2f),
                flipFreq          = Random.Range(flipFreqMin, flipFreqMax),
                tailleBase        = taille,
            });

            go.transform.position = new Vector3(
                pos.x + transform.position.x,
                pos.y + transform.position.y,
                transform.position.z);
        }

        /// <summary>
        /// Retourne un point aléatoire sur le bord d'entrée du vent
        /// (bord opposé à la direction de dérive).
        /// </summary>
        private Vector2 BordEntree(Vector2 direction)
        {
            float hw = tailleZone.x * 0.5f;
            float hh = tailleZone.y * 0.5f;

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
            // Zone de simulation
            Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.35f);
            Gizmos.DrawWireCube(transform.position,
                                new Vector3(tailleZone.x, tailleZone.y, 0f));

            // Direction du vent
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
