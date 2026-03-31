using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Barrage.Formulaires;

namespace Barrage.UI
{
    /// <summary>
    /// Affiche les répliques du garde dans une bulle de dialogue.
    ///
    /// Deux instances coexistent dans la scène :
    ///   - Bulle haute (<see cref="estBulleRouge"/> = false) → active pour les états Vert et Orange.
    ///   - Bulle basse (<see cref="estBulleRouge"/> = true)  → active uniquement pour l'état Rouge.
    ///
    /// <see cref="VisuelGardeUI.OnEtatChange"/> pilote le basculement entre les deux bulles.
    /// Lors d'une transition, la réplique en cours est transférée à la bulle qui prend le relais.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    // Priorité -100 : Awake() doit lire AUneDemandeSauvegardée
    // avant que MainDuGardeUI (ordre 0) n'appelle ChargerDepuisSession() qui l'efface.
    [DefaultExecutionOrder(-100)]
    public class BulleDialogueGardeUI : MonoBehaviour
    {
        // ── Durées d'affichage ────────────────────────────────────────────────
        private const float DUREE_AFFICHAGE_DOCUMENT   = 2.5f;
        private const float DUREE_AFFICHAGE_VALIDATION = 4.0f;
        private const float DUREE_FONDU                = 0.25f;

        // ── Répliques ─────────────────────────────────────────────────────────

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

        private static readonly string[] REPLIQUES_PREMIER_BARRAGE = new[]
        {
            "Il vous faudrait ces formulaires pour passer le prochain contrôle, souvenez vous en !"
        };

        // ── Champs sérialisés ─────────────────────────────────────────────────

        [Header("Identité de la bulle")]
        [Tooltip("Coché sur la bulle BASSE (état Rouge). Décoché sur la bulle HAUTE (états Vert + Orange).")]
        [SerializeField] private bool estBulleRouge;

        [Header("Références")]
        [SerializeField] private TextMeshProUGUI            texteDialogue;
        [SerializeField] private MainDuGardeUI              mainDuGarde;
        [SerializeField] private BarrePatience              barrePatience;
        [SerializeField] private AffichageProchaineDemandeUI affichageSuivant;
        [SerializeField] private VisuelGardeUI              visuelGarde;
        [SerializeField] private DonnéesSession             donnéesSession;

        [Tooltip("L'autre bulle — celle qui prend le relais lors d'un changement d'état.")]
        [SerializeField] private BulleDialogueGardeUI       autresBulle;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float délaiDemande = 1.0f;

        // ── État interne ──────────────────────────────────────────────────────

        private CanvasGroup _canvasGroup;
        private Coroutine   _coroutineActive;
        private Coroutine   _coroutineMasquage;
        private bool        _barrageTerminé;
        private bool        _visible;
        private bool        _estPremierBarrage; // capturé dans Awake avant effacement

        private string  _texteEnCours;
        private float   _duréeRestante = -1f;

        // ── Propriété ─────────────────────────────────────────────────────────

        /// <summary>True si cette bulle est actuellement la bulle active.</summary>
        public bool EstActive => EstBulleActivePourEtat(visuelGarde != null
            ? visuelGarde.EtatCourant
            : VisuelGardeUI.EtatGarde.Vert);

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            MasquerImmédiatement();

            // Lire ici — avant que MainDuGardeUI.Awake() n'appelle
            // ListeAttenteGarde.ChargerDepuisSession() qui efface prochaineDemandeBarrage.
            _estPremierBarrage = donnéesSession == null || !donnéesSession.AUneDemandeSauvegardée;
        }

        private void Start()
        {
            if (!EstActive) return;

            if (_estPremierBarrage)
                LancerRéplique(REPLIQUES_PREMIER_BARRAGE, duréeAffichage: -1f);
            else
                LancerRéplique(REPLIQUES_DEMANDE, duréeAffichage: -1f);
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

            if (visuelGarde != null)
                visuelGarde.OnEtatChange += OnEtatGardeChange;
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

            if (visuelGarde != null)
                visuelGarde.OnEtatChange -= OnEtatGardeChange;
        }

        // ── Handlers d'événements ─────────────────────────────────────────────

        private void OnFormulaireRemis(FormulaireType _)
        {
            if (_barrageTerminé || !EstActive) return;

            LancerRéplique(REPLIQUES_BON_DOCUMENT, DUREE_AFFICHAGE_DOCUMENT,
                rappel: () =>
                {
                    if (!_barrageTerminé && EstActive)
                        LancerRéplique(REPLIQUES_DEMANDE, duréeAffichage: -1f);
                });
        }

        private void OnFormulaireIncorrect()
        {
            if (_barrageTerminé || !EstActive) return;

            LancerRéplique(REPLIQUES_MAUVAIS_DOCUMENT, DUREE_AFFICHAGE_DOCUMENT,
                rappel: () =>
                {
                    if (!_barrageTerminé && EstActive)
                        LancerRéplique(REPLIQUES_DEMANDE, duréeAffichage: -1f);
                });
        }

        private void OnBarrageValidé()
        {
            // Marquer les deux bulles : l'une est active mais l'autre doit aussi savoir
            // que le barrage est terminé en cas de transition d'état imminente.
            _barrageTerminé = true;
            if (!EstActive) return;

            LancerRéplique(REPLIQUES_BARRAGE_VALIDE, DUREE_AFFICHAGE_VALIDATION,
                rappel: () => LancerRéplique(REPLIQUES_SEQUENCE_SUIVANTE, duréeAffichage: -1f));
        }

        private void OnPatienceEpuisée()
        {
            _barrageTerminé = true;
            if (!EstActive) return;

            LancerRéplique(REPLIQUES_BARRAGE_NON_VALIDE, duréeAffichage: -1f);
        }

        /// <summary>
        /// Appelé par <see cref="VisuelGardeUI"/> lors d'un changement d'état.
        /// La bulle qui devient active reprend la réplique en cours de l'autre.
        /// </summary>
        private void OnEtatGardeChange(VisuelGardeUI.EtatGarde nouvelEtat)
        {
            bool doitEtreActive = EstBulleActivePourEtat(nouvelEtat);

            if (doitEtreActive)
            {
                // Je prends le relais — je reprends le texte de l'autre bulle
                string texteAReprendre = autresBulle != null ? autresBulle._texteEnCours : null;
                autresBulle?.CéderLaParole();

                if (!string.IsNullOrEmpty(texteAReprendre))
                    LancerRéplicueDirecte(texteAReprendre, duréeAffichage: -1f);
                else if (!_barrageTerminé)
                    LancerRéplique(REPLIQUES_DEMANDE, duréeAffichage: -1f);
            }
            else
            {
                // Je cède la parole — l'autre bulle s'en chargera
                CéderLaParole();
            }
        }

        // ── API interne (appelée par l'autre bulle) ───────────────────────────

        /// <summary>
        /// Stoppe toute coroutine en cours, fait disparaître la bulle en fondu,
        /// puis la masque. Appelé quand l'autre bulle prend le relais.
        /// </summary>
        public void CéderLaParole()
        {
            // Arrêter toute coroutine de dialogue ou de masquage en cours
            if (_coroutineActive != null)
            {
                StopCoroutine(_coroutineActive);
                _coroutineActive = null;
            }
            if (_coroutineMasquage != null)
            {
                StopCoroutine(_coroutineMasquage);
                _coroutineMasquage = null;
            }

            if (_visible)
            {
                // Fondu sortant : la bulle était visible, on la fait disparaître
                _coroutineMasquage = StartCoroutine(FondirEtMasquer());
            }
            else
            {
                MasquerImmédiatement();
            }

            _texteEnCours = null;
            _visible      = false;
        }

        private IEnumerator FondirEtMasquer()
        {
            float alphaDepart = _canvasGroup.alpha;
            float t = 0f;
            while (t < DUREE_FONDU)
            {
                t += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(alphaDepart, 0f, Mathf.SmoothStep(0f, 1f, t / DUREE_FONDU));
                yield return null;
            }
            MasquerImmédiatement();
            _coroutineMasquage = null;
        }

        // ── Affichage ─────────────────────────────────────────────────────────

        private void LancerRéplique(string[] répliques, float duréeAffichage, Action rappel = null)
        {
            string texte = ChoisirAléatoire(répliques);
            LancerRéplicueDirecte(texte, duréeAffichage, rappel);
        }

        private void LancerRéplicueDirecte(string texte, float duréeAffichage, Action rappel = null)
        {
            if (_coroutineActive != null)
                StopCoroutine(_coroutineActive);

            _texteEnCours  = texte;
            _duréeRestante = duréeAffichage;
            _coroutineActive = StartCoroutine(AfficherRéplique(texte, duréeAffichage, rappel));
        }

        private IEnumerator AfficherRéplique(string texte, float duréeAffichage, Action rappel)
        {
            _visible = true;

            // Annuler un éventuel fondu sortant encore en cours
            if (_coroutineMasquage != null)
            {
                StopCoroutine(_coroutineMasquage);
                _coroutineMasquage = null;
            }

            yield return StartCoroutine(AnimerAlpha(0f, 1f, DUREE_FONDU));

            if (texteDialogue != null)
                texteDialogue.text = texte;

            if (duréeAffichage < 0f)
            {
                // Persistant — coroutine terminée mais la bulle reste visible
                _coroutineActive = null;
                yield break;
            }

            yield return new WaitForSeconds(duréeAffichage);

            yield return StartCoroutine(AnimerAlpha(1f, 0f, DUREE_FONDU));

            _texteEnCours    = null;
            _visible         = false;
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

        private void MasquerImmédiatement()
        {
            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Détermine si cette bulle doit être active pour un état donné du garde.
        /// </summary>
        private bool EstBulleActivePourEtat(VisuelGardeUI.EtatGarde etat)
        {
            return estBulleRouge
                ? etat == VisuelGardeUI.EtatGarde.Rouge
                : etat != VisuelGardeUI.EtatGarde.Rouge;
        }

        private static string ChoisirAléatoire(string[] répliques)
        {
            if (répliques == null || répliques.Length == 0) return string.Empty;
            return répliques[UnityEngine.Random.Range(0, répliques.Length)];
        }
    }
}
