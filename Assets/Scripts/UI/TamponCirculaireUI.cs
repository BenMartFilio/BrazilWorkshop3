using UnityEngine;
using UnityEngine.UI;

namespace Barrage.UI
{
    /// <summary>
    /// Graphic procédural simulant l'empreinte d'un tampon encreur circulaire autour d'une lettre.
    /// Dessine un anneau dont les bords intérieur et extérieur sont bruités (Perlin) pour imiter
    /// l'encre inégalement répartie, avec des lacunes (zones sans contact) et des gouttes d'encre
    /// dispersées à la périphérie.
    /// Entièrement déterministe via une graine — le même résultat visuel est reproduit à chaque
    /// rebuild du mesh (canvas dirty, changement de résolution…).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class TamponCirculaireUI : MaskableGraphic
    {
        // ── Qualité du maillage ───────────────────────────────────────────────
        private const int SEGS_ANNEAU = 80; // segments de l'anneau principal
        private const int SEGS_GOUTTE = 7;  // segments de chaque goutte d'encre

        // ── Paramètres de forme (initialisés via Initialiser) ─────────────────
        private int   _graine       = 42;
        private float _rayonBase    = 80f;
        private float _epaisseur    = 20f;
        private float _baveExt      = 15f;  // amplitude bruit bord extérieur
        private float _baveInt      = 8f;   // amplitude bruit bord intérieur
        private float _freqNoise    = 2.6f; // fréquence spatiale du bruit Perlin
        private int   _nbGouttes    = 9;    // gouttes d'encre autour de l'anneau
        private float _tailleGoutte = 9f;   // rayon de base d'une goutte (px)
        private float _probGap      = 0.07f; // probabilité qu'un segment soit absent

        /// <summary>
        /// Configure le tampon avec ses paramètres de forme et régénère le mesh.
        /// Doit être appelé avant que le GameObject soit visible.
        /// </summary>
        public void Initialiser(int graine, float rayon, float epaisseur,
                                float baveExt, float baveInt)
        {
            _graine    = graine;
            _rayonBase = rayon;
            _epaisseur = epaisseur;
            _baveExt   = baveExt;
            _baveInt   = baveInt;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            // Générateur déterministe : même graine → même résultat visuel à chaque rebuild.
            var rng  = new System.Random(_graine);
            float s  = (float)(rng.NextDouble() * 100.0); // décalage de la seed Perlin

            // ── Précalcul des rayons bruités pour chaque vertex ──────────────
            float[] rOut = new float[SEGS_ANNEAU + 1];
            float[] rIn  = new float[SEGS_ANNEAU + 1];
            bool[]  gap  = new bool[SEGS_ANNEAU];

            for (int i = 0; i <= SEGS_ANNEAU; i++)
            {
                float a  = (float)i / SEGS_ANNEAU * Mathf.PI * 2f;
                float cx = Mathf.Cos(a) * _freqNoise;
                float cy = Mathf.Sin(a) * _freqNoise;
                rOut[i] = _rayonBase
                         + (Mathf.PerlinNoise(s + cx,          cy)          * 2f - 1f) * _baveExt;
                rIn[i]  = Mathf.Max(1f,
                          (_rayonBase - _epaisseur)
                         + (Mathf.PerlinNoise(s + 9.1f + cx,   cy + 9.1f)  * 2f - 1f) * _baveInt);
            }

            for (int i = 0; i < SEGS_ANNEAU; i++)
                gap[i] = rng.NextDouble() < _probGap;

            Color32 c = color;

            // ── Anneau principal ──────────────────────────────────────────────
            for (int i = 0; i < SEGS_ANNEAU; i++)
            {
                if (gap[i]) continue;

                float   a0 = (float)i       / SEGS_ANNEAU * Mathf.PI * 2f;
                float   a1 = (float)(i + 1) / SEGS_ANNEAU * Mathf.PI * 2f;
                Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
                Vector2 d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));

                int bi = vh.currentVertCount;
                vh.AddVert(d0 * rOut[i],      c, Vector2.zero); // 0 ext t0
                vh.AddVert(d0 * rIn[i],       c, Vector2.zero); // 1 int t0
                vh.AddVert(d1 * rOut[i + 1],  c, Vector2.zero); // 2 ext t1
                vh.AddVert(d1 * rIn[i + 1],   c, Vector2.zero); // 3 int t1

                vh.AddTriangle(bi,     bi + 1, bi + 2);
                vh.AddTriangle(bi + 1, bi + 3, bi + 2);
            }

            // ── Gouttes d'encre à la périphérie ──────────────────────────────
            for (int g = 0; g < _nbGouttes; g++)
            {
                // Position angulaire de la goutte — légèrement décalée de la grille régulière
                float aG   = (float)g / _nbGouttes * Mathf.PI * 2f
                           + (float)(rng.NextDouble() * 1.4 - 0.7);
                // Distance depuis le centre — autour du rayon extérieur
                float rG   = _rayonBase + (float)(rng.NextDouble() * 2.0 - 1.0) * _baveExt * 0.5f;
                float size = _tailleGoutte * (0.4f + (float)rng.NextDouble() * 1.3f);

                Vector2 center = new Vector2(Mathf.Cos(aG), Mathf.Sin(aG)) * rG;
                int     ci     = vh.currentVertCount;

                vh.AddVert((Vector3)center, c, Vector2.zero); // centre de la goutte

                for (int k = 0; k < SEGS_GOUTTE; k++)
                {
                    float ak = (float)k / SEGS_GOUTTE * Mathf.PI * 2f;
                    // Rayon irrégulier par vertex pour l'aspect organique
                    float r  = size * (0.5f + (float)rng.NextDouble() * 1.1f);
                    Vector2 p = center + new Vector2(Mathf.Cos(ak), Mathf.Sin(ak)) * r;
                    vh.AddVert((Vector3)p, c, Vector2.zero);
                }

                // Fan de triangles — le dernier se referme sur le premier vertex du périmètre
                for (int k = 0; k < SEGS_GOUTTE; k++)
                    vh.AddTriangle(ci, ci + k + 1, ci + (k + 1) % SEGS_GOUTTE + 1);
            }
        }
    }
}
