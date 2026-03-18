using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Barrage.Formulaires;

namespace Barrage.UI
{
    /// <summary>
    /// Zone de dépôt représentant la main du garde.
    /// Valide l'ordre de remise selon la séquence de ListeAttenteGarde.
    /// - Bon type au bon moment → animation positive (grossissement doux + agitation gentille).
    /// - Mauvais type ou mauvais ordre → animation négative (rebonds violents 1 s dans la PartieHaute).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MainDuGardeUI : MonoBehaviour
    {
        [Header("Zones UI")]
        [Tooltip("RectTransform de la PartieHaute, zone de rebond pour l'animation négative.")]
        [SerializeField] private RectTransform partieHaute;

        [Header("Validation")]
        [Tooltip("ScriptableObject définissant la séquence ordonnée des formulaires attendus.")]
        [SerializeField] private ListeAttenteGarde listeAttenteGarde;

        // ── Animation positive ────────────────────────────────────────────────
        private const float POS_ECHELLE_MAX  = 1.25f;  // pic de grossissement
        private const float POS_DUREE_MONTEE = 0.15f;  // s vers le pic
        private const float POS_DUREE_WOBBLE = 0.60f;  // s d'agitation douce
        private const float POS_WOBBLE_AMP   = 8f;     // degrés — gentil
        private const float POS_WOBBLE_FREQ  = 3.5f;   // Hz

        // ── Animation négative ────────────────────────────────────────────────
        private const float NEG_VITESSE_INITIALE  = 3200f; // px/s — très violent
        private const float NEG_REBOND_COEFF      = 0.97f; // quasi sans perte d'énergie
        private const float NEG_REBOND_BOOST      = 1.08f; // multiplicateur supplémentaire à chaque impact
        private const float NEG_DUREE             = 1.0f;  // s — durée fixe avant destruction
        private const float NEG_ROTATION_COEFF    = 0.35f; // vrille agressive à chaque impact

        public RectTransform RectTransform { get; private set; }

        /// <summary>Déclenché lorsqu'un formulaire est correctement remis au garde (bon type, bon ordre).</summary>
        public event Action<FormulaireType> OnFormulaireRemis;

        /// <summary>Déclenché lorsqu'un formulaire incorrect ou dans le mauvais ordre est remis.</summary>
        public event Action OnFormulaireIncorrect;

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
            listeAttenteGarde?.Réinitialiser();
        }

        /// <summary>Reçoit un formulaire du système poche (FormulaireUI).</summary>
        public void RecevoirFormulaire(FormulaireUI formulaire)
            => RecevoirInterne(formulaire.Type, formulaire.gameObject);

        /// <summary>Reçoit un formulaire du système libre (FormulaireLibre).</summary>
        public void RecevoirFormulaire(FormulaireLibre formulaire)
            => RecevoirInterne(formulaire.Type, formulaire.gameObject);

        private void RecevoirInterne(FormulaireType type, GameObject go)
        {
            bool correct = listeAttenteGarde != null && listeAttenteGarde.ValiderProchain(type);

            if (correct)
            {
                OnFormulaireRemis?.Invoke(type);
                Debug.Log($"[MainDuGarde] ✓ Correct : {type}");
                StartCoroutine(AnimerPositif(go));
            }
            else
            {
                OnFormulaireIncorrect?.Invoke();
                Debug.Log($"[MainDuGarde] ✗ Incorrect ou mauvais ordre : {type}");
                StartCoroutine(AnimerNégatif(go));
            }
        }

        // ── Animation positive ────────────────────────────────────────────────

        private IEnumerator AnimerPositif(GameObject go)
        {
            if (go == null) yield break;

            var rt = go.GetComponent<RectTransform>();
            if (rt == null) { Destroy(go); yield break; }

            SetRaycast(go, false);

            // Reparenter dans PartieHaute en preservant la position visuelle exacte.
            // worldPositionStays = true : Unity recalcule anchoredPosition automatiquement.
            if (partieHaute != null)
                rt.SetParent(partieHaute, true);

            Vector3 échelleInitiale = rt.localScale;

            // Phase 1 : grossissement doux
            float t = 0f;
            while (t < POS_DUREE_MONTEE)
            {
                if (go == null) yield break;
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / POS_DUREE_MONTEE);
                rt.localScale = échelleInitiale * Mathf.Lerp(1f, POS_ECHELLE_MAX, p);
                yield return null;
            }

            // Phase 2 : agitation gentille avec retour progressif à l'échelle initiale
            t = 0f;
            while (t < POS_DUREE_WOBBLE)
            {
                if (go == null) yield break;
                t += Time.deltaTime;
                float ratio    = t / POS_DUREE_WOBBLE;
                float décroiss = 1f - ratio;
                float angle    = Mathf.Sin(t * POS_WOBBLE_FREQ * Mathf.PI * 2f) * POS_WOBBLE_AMP * décroiss;
                float échelle  = Mathf.Lerp(POS_ECHELLE_MAX, 1f, ratio);

                rt.localScale       = échelleInitiale * échelle;
                rt.localEulerAngles = new Vector3(0f, 0f, angle);
                yield return null;
            }

            if (go != null) Destroy(go);
        }

        // ── Animation négative ────────────────────────────────────────────────

        private IEnumerator AnimerNégatif(GameObject go)
        {
            if (go == null) yield break;

            var rt = go.GetComponent<RectTransform>();
            if (rt == null) { Destroy(go); yield break; }

            if (partieHaute == null)
            {
                Debug.LogWarning("[MainDuGardeUI] partieHaute non assignée — animation négative ignorée.");
                Destroy(go);
                yield break;
            }

            SetRaycast(go, false);

            // Reparenter dans PartieHaute en conservant la position visuelle
            Vector3 positionMonde = rt.position;
            rt.SetParent(partieHaute, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.position  = positionMonde;

            // Direction initiale aléatoire avec forte composante (angle 25°-65°)
            float angleDir = UnityEngine.Random.Range(25f, 65f);
            if (UnityEngine.Random.value > 0.5f) angleDir = 180f - angleDir;

            Vector2 vitesse = new Vector2(
                Mathf.Cos(angleDir * Mathf.Deg2Rad),
                Mathf.Sin(angleDir * Mathf.Deg2Rad)) * NEG_VITESSE_INITIALE;

            float hw    = rt.sizeDelta.x * 0.5f;
            float hh    = rt.sizeDelta.y * 0.5f;
            float temps = 0f;

            while (temps < NEG_DUREE && go != null)
            {
                temps += Time.deltaTime;

                // Rect recalculé chaque frame (PartieHaute peut être dynamique)
                Rect bounds = partieHaute.rect;
                Vector2 pos = rt.anchoredPosition + vitesse * Time.deltaTime;

                // Rebond horizontal
                if (pos.x - hw < bounds.xMin)
                {
                    pos.x     = bounds.xMin + hw;
                    vitesse.x = Mathf.Abs(vitesse.x) * NEG_REBOND_COEFF * NEG_REBOND_BOOST;
                }
                else if (pos.x + hw > bounds.xMax)
                {
                    pos.x     = bounds.xMax - hw;
                    vitesse.x = -Mathf.Abs(vitesse.x) * NEG_REBOND_COEFF * NEG_REBOND_BOOST;
                }

                // Rebond vertical
                if (pos.y - hh < bounds.yMin)
                {
                    pos.y     = bounds.yMin + hh;
                    vitesse.y = Mathf.Abs(vitesse.y) * NEG_REBOND_COEFF * NEG_REBOND_BOOST;
                }
                else if (pos.y + hh > bounds.yMax)
                {
                    pos.y     = bounds.yMax - hh;
                    vitesse.y = -Mathf.Abs(vitesse.y) * NEG_REBOND_COEFF * NEG_REBOND_BOOST;
                }

                rt.anchoredPosition  = pos;
                rt.localEulerAngles += new Vector3(0f, 0f, vitesse.x * Time.deltaTime * NEG_ROTATION_COEFF);

                yield return null;
            }

            if (go != null) Destroy(go);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetRaycast(GameObject go, bool actif)
        {
            foreach (var graphic in go.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = actif;
        }
    }
}
