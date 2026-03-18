using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Barrage.UI
{
    /// <summary>
    /// Orchestre l'animation de Game Over :
    ///   1. Phase explosion  — chaque carte reçoit une impulsion aléatoire et vole dans tous les sens.
    ///   2. Phase attente    — délai avant la convergence.
    ///   3. Phase convergence — les cartes se redimensionnent et se replacent pour former "GAME OVER".
    ///
    /// Doit être abonné à BarrePatience.OnPatienceEpuisée.
    /// </summary>
    public class AnimationGameOver : MonoBehaviour
    {
        // ── Timing ───────────────────────────────────────────────────────────
        private const float DUREE_EXPLOSION    = 0.6f;  // s — vol chaotique
        private const float DELAI_CONVERGENCE  = 0.2f;  // s — pause avant convergence
        private const float DUREE_CONVERGENCE  = 1.1f;  // s — animation vers "GAME OVER"
        private const float DUREE_FADE_TEXTE   = 0.5f;  // s — fondu entrant du texte TMP

        // ── Layout "GAME OVER" ───────────────────────────────────────────────
        // Deux lignes : "GAME" (4 cartes) et "OVER" (4 cartes).
        // La 9e carte (si présente) sert d'espace central ou est masquée.
        private const string LIGNE_1 = "GAME";
        private const string LIGNE_2 = "OVER";

        [Header("Références")]
        [Tooltip("RectTransform du Canvas racine (couche de glissement) — espace de référence pour le layout.")]
        [SerializeField] private RectTransform coucheGlissement;
        [Tooltip("FormulaireLibreManager pour accéder aux cartes actives.")]
        [SerializeField] private FormulaireLibreManager formulaireManager;
        [Tooltip("BarrePatience dont OnPatienceEpuisée déclenche l'animation.")]
        [SerializeField] private BarrePatience barrePatience;

        [Header("Texte GAME OVER")]
        [Tooltip("Prefab TextMeshProUGUI instancié par-dessus les cartes une fois la formation terminée.")]
        [SerializeField] private TMP_FontAsset fonteGameOver;
        [SerializeField] private float          taillePolice = 220f;
        [SerializeField] private Color          couleurTexte = new Color(0.95f, 0.12f, 0.08f, 1f);

        [Header("Layout")]
        [Tooltip("Hauteur d'une ligne de cartes en pixels (espace de la CoucheGlissement).")]
        [SerializeField] private float hauteurLigne    = 360f;
        [Tooltip("Largeur d'une carte-lettre en pixels.")]
        [SerializeField] private float largeurLettre   = 180f;
        [Tooltip("Espacement horizontal entre cartes d'une même ligne.")]
        [SerializeField] private float espacementH     = 12f;
        [Tooltip("Espacement vertical entre les deux lignes.")]
        [SerializeField] private float espacementV     = 20f;
        [Tooltip("Centre vertical du bloc GAME OVER (0 = centre du canvas).")]
        [SerializeField] private float offsetY         = 0f;

        [Header("Explosion")]
        [SerializeField] private float vitesseExplosionMin = 600f;
        [SerializeField] private float vitesseExplosionMax = 1400f;

        private void OnEnable()
        {
            if (barrePatience != null)
                barrePatience.OnPatienceEpuisée += Déclencher;
        }

        private void OnDisable()
        {
            if (barrePatience != null)
                barrePatience.OnPatienceEpuisée -= Déclencher;
        }

        /// <summary>Point d'entrée : lance la séquence complète.</summary>
        public void Déclencher()
        {
            StartCoroutine(SequenceGameOver());
        }

        private IEnumerator SequenceGameOver()
        {
            // Récupérer toutes les cartes encore présentes
            List<FormulaireLibre> cartes = formulaireManager.ObtenirCartes();
            if (cartes.Count == 0) yield break;

            // ── Phase 1 : Explosion ──────────────────────────────────────────
            foreach (var carte in cartes)
            {
                float angle    = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float vitesse  = UnityEngine.Random.Range(vitesseExplosionMin, vitesseExplosionMax);
                var   impulsion = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * vitesse;
                carte.ExplosionGameOver(impulsion);
            }

            yield return new WaitForSeconds(DUREE_EXPLOSION + DELAI_CONVERGENCE);

            // ── Phase 2 : Calcul des positions cibles ────────────────────────
            List<(Vector2 pos, Vector2 taille)> slots = CalculerSlots(cartes.Count);

            // ── Phase 3 : Convergence parallèle ─────────────────────────────
            var convergences = new List<Coroutine>();

            for (int i = 0; i < cartes.Count; i++)
            {
                int idx = i < slots.Count ? i : slots.Count - 1;
                (Vector2 pos, Vector2 taille) = slots[idx];

                // Couleur translucide foncée pour que les lettres TMP se lisent bien dessus
                Color couleurCible = new Color(0.12f, 0.12f, 0.18f, 0.88f);

                var co = StartCoroutine(cartes[i].AnimerVers(pos, taille, 0f, couleurCible, DUREE_CONVERGENCE));
                convergences.Add(co);
            }

            // Attendre la fin de toutes les convergences
            foreach (var co in convergences)
                yield return co;

            // ── Phase 4 : Afficher le texte "GAME OVER" ─────────────────────
            yield return StartCoroutine(AfficherTexteGameOver());
        }

        /// <summary>
        /// Calcule les positions et tailles des slots en deux lignes ("GAME" / "OVER")
        /// centrées dans la CoucheGlissement.
        /// Les cartes en surplus (au-delà de 8) sont regroupées sur le dernier slot.
        /// </summary>
        private List<(Vector2, Vector2)> CalculerSlots(int nbCartes)
        {
            var résultat = new List<(Vector2, Vector2)>();
            Vector2 taille = new Vector2(largeurLettre, hauteurLigne);

            string[] lignes    = { LIGNE_1, LIGNE_2 };
            int      nbLignes  = lignes.Length;
            float    largMaxLigne = Mathf.Max(LIGNE_1.Length, LIGNE_2.Length);
            float    blocLargeur = largMaxLigne * largeurLettre + (largMaxLigne - 1) * espacementH;
            float    blocHauteur = nbLignes * hauteurLigne + (nbLignes - 1) * espacementV;

            float yDepart = offsetY + blocHauteur * 0.5f - hauteurLigne * 0.5f;

            for (int l = 0; l < lignes.Length; l++)
            {
                int    nbLettres  = lignes[l].Length;
                float  largLigne  = nbLettres * largeurLettre + (nbLettres - 1) * espacementH;
                float  xDepart    = -largLigne * 0.5f + largeurLettre * 0.5f;
                float  y          = yDepart - l * (hauteurLigne + espacementV);

                for (int c = 0; c < nbLettres; c++)
                {
                    float x = xDepart + c * (largeurLettre + espacementH);
                    résultat.Add((new Vector2(x, y), taille));
                }
            }

            return résultat;
        }

        /// <summary>
        /// Crée un TextMeshProUGUI par-dessus les cartes formant "GAME\nOVER"
        /// et l'anime en fondu entrant.
        /// </summary>
        private IEnumerator AfficherTexteGameOver()
        {
            var go = new GameObject("TexteGameOver", typeof(RectTransform), typeof(CanvasRenderer),
                                    typeof(TextMeshProUGUI));
            go.transform.SetParent(coucheGlissement, false);
            go.layer = LayerMask.NameToLayer("UI");

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, offsetY);
            rt.sizeDelta        = new Vector2(2000f, 900f);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text              = "GAME\nOVER";
            tmp.fontSize          = taillePolice;
            tmp.fontStyle         = FontStyles.Bold;
            tmp.alignment         = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.lineSpacing       = -20f;

            if (fonteGameOver != null)
                tmp.font = fonteGameOver;

            // Fondu entrant
            Color cFinal  = couleurTexte;
            Color cDepart = new Color(cFinal.r, cFinal.g, cFinal.b, 0f);
            tmp.color = cDepart;

            float t = 0f;
            while (t < DUREE_FADE_TEXTE)
            {
                t += Time.deltaTime;
                tmp.color = Color.Lerp(cDepart, cFinal, Mathf.SmoothStep(0f, 1f, t / DUREE_FADE_TEXTE));
                yield return null;
            }

            tmp.color = cFinal;
        }
    }
}
