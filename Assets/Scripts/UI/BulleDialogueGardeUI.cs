using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Barrage.Formulaires;

namespace Barrage.UI
{
    /// <summary>
    /// Affiche les répliques du garde dans la bulle de dialogue en fonction
    /// des événements du barrage : demande de documents, bon/mauvais document,
    /// barrage validé, barrage échoué (game over), séquence suivante.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class BulleDialogueGardeUI : MonoBehaviour
    {
        // ── Durées d'affichage ────────────────────────────────────────────────
        private const float DUREE_AFFICHAGE_DOCUMENT   = 2.5f;  // s — bon ou mauvais document
        private const float DUREE_AFFICHAGE_VALIDATION = 4.0f;  // s — barrage validé ou refusé
        private const float DUREE_FONDU                = 0.25f; // s — fondu in/out

        // ── Répliques par contexte ────────────────────────────────────────────

        private static readonly string[] REPLIQUES_DEMANDE = new[]
        {
            "Veuillez me fournir les documents requis.",
            "Fournissez-moi les documents requis."
        };

        private static readonly string[] REPLIQUES_BON_DOCUMENT = new[]
        {
            "Merci.",
            "Très bien."
        };

        private static readonly string[] REPLIQUES_MAUVAIS_DOCUMENT = new[]
        {
            "Je commence à croire que vous êtes suspect…",
            "Mauvais document.",
            "Ma patience a des limites."
        };

        private static readonly string[] REPLIQUES_BARRAGE_VALIDE = new[]
        {
            "Les documents sont conformes.",
            "Bonne route.",
            "C'est bon, vous pouvez passer."
        };

        private static readonly string[] REPLIQUES_BARRAGE_NON_VALIDE = new[]
        {
            "Sortez de la voiture, les mains en l'air, je vous arrête !"
        };

        private static readonly string[] REPLIQUES_SEQUENCE_SUIVANTE = new[]
        {
            "Au prochain poste, vous devrez présenter ces documents."
        };

        // ── Références ────────────────────────────────────────────────────────

        [Header("Références")]
        [Tooltip("Composant TextMeshProUGUI de la bulle de dialogue.")]
        [SerializeField] private TextMeshProUGUI texteDialogue;

        [Tooltip("MainDuGardeUI dont les événements pilotent les répliques.")]
        [SerializeField] private MainDuGardeUI mainDuGarde;

        [Tooltip("BarrePatience dont OnPatienceEpuisée déclenche la réplique d'arrestation.")]
        [SerializeField] private BarrePatience barrePatience;

        [Tooltip("AffichageProchaineDemandeUI dont OnBarrageValidé déclenche la réplique suivante. " +
                 "Laissez vide si non utilisé.")]
        [SerializeField] private AffichageProchaineDemandeUI affichageSuivant;

        [Header("Délai initial (demande des documents)")]
        [Tooltip("Délai en secondes avant d'afficher la première réplique de demande au démarrage.")]
        [SerializeField, Min(0f)] private float délaiDemande = 1.0f;

        // ── État interne ──────────────────────────────────────────────────────

        private CanvasGroup _canvasGroup;
        private Coroutine   _coroutineActive;
        private bool        _barrageTerminé;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private void Start()
        {
            // Afficher la première demande de documents après un court délai
            LancerRéplique(REPLIQUES_DEMANDE, duréeAffichage: -1f); // persistante jusqu'au 1er doc
        }

        private void OnEnable()
        {
            if (mainDuGarde != null)
            {
                mainDuGarde.OnFormulaireRemis    += OnFormulaireRemis;
                mainDuGarde.OnFormulaireIncorrect += OnFormulaireIncorrect;
                mainDuGarde.OnBarrageValidé       += OnBarrageValidé;
            }

            if (barrePatience != null)
                barrePatience.OnPatienceEpuisée += OnPatienceEpuisée;
        }

        private void OnDisable()
        {
            if (mainDuGarde != null)
            {
                mainDuGarde.OnFormulaireRemis    -= OnFormulaireRemis;
                mainDuGarde.OnFormulaireIncorrect -= OnFormulaireIncorrect;
                mainDuGarde.OnBarrageValidé       -= OnBarrageValidé;
            }

            if (barrePatience != null)
                barrePatience.OnPatienceEpuisée -= OnPatienceEpuisée;
        }

        // ── Handlers d'événements ─────────────────────────────────────────────

        private void OnFormulaireRemis(FormulaireType _)
        {
            if (_barrageTerminé) return;

            LancerRéplique(REPLIQUES_BON_DOCUMENT, DUREE_AFFICHAGE_DOCUMENT,
                rappel: () =>
                {
                    // Revenir à la réplique de demande si le barrage n'est pas encore terminé
                    if (!_barrageTerminé)
                        LancerRéplique(REPLIQUES_DEMANDE, duréeAffichage: -1f);
                });
        }

        private void OnFormulaireIncorrect()
        {
            if (_barrageTerminé) return;

            LancerRéplique(REPLIQUES_MAUVAIS_DOCUMENT, DUREE_AFFICHAGE_DOCUMENT,
                rappel: () =>
                {
                    if (!_barrageTerminé)
                        LancerRéplique(REPLIQUES_DEMANDE, duréeAffichage: -1f);
                });
        }

        private void OnBarrageValidé()
        {
            _barrageTerminé = true;
            LancerRéplique(REPLIQUES_BARRAGE_VALIDE, DUREE_AFFICHAGE_VALIDATION,
                rappel: () => LancerRéplique(REPLIQUES_SEQUENCE_SUIVANTE, duréeAffichage: -1f));
        }

        private void OnPatienceEpuisée()
        {
            _barrageTerminé = true;
            LancerRéplique(REPLIQUES_BARRAGE_NON_VALIDE, duréeAffichage: -1f);
        }

        // ── Affichage ─────────────────────────────────────────────────────────

        /// <summary>
        /// Lance l'affichage d'une réplique aléatoire du tableau donné.
        /// Si <paramref name="duréeAffichage"/> est négatif, la bulle reste visible indéfiniment.
        /// <paramref name="rappel"/> est invoqué après la disparition (si durée positive).
        /// </summary>
        private void LancerRéplique(string[] répliques, float duréeAffichage, Action rappel = null)
        {
            if (_coroutineActive != null)
                StopCoroutine(_coroutineActive);

            string texte = ChoisirAléatoire(répliques);
            _coroutineActive = StartCoroutine(AfficherRéplique(texte, duréeAffichage, rappel));
        }

        private IEnumerator AfficherRéplique(string texte, float duréeAffichage, Action rappel)
        {
            // Fondu entrant
            yield return StartCoroutine(AnimerAlpha(0f, 1f, DUREE_FONDU));

            if (texteDialogue != null)
                texteDialogue.text = texte;

            if (duréeAffichage < 0f)
            {
                // Persistent — on reste visible sans timer
                _coroutineActive = null;
                yield break;
            }

            yield return new WaitForSeconds(duréeAffichage);

            // Fondu sortant
            yield return StartCoroutine(AnimerAlpha(1f, 0f, DUREE_FONDU));

            _coroutineActive = null;
            rappel?.Invoke();
        }

        private IEnumerator AnimerAlpha(float de, float vers, float durée)
        {
            float t = 0f;
            while (t < durée)
            {
                t += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(de, vers, Mathf.SmoothStep(0f, 1f, t / durée));
                yield return null;
            }
            _canvasGroup.alpha = vers;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string ChoisirAléatoire(string[] répliques)
        {
            if (répliques == null || répliques.Length == 0) return string.Empty;
            return répliques[UnityEngine.Random.Range(0, répliques.Length)];
        }
    }
}
