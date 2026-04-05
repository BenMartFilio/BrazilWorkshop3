using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Random = UnityEngine.Random;

namespace Barrage.UI
{
    /// <summary>
    /// Orchestre l'animation de Game Over :
    ///   1. Phase mise en place  — les 8 cartes glissent vers la formation "GAME / OVER"
    ///                             en deux rangées de 4. Les cartes ne sont plus déplaçables.
    ///   2. Phase tamponnage     — toutes les 0.5 s, une lettre est tamponnée sur sa carte
    ///                             via une animation scale-punch (impression d'un tampon encreur).
    ///
    /// Doit être abonné à BarrePatience.OnPatienceEpuisée.
    /// </summary>
    public class AnimationGameOver : MonoBehaviour
    {
        // ── Timing ───────────────────────────────────────────────────────────
        private const float DUREE_MISE_EN_PLACE  = 0.9f;   // s — glissement des cartes vers leurs slots
        private const float DELAI_AVANT_TAMPONS  = 0.3f;   // s — pause après la mise en place

        // ── Vérification du positionnement ───────────────────────────────────
        private const float SEUIL_POSITION_OK    = 4f;     // px — écart max accepté par rapport au slot
        private const int   MAX_TENTATIVES_POS   = 5;      // tentatives de correction avant d'abandonner
        private const float DUREE_CORRECTION_POS = 0.25f;  // s — durée de la coroutine de correction

        // ── Tampon ────────────────────────────────────────────────────────────
        private const float TAMPON_DUREE_DESCENTE = 0.07f; // s — le texte "tombe" vers la carte
        private const float TAMPON_ECRASE_SCALE   = 1.25f; // pic de scale au moment de l'impact
        private const float TAMPON_DUREE_REBOND   = 0.18f; // s — retour à l'échelle normale
        private const float TAMPON_ROTATION_MAX   = 6f;    // ° — légère inclinaison aléatoire

        // ── Layout "GAME OVER" ───────────────────────────────────────────────
        private const string LIGNE_1 = "GAME";
        private const string LIGNE_2 = "OVER";
        private const int    NB_CARTES_REQUISES = 8;

        [Header("Références")]
        [Tooltip("RectTransform du Canvas racine (couche de glissement) — espace de référence pour le layout.")]
        [SerializeField] private RectTransform coucheGlissement;
        [Tooltip("FormulaireLibreManager pour accéder aux cartes actives.")]
        [SerializeField] private FormulaireLibreManager formulaireManager;
        [Tooltip("BarrePatience dont OnPatienceEpuisée déclenche l'animation.")]
        [SerializeField] private BarrePatience barrePatience;
        [Tooltip("Canvas MainDuGarde à désactiver pendant l'animation de game over.")]
        [SerializeField] private Canvas mainDuGarde;
        [Tooltip("Vignette d'assombrissement à animer en début de game over.")]
        [SerializeField] private VignetteGameOver vignette;

        [Header("Timing tampons")]
        [Tooltip("Délai en secondes entre l'arrivée de chaque lettre tamponnée.")]
        [SerializeField] private float delaiEntreTampons = 0.5f;

        [Header("Retour au menu")]
        [Tooltip("Délai en secondes après le dernier tampon avant le chargement de la scène menu.")]
        [SerializeField] private float délaiAvantMenu = 2f;

        [Header("Cartes — taille et disposition")]
        [Tooltip("Largeur d'une carte-lettre en pixels. Valeur originale : 175.")]
        [SerializeField] private float largeurLettre = 105f;
        [Tooltip("Hauteur d'une carte-lettre en pixels. Valeur originale : 340.")]
        [SerializeField] private float hauteurLigne  = 204f;
        [Tooltip("Espacement horizontal entre les cartes d'une même ligne.")]
        [SerializeField] private float espacementH   = 10f;
        [Tooltip("Espacement vertical entre les deux lignes GAME / OVER.")]
        [SerializeField] private float espacementV   = 14f;
        [Tooltip("Décalage vertical du bloc 'GAME OVER' par rapport au centre du canvas. Négatif = vers le bas.")]
        [SerializeField] private float offsetY       = 0f;

        [Header("Lettre tamponnée")]
        [Tooltip("Police utilisée pour les lettres tamponnées sur chaque carte.")]
        [SerializeField] private TMP_FontAsset fonteGameOver;
        [Tooltip("Taille de la lettre en points. Valeur originale : 160.")]
        [SerializeField] private float taillePolice  = 96f;
        [Tooltip("Couleur de la lettre et de l'anneau tamponné.")]
        [SerializeField] private Color couleurTexte  = new Color(0.88f, 0.08f, 0.06f, 1f);

        [Header("Anneau tamponné")]
        [Tooltip("Rayon du cercle tampon en pixels. Valeur originale : 103.")]
        [SerializeField] private float anneauRayon    = 62f;
        [Tooltip("Épaisseur du trait de l'anneau en pixels. Valeur originale : 20.")]
        [SerializeField] private float anneauEpaisseur = 12f;
        [Tooltip("Amplitude du bruit sur le bord extérieur (bavure externe). Valeur originale : 15.")]
        [SerializeField] private float anneauBaveExt  = 9f;
        [Tooltip("Amplitude du bruit sur le bord intérieur (bavure interne). Valeur originale : 8.")]
        [SerializeField] private float anneauBaveInt  = 5f;

        /// <summary>Déclenché après la fin de l'animation et du délai final, à la place du chargement de scène.</summary>
        public event Action OnAnimationTerminée;


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

        /// <summary>Point d'entrée : lance la séquence complète de game over.</summary>
        public void Déclencher()
        {
            StartCoroutine(SequenceGameOver());
        }

        private IEnumerator SequenceGameOver()
        {
            // Désactiver le canvas MainDuGarde pendant toute l'animation
            if (mainDuGarde != null)
                mainDuGarde.gameObject.SetActive(false);

            // Attendre que la vignette atteigne son alpha initial AVANT de lancer les animations
            if (vignette != null)
                yield return StartCoroutine(vignette.AnimerApparition());

            // Garantir qu'il y a exactement NB_CARTES_REQUISES cartes disponibles.
            formulaireManager.CompleterCartesGameOver(NB_CARTES_REQUISES);

            List<FormulaireLibre> cartes = formulaireManager.ObtenirCartes();

            int nb = Mathf.Min(cartes.Count, NB_CARTES_REQUISES);
            if (nb == 0)
            {
                Debug.LogError("[AnimationGameOver] Aucune carte disponible après complétion. " +
                               "Vérifier que 'Formulaires Data' est renseigné dans FormulaireLibreManager.");
                yield break;
            }

            List<(Vector2 pos, Vector2 taille)> slots = CalculerSlots();

            // ── Phase 1 : Mise en place ──────────────────────────────────────
            var convergences = new List<Coroutine>();

            for (int i = 0; i < nb; i++)
            {
                (Vector2 pos, Vector2 taille) = slots[i];
                var co = StartCoroutine(cartes[i].AnimerVers(
                    pos, taille, 0f, Color.white, DUREE_MISE_EN_PLACE));
                convergences.Add(co);
            }

            foreach (var co in convergences)
                yield return co;

            // Figer la physique — exclut aussi ces cartes de ResoudreCollisions()
            for (int i = 0; i < nb; i++)
                cartes[i].FigerPourGameOver();

            // ── Vérification et replacement avant tamponnage ──────────────────
            // On attend 2 frames pour que LateUpdate ait fini toute passe résiduelle
            // de collision avant de mesurer les écarts.
            yield return null;
            yield return null;

            // Collecter toutes les cartes hors de leur slot et les corriger EN PARALLÈLE.
            // On boucle jusqu'à ce que toutes soient en place (max MAX_TENTATIVES_POS tours).
            for (int tentative = 0; tentative < MAX_TENTATIVES_POS; tentative++)
            {
                var corrections = new List<Coroutine>();

                for (int i = 0; i < nb; i++)
                {
                    if (cartes[i] == null) continue;

                    float ecart = Vector2.Distance(
                        cartes[i].Rt.anchoredPosition, slots[i].pos);

                    if (ecart > SEUIL_POSITION_OK)
                    {
                        // Lancer la correction en parallèle (ne pas bloquer ici)
                        var co = StartCoroutine(cartes[i].AnimerVers(
                            slots[i].pos, slots[i].taille, 0f, Color.white, DUREE_CORRECTION_POS));
                        corrections.Add(co);
                    }
                }

                // Si aucune correction n'a été nécessaire, toutes les cartes sont en place
                if (corrections.Count == 0) break;

                // Attendre que TOUTES les corrections parallèles soient terminées
                foreach (var co in corrections)
                    yield return co;

                // Laisser LateUpdate se stabiliser avant la prochaine vérification
                yield return null;
            }

            // Forcer la position, la taille et la rotation exactes sur toutes les cartes —
            // garantie absolue avant d'afficher les tampons.
            for (int i = 0; i < nb; i++)
            {
                if (cartes[i] == null) continue;
                cartes[i].Rt.anchoredPosition = slots[i].pos;
                cartes[i].Rt.sizeDelta        = slots[i].taille;
                cartes[i].Rt.localEulerAngles = Vector3.zero;
            }

            // Une frame de rendu pour que l'affichage soit à jour avant les tampons
            yield return null;

            yield return new WaitForSeconds(DELAI_AVANT_TAMPONS);

            // ── Phase 2 : Tamponnage lettre par lettre ───────────────────────
            string texteComplet = LIGNE_1 + LIGNE_2; // "GAMEOVER"

            // L'alpha monte de alphaInitial (déjà atteint) jusqu'à 1.0 sur nb tampons.
            // Chaque tampon déclenche une transition douce vers le palier suivant.
            for (int i = 0; i < nb; i++)
            {
                char lettre = i < texteComplet.Length ? texteComplet[i] : '?';

                yield return StartCoroutine(
                    TamponnerLettre(slots[i].pos, lettre, slots[i].taille));

                // Amplifier la vignette après chaque tampon (non bloquant)
                if (vignette != null)
                {
                    float progression = (i + 1f) / nb;
                    float alphaVoulu  = Mathf.Lerp(vignette.AlphaInitial, 1f, progression);
                    vignette.AnimerVersAlpha(alphaVoulu, delaiEntreTampons * 0.9f);
                }

                if (i < nb - 1)
                    yield return new WaitForSeconds(delaiEntreTampons);
            }

            // Réactiver le canvas MainDuGarde à la fin de l'animation
            if (mainDuGarde != null)
                mainDuGarde.gameObject.SetActive(true);

            // Attendre puis retourner sur la scène menu
            yield return new WaitForSeconds(délaiAvantMenu);
            OnAnimationTerminée?.Invoke();
        }

        /// <summary>Supprime les tampons visuels créés pendant l'animation et masque la vignette. Appelé lors d'un revive.</summary>
        public void Nettoyer()
        {

            foreach (Transform child in coucheGlissement)
            {
                if (child.name.StartsWith("Tampon_"))
                    Destroy(child.gameObject);
            }

            if (vignette != null)
                vignette.gameObject.SetActive(false);

            if (mainDuGarde != null)
                mainDuGarde.gameObject.SetActive(true);
        }


        // ── Tamponnage ────────────────────────────────────────────────────────

        /// <summary>
        /// Crée un conteneur enfant de coucheGlissement avec :
        ///   - TamponCirculaireUI (rendu en arrière-plan — effet tampon encreur)
        ///   - TextMeshProUGUI    (rendu au premier plan — la lettre)
        /// Le conteneur descend depuis le haut de la carte avec un fondu entrant,
        /// puis subit un scale-punch à l'impact.
        /// Reçoit directement la position du slot pour éviter tout accès sur carte.Rt
        /// (la carte peut avoir été détruite entre deux tampons).
        /// </summary>
        private IEnumerator TamponnerLettre(Vector2 posSlot, char lettre, Vector2 tailleCarte)
        {
            // ── Conteneur parent (gère position + scale pour les deux enfants) ─
            var container = new GameObject($"Tampon_{lettre}", typeof(RectTransform));
            container.transform.SetParent(coucheGlissement, false);
            container.layer = LayerMask.NameToLayer("UI");

            var rtC = container.GetComponent<RectTransform>();
            rtC.anchorMin = new Vector2(0.5f, 0.5f);
            rtC.anchorMax = new Vector2(0.5f, 0.5f);
            rtC.pivot     = new Vector2(0.5f, 0.5f);
            rtC.sizeDelta = tailleCarte;

            float inclinaison = Random.Range(-TAMPON_ROTATION_MAX, TAMPON_ROTATION_MAX);
            rtC.localEulerAngles = new Vector3(0f, 0f, inclinaison);

            // ── Anneau (enfant 0 — rendu derrière la lettre) ──────────────────
            var goAnneau = new GameObject("Anneau", typeof(RectTransform), typeof(CanvasRenderer));
            goAnneau.transform.SetParent(container.transform, false);
            goAnneau.layer = LayerMask.NameToLayer("UI");

            var rtA = goAnneau.GetComponent<RectTransform>();
            rtA.anchorMin = new Vector2(0.5f, 0.5f);
            rtA.anchorMax = new Vector2(0.5f, 0.5f);
            rtA.pivot     = new Vector2(0.5f, 0.5f);
            rtA.sizeDelta = tailleCarte;

            var anneau = goAnneau.AddComponent<TamponCirculaireUI>();
            anneau.color = new Color(couleurTexte.r, couleurTexte.g, couleurTexte.b, 0f);
            anneau.Initialiser(
                Random.Range(0, 99999),
                anneauRayon, anneauEpaisseur,
                anneauBaveExt, anneauBaveInt);

            // ── Lettre TMP (enfant 1 — rendu devant l'anneau) ─────────────────
            var goLettre = new GameObject("Lettre",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            goLettre.transform.SetParent(container.transform, false);
            goLettre.layer = LayerMask.NameToLayer("UI");

            var rtL = goLettre.GetComponent<RectTransform>();
            rtL.anchorMin = new Vector2(0.5f, 0.5f);
            rtL.anchorMax = new Vector2(0.5f, 0.5f);
            rtL.pivot     = new Vector2(0.5f, 0.5f);
            rtL.sizeDelta = tailleCarte;

            var tmp = goLettre.GetComponent<TextMeshProUGUI>();
            tmp.text               = lettre.ToString();
            tmp.fontSize           = taillePolice;
            tmp.fontStyle          = FontStyles.Bold;
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.textWrappingMode   = TextWrappingModes.NoWrap;
            tmp.color              = new Color(couleurTexte.r, couleurTexte.g, couleurTexte.b, 0f);
            if (fonteGameOver != null) tmp.font = fonteGameOver;

            // ── Positions de départ / cible depuis le slot (pas depuis carte.Rt) ──
            Vector2 posDepart = posSlot + new Vector2(0f, tailleCarte.y * 1.5f);
            Vector2 posCible  = posSlot;

            rtC.anchoredPosition = posDepart;
            rtC.localScale       = Vector3.one;

            // ── Phase 1 : descente rapide + apparition en fondu ───────────────
            float t = 0f;
            while (t < TAMPON_DUREE_DESCENTE)
            {
                t += Time.deltaTime;
                float p      = Mathf.SmoothStep(0f, 1f, t / TAMPON_DUREE_DESCENTE);
                Color cAlpha = new Color(couleurTexte.r, couleurTexte.g, couleurTexte.b, p);

                rtC.anchoredPosition = Vector2.Lerp(posDepart, posCible, p);
                tmp.color            = cAlpha;
                anneau.color         = cAlpha;
                yield return null;
            }

            rtC.anchoredPosition = posCible;
            Color cFull = new Color(couleurTexte.r, couleurTexte.g, couleurTexte.b, 1f);
            tmp.color    = cFull;
            anneau.color = cFull;

            // ── Phase 2 : impact scale-punch ──────────────────────────────────
            rtC.localScale = Vector3.one * TAMPON_ECRASE_SCALE;

            t = 0f;
            while (t < TAMPON_DUREE_REBOND)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / TAMPON_DUREE_REBOND);
                rtC.localScale = Vector3.one * Mathf.Lerp(TAMPON_ECRASE_SCALE, 1f, p);
                yield return null;
            }

            rtC.localScale = Vector3.one;
        }

        // ── Layout ────────────────────────────────────────────────────────────

        /// <summary>
        /// Calcule les 8 slots (position + taille) formant "GAME" (ligne 1) et "OVER" (ligne 2),
        /// centrés dans la coucheGlissement.
        /// </summary>
        private List<(Vector2, Vector2)> CalculerSlots()
        {
            var résultat = new List<(Vector2, Vector2)>();
            var taille   = new Vector2(largeurLettre, hauteurLigne);

            string[] lignes       = { LIGNE_1, LIGNE_2 };
            float    largMaxLigne = Mathf.Max(LIGNE_1.Length, LIGNE_2.Length);
            float    blocHauteur  = lignes.Length * hauteurLigne + (lignes.Length - 1) * espacementV;

            float yDepart = offsetY + blocHauteur * 0.5f - hauteurLigne * 0.5f;

            for (int l = 0; l < lignes.Length; l++)
            {
                int   nbLettres = lignes[l].Length;
                float largLigne = nbLettres * largeurLettre + (nbLettres - 1) * espacementH;
                float xDepart   = -largLigne * 0.5f + largeurLettre * 0.5f;
                float y         = yDepart - l * (hauteurLigne + espacementV);

                for (int c = 0; c < nbLettres; c++)
                {
                    float x = xDepart + c * (largeurLettre + espacementH);
                    résultat.Add((new Vector2(x, y), taille));
                }
            }

            return résultat;
        }
    }
}
