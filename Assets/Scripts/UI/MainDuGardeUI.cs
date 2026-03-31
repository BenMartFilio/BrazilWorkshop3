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
        [Tooltip("ScriptableObject de session — contient la demande sauvegardée du barrage précédent.")]
        [SerializeField] private DonnéesSession donnéesSession;
        [Tooltip("Demande aléatoire de secours si aucune demande n'est sauvegardée en session.")]
        [SerializeField] private DemandeBarrage demandeAléatoire;

        // ── Animation positive ────────────────────────────────────────────────
        // (constantes déplacées dans AnimerPositif)

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

        /// <summary>Déclenché une seule fois quand toute la séquence du barrage est complétée.</summary>
        public event Action OnBarrageValidé;

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();

            bool aDemande = donnéesSession != null && donnéesSession.AUneDemandeSauvegardée;

            Debug.Log($"[MainDuGardeUI] Awake — donnéesSession={donnéesSession?.name ?? "NULL"}, " +
                      $"AUneDemandeSauvegardée={aDemande}");

            if (!aDemande)
            {
                Debug.Log("[MainDuGardeUI] Premier barrage détecté → composant désactivé (PremierBarrageController prend la main).");
                enabled = false;
                return;
            }

            if (donnéesSession != null)
            {
                Debug.Log($"[MainDuGardeUI] prochaineDemandeBarrage en session ({donnéesSession.prochaineDemandeBarrage?.Length ?? 0} entrées) : " +
                          (donnéesSession.prochaineDemandeBarrage?.Length > 0
                              ? string.Join(", ", donnéesSession.prochaineDemandeBarrage)
                              : "<vide>"));
            }

            if (listeAttenteGarde != null)
            {
                Debug.Log($"[MainDuGardeUI] Appel de ChargerDepuisSession sur '{listeAttenteGarde.name}'.");
                listeAttenteGarde.ChargerDepuisSession(donnéesSession, demandeAléatoire);
            }
            else
            {
                Debug.LogError("[MainDuGardeUI] listeAttenteGarde non assignée — impossible de charger la demande !");
            }
        }

        /// <summary>Reçoit un formulaire du système poche (FormulaireUI).</summary>
        public void RecevoirFormulaire(FormulaireUI formulaire)
            => RecevoirInterne(formulaire.Type, formulaire.gameObject);

        /// <summary>Reçoit un formulaire du système libre (FormulaireLibre).</summary>
        public void RecevoirFormulaire(FormulaireLibre formulaire)
            => RecevoirInterne(formulaire.Type, formulaire.gameObject);

        private void RecevoirInterne(FormulaireType type, GameObject go)
        {
            Debug.Log($"[MainDuGardeUI] RecevoirInterne({type}) — listeAttenteGarde={listeAttenteGarde?.name ?? "NULL"}, " +
                      $"enabled={enabled}");

            bool correct = listeAttenteGarde != null && listeAttenteGarde.ValiderProchain(type);

            if (correct)
            {
                OnFormulaireRemis?.Invoke(type);
                Debug.Log($"[MainDuGarde] ✓ Correct : {type}. EstTerminée={listeAttenteGarde.EstTerminée}");
                StartCoroutine(AnimerPositif(go));

                if (listeAttenteGarde.EstTerminée)
                {
                    Debug.Log("[MainDuGarde] ✓ Barrage validé — séquence complète. Déclenchement OnBarrageValidé.");
                    OnBarrageValidé?.Invoke();
                }
            }
            else
            {
                OnFormulaireIncorrect?.Invoke();
                Debug.LogWarning($"[MainDuGarde] ✗ Incorrect : {type} (non attendu ou déjà épuisé).");
                StartCoroutine(AnimerNégatif(go));
            }
        }

        // ── Animation positive ────────────────────────────────────────────────

        private const float POS_DUREE_PULSE   = 0.55f;  // s — durée du pulsé trémolo
        private const float POS_PULSE_SCALE   = 0.06f;  // amplitude relative du grow/shrink (±6 %)
        private const float POS_PULSE_FREQ    = 18f;    // Hz — vibration rapide façon trémolo
        private const float POS_WOBBLE_AMP    = 3f;     // ° — légère rotation oscillante
        private const float POS_WOBBLE_FREQ   = 9f;     // Hz — rotation rapide synchronisée
        private const float POS_DUREE_FADEOUT = 0.30f;  // s — fondu + rétrécissement de sortie
        private const float POS_SHRINK_FINAL  = 0.75f;  // échelle finale avant destruction

        private IEnumerator AnimerPositif(GameObject go)
        {
            if (go == null) yield break;

            var rt = go.GetComponent<RectTransform>();
            if (rt == null) { Destroy(go); yield break; }

            SetRaycast(go, false);

            // Immobiliser le formulaire exactement là où il a été lâché — pas de re-parentage.
            // Le formulaire reste visible dans la MainDuGarde pendant toute l'animation.
            Vector3 échelleInitiale = rt.localScale;
            Vector3 rotInitiale     = rt.localEulerAngles;

            // ── Phase 1 : trémolo (pulse + wobble) ──────────────────────────
            float t = 0f;
            while (t < POS_DUREE_PULSE)
            {
                if (go == null) yield break;
                t += Time.deltaTime;

                float ratio    = t / POS_DUREE_PULSE;
                // Enveloppe : monte vite, s'estompe progressivement vers la fin
                float envelope = Mathf.Sin(ratio * Mathf.PI);

                // Pulse scale : oscillation rapide autour de l'échelle initiale
                float pulseFactor = 1f + Mathf.Sin(t * POS_PULSE_FREQ * Mathf.PI * 2f)
                                       * POS_PULSE_SCALE * envelope;

                // Wobble rotation : légère oscillation angulaire décalée de π/2
                float wobbleAngle = Mathf.Sin(t * POS_WOBBLE_FREQ * Mathf.PI * 2f + Mathf.PI * 0.5f)
                                  * POS_WOBBLE_AMP * envelope;

                rt.localScale       = échelleInitiale * pulseFactor;
                rt.localEulerAngles = new Vector3(0f, 0f, rotInitiale.z + wobbleAngle);
                yield return null;
            }

            // ── Phase 2 : fondu + rétrécissement ────────────────────────────
            // Récupérer tous les Graphic pour le fondu alpha.
            var graphics = go.GetComponentsInChildren<Graphic>(true);

            // Capturer les alphas initiaux de chaque graphic pour un fondu proportionnel.
            float[] alphasInit = new float[graphics.Length];
            for (int i = 0; i < graphics.Length; i++)
                alphasInit[i] = graphics[i].color.a;

            t = 0f;
            while (t < POS_DUREE_FADEOUT)
            {
                if (go == null) yield break;
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / POS_DUREE_FADEOUT);

                // Rétrécissement doux vers POS_SHRINK_FINAL
                rt.localScale = échelleInitiale * Mathf.Lerp(1f, POS_SHRINK_FINAL, p);

                // Fondu alpha
                for (int i = 0; i < graphics.Length; i++)
                {
                    Color c = graphics[i].color;
                    c.a = Mathf.Lerp(alphasInit[i], 0f, p);
                    graphics[i].color = c;
                }

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
