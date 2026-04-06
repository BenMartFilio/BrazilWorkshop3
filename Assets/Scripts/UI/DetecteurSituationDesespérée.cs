using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barrage.Formulaires;

namespace Barrage.UI
{
    /// <summary>
    /// Détecte au démarrage du barrage si le joueur est dans une situation sans issue :
    /// aucun formulaire requis dans son inventaire ET aucun objet bonus salvateur
    /// (FormulairePasePartout, BadgeDuGouvernement).
    ///
    /// Si la situation est désespérée :
    ///   1. Le sprite du garde bascule immédiatement sur GardeLow.
    ///   2. La bulle basse affiche "Vous n'avez pas les formulaires ?!" pendant une seconde.
    ///   3. Le GestionnaireGameOver de la scène Barrage se déclenche.
    ///
    /// Ce composant ne s'active PAS au premier barrage (pas de demande sauvegardée en session).
    ///
    /// Ordre d'exécution : -90
    ///   - Après BulleDialogueGardeUI (-100) qui lit AUneDemandeSauvegardée.
    ///   - Avant MainDuGardeUI (-50) qui appelle ChargerDepuisSession() et efface prochaineDemandeBarrage.
    ///   → On peut capturer les besoins dans Awake avant qu'ils soient effacés.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class DetecteurSituationDesespérée : MonoBehaviour
    {
        private const string MESSAGE_DESESPOIR    = "Vous n'avez pas les formulaires ?!";
        private const float  DELAI_AVANT_GAMEOVER = 1f;

        [Header("Données")]
        [Tooltip("Session courante — permet de lire la demande avant qu'elle ne soit effacée par MainDuGardeUI.")]
        [SerializeField] private DonnéesSession donnéesSession;

        [Tooltip("Inventaire formulaires du joueur (standards uniquement).")]
        [SerializeField] private FormulaireInventaire inventaire;

        [Tooltip("Données joueur — pour lire les quantités de FormulairePasePartout et BadgeDuGouvernement.")]
        [SerializeField] private SO_PlayerDatas donneesJoueur;

        [Header("Visuels garde")]
        [Tooltip("VisuelGardeUI pour forcer l'état Rouge (GardeLow) immédiatement.")]
        [SerializeField] private VisuelGardeUI visuelGarde;

        [Tooltip("BulleDialogueGardeUI basse (estBulleRouge=true) qui affichera le message de désespoir.")]
        [SerializeField] private BulleDialogueGardeUI bulleBasse;

        [Header("Game Over")]
        [Tooltip("GestionnaireGameOver à déclencher après le délai.")]
        [SerializeField] private GestionnaireGameOver gestionnaireGameOver;

        [Header("Patience")]
        [Tooltip("BarrePatience à geler immédiatement pour éviter un double déclenchement par timeout.")]
        [SerializeField] private BarrePatience barrePatience;

        // Besoins capturés dans Awake avant que ChargerDepuisSession() efface prochaineDemandeBarrage.
        private Dictionary<FormulaireType, int> _besoinsCapturés;
        private bool _estPremierBarrage;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _estPremierBarrage = donnéesSession == null || !donnéesSession.AUneDemandeSauvegardée;

            if (_estPremierBarrage)
            {
                Debug.Log("[DetecteurSituationDesespérée] Premier barrage — aucune détection.");
                return;
            }

            // Capturer ici la demande brute AVANT que MainDuGardeUI.Awake() (-50) ne l'efface.
            _besoinsCapturés = CapturerBesoins(donnéesSession.prochaineDemandeBarrage);

            Debug.Log($"[DetecteurSituationDesespérée] Besoins capturés ({_besoinsCapturés.Count} types) : " +
                      string.Join(", ", FormatBesoins(_besoinsCapturés)));
        }

        private void Start()
        {
            if (_estPremierBarrage) return;

            if (EstSituationDesespérée())
            {
                Debug.Log("[DetecteurSituationDesespérée] Situation désespérée — déclenchement de la séquence.");
                StartCoroutine(SéquenceDesespoir());
            }
        }

        // ── Détection ─────────────────────────────────────────────────────────

        /// <summary>
        /// Retourne true si le joueur ne peut satisfaire aucun des formulaires requis
        /// et ne possède aucun objet salvateur.
        /// </summary>
        private bool EstSituationDesespérée()
        {
            if (PossedeObjetSalvateur())
            {
                Debug.Log("[DetecteurSituationDesespérée] Objet salvateur détecté — situation non désespérée.");
                return false;
            }

            if (inventaire == null || _besoinsCapturés == null || _besoinsCapturés.Count == 0)
            {
                Debug.LogWarning("[DetecteurSituationDesespérée] Inventaire ou besoins manquants — détection ignorée.");
                return false;
            }

            foreach (var kvp in _besoinsCapturés)
            {
                int possédé = inventaire.ObtenirQuantité(kvp.Key);
                if (possédé < kvp.Value)
                {
                    Debug.Log($"[DetecteurSituationDesespérée] Formulaire manquant : {kvp.Key} " +
                              $"(requis={kvp.Value}, possédé={possédé})");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Retourne true si le joueur possède au moins un FormulairePasePartout
        /// ou BadgeDuGouvernement dans SO_PlayerDatas.
        /// </summary>
        private bool PossedeObjetSalvateur()
        {
            if (donneesJoueur == null) return false;

            foreach (InventoryEntry entree in donneesJoueur.ObtenirInventairePlat())
            {
                if (entree.quantity <= 0) continue;

                if (entree.objectName == "FormulairePasePartout" ||
                    entree.objectName == "BadgeDuGouvernement")
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Construit le dictionnaire type→quantité requise depuis le snapshot brut de session.
        /// Ignore les objets spéciaux non demandables au barrage.
        /// </summary>
        private static Dictionary<FormulaireType, int> CapturerBesoins(FormulaireType[] demande)
        {
            var besoins = new Dictionary<FormulaireType, int>();

            if (demande == null) return besoins;

            foreach (FormulaireType type in demande)
            {
                if (EstObjetSpécial(type)) continue;

                if (!besoins.ContainsKey(type))
                    besoins[type] = 0;
                besoins[type]++;
            }

            return besoins;
        }

        private static bool EstObjetSpécial(FormulaireType type)
            => type == FormulaireType.LiasseDeBillets
            || type == FormulaireType.FormulairePasePartout
            || type == FormulaireType.BadgeDuGouvernement;

        // ── Séquence désespoir ────────────────────────────────────────────────

        private IEnumerator SéquenceDesespoir()
        {
            // 1. Geler la patience immédiatement pour éviter un double game over par timeout.
            barrePatience?.Geler();

            // 2. Forcer l'état Rouge sur VisuelGardeUI — met à jour EtatCourant ET émet OnEtatChange,
            //    ce qui fait basculer les bulles vers la bonne configuration (bulle haute cède, bulle basse prend le relais).
            if (visuelGarde != null)
                visuelGarde.ForcerEtatRouge();
            else
                Debug.LogWarning("[DetecteurSituationDesespérée] visuelGarde non assigné.");

            // 3. Afficher le message dans la bulle basse — APRÈS ForcerEtatRouge pour que EstActive == true.
            AfficherBulleDesespoir();

            // 4. Attendre avant le game over.
            yield return new WaitForSeconds(DELAI_AVANT_GAMEOVER);

            // 5. Déclencher le game over.
            gestionnaireGameOver?.Déclencher();
        }

        /// <summary>
        /// Affiche le message de désespoir dans la bulle basse via son API dédiée.
        /// </summary>
        private void AfficherBulleDesespoir()
        {
            if (bulleBasse == null)
            {
                Debug.LogWarning("[DetecteurSituationDesespérée] bulleBasse non assignée.");
                return;
            }

            bulleBasse.AfficherMessageDesespoir(MESSAGE_DESESPOIR);
        }

        // ── Helpers debug ─────────────────────────────────────────────────────

        private static IEnumerable<string> FormatBesoins(Dictionary<FormulaireType, int> besoins)
        {
            foreach (var kvp in besoins)
                yield return $"{kvp.Key}×{kvp.Value}";
        }
    }
}
