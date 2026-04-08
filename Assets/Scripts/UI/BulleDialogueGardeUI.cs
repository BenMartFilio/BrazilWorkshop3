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
            "Il vous faudra ces formulaires pour passer le prochain contrôle, souvenez-vous en !"
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
        [SerializeField] private AudioEventDispatcher audioEventDispatcher;
        [SerializeField] private AudioType _gardTalk;

        [Tooltip("L'autre bulle — celle qui prend le relais lors d'un changement d'état.")]
        [SerializeField] private BulleDialogueGardeUI       autresBulle;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float délaiDemande = 1.0f;

        // ── État interne ──────────────────────────────────────────────────────

        private CanvasGroup _canvasGroup;
        private Coroutine   _coroutineActive;
        private Coroutine   _coroutineMasquage;
        private Coroutine   _coroutineAlpha;    // ← coroutine AnimerAlpha en cours (fade-in ou fade-out)
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

        /// <summary>
        /// Affiche un message de désespoir directement dans cette bulle, sans tenir compte de l'état courant
        /// du garde ni de <see cref="estBulleRouge"/>. Utilisé par DetecteurSituationDesespérée
        /// pour forcer l'affichage du message "Vous n'avez pas les formulaires ?!" dans la bulle basse.
        /// </summary>
        public void AfficherMessageDesespoir(string message)
        {
            _barrageTerminé = true;
            if (_coroutineActive != null)
            {
                StopCoroutine(_coroutineActive);
                _coroutineActive = null;
            }
            LancerRéplicueDirecte(message, duréeAffichage: -1f);
        }

        /// <summary>
        /// Réinitialise la bulle et affiche la réplique "prochain poste".
        /// Appelé lors d'un revive, côté des deux bulles — seule la bulle active pour l'état courant s'affiche.
        /// </summary>
        public void AfficherSequenceSuivante()
        {
            // Réautoriser l'affichage (_barrageTerminé était true suite au OnPatienceEpuisée)
            _barrageTerminé = false;

            if (!EstActive) return;

            if (_coroutineActive != null)
            {
                StopCoroutine(_coroutineActive);
                _coroutineActive = null;
            }

            LancerRéplique(REPLIQUES_SEQUENCE_SUIVANTE, duréeAffichage: -1f);
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

            if (!doitEtreActive) return; // La bulle qui prend le relais gère le masquage de l'autre.

            // Je prends le relais — je masque l'autre bulle immédiatement avant d'apparaître.
            string texteAReprendre = autresBulle != null ? autresBulle._texteEnCours : null;
            autresBulle?.CéderLaParoleSansAnimation();

            if (_barrageTerminé)
            {
                if (!string.IsNullOrEmpty(texteAReprendre))
                    LancerRéplicueDirecte(texteAReprendre, duréeAffichage: -1f);
                return;
            }

            if (!string.IsNullOrEmpty(texteAReprendre))
                LancerRéplicueDirecte(texteAReprendre, duréeAffichage: -1f);
            else
                LancerRéplique(REPLIQUES_DEMANDE, duréeAffichage: -1f);
        }

        // ── API interne (appelée par l'autre bulle) ───────────────────────────

        /// <summary>
        /// Stoppe toute coroutine en cours, fait disparaître la bulle en fondu,
        /// puis la masque. Appelé quand l'autre bulle prend le relais depuis l'état Rouge
        /// vers Vert/Orange (fondu acceptable car pas de superposition).
        /// </summary>
        public void CéderLaParole()
        {
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
                _coroutineMasquage = StartCoroutine(FondirEtMasquer());
            else
                MasquerImmédiatement();

            _texteEnCours = null;
            _visible      = false;
        }

        /// <summary>
        /// Comme <see cref="CéderLaParole"/> mais masque la bulle instantanément, sans fondu.
        /// À utiliser lors des transitions vers l'état Rouge pour éviter le chevauchement
        /// visuel entre la bulle haute et la bulle basse.
        /// </summary>
        public void CéderLaParoleSansAnimation()
        {
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
            // Stopper AnimerAlpha avant MasquerImmédiatement — sinon elle écrase l'alpha à 1.
            if (_coroutineAlpha != null)
            {
                StopCoroutine(_coroutineAlpha);
                _coroutineAlpha = null;
            }

            MasquerImmédiatement();
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
            _coroutineAlpha    = null;
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

            if (audioEventDispatcher != null) audioEventDispatcher.PlayAudio(_gardTalk);

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

            _coroutineAlpha = StartCoroutine(AnimerAlpha(0f, 1f, DUREE_FONDU));
            yield return _coroutineAlpha;
            _coroutineAlpha = null;

            if (texteDialogue != null)
                texteDialogue.text = texte;

            if (duréeAffichage < 0f)
            {
                _coroutineActive = null;
                yield break;
            }

            yield return new WaitForSeconds(duréeAffichage);

            _coroutineAlpha = StartCoroutine(AnimerAlpha(1f, 0f, DUREE_FONDU));
            yield return _coroutineAlpha;
            _coroutineAlpha = null;

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
