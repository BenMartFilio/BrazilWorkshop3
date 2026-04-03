using System.Collections.Generic;
using UnityEngine;

namespace Barrage.Formulaires
{
    /// <summary>
    /// ScriptableObject définissant l'ensemble des formulaires à remettre au garde.
    /// La validation est basée sur les quantités restantes par type :
    /// le joueur peut soumettre les formulaires dans n'importe quel ordre,
    /// tant qu'il fournit le bon type et que la quantité demandée n'est pas épuisée.
    ///
    /// Les données sérialisées (<see cref="séquenceFallback"/>) servent uniquement de
    /// valeur de secours. La demande active est toujours stockée dans <see cref="_comptesRuntime"/>
    /// afin de ne jamais modifier le contenu de l'asset sur disque.
    /// </summary>
    [CreateAssetMenu(fileName = "ListeAttenteGarde", menuName = "Barrage/Liste Attente Garde")]
    public class ListeAttenteGarde : ScriptableObject
    {
        [Tooltip("Séquence de secours utilisée uniquement si aucune demande session ni DemandeBarrage n'est disponible.")]
        [SerializeField] private List<FormulaireType> séquenceFallback = new();

        // Quantités restantes par type — jamais persistées sur disque.
        private readonly Dictionary<FormulaireType, int> _comptesRuntime = new();

        // Nombre total de formulaires encore à remettre (toutes types confondus).
        private int _restants;

        /// <summary>Remet tous les compteurs à zéro.</summary>
        public void Réinitialiser()
        {
            _comptesRuntime.Clear();
            _restants = 0;
        }

        /// <summary>
        /// Valide le formulaire remis.
        /// Retourne true si ce type figure encore dans la demande (indépendamment de l'ordre).
        /// Retourne false si le type n'est pas attendu ou si sa quantité est déjà épuisée.
        /// </summary>
        public bool ValiderProchain(FormulaireType type)
        {
            Debug.Log($"[ListeAttenteGarde] ValiderProchain({type}) appelé. " +
                      $"_restants={_restants}, état : {DescriptionRestants()}");

            if (!_comptesRuntime.TryGetValue(type, out int compte) || compte <= 0)
            {
                Debug.LogWarning($"[ListeAttenteGarde] ✗ Type non attendu ou déjà épuisé : {type}. " +
                          $"Restants ({_restants}) : {DescriptionRestants()}");
                return false;
            }

            _comptesRuntime[type] = compte - 1;
            _restants--;
            Debug.Log($"[ListeAttenteGarde] ✓ Accepté : {type}. Restants après ({_restants}) : {DescriptionRestants()}");
            return true;
        }

        /// <summary>True quand tous les formulaires demandés ont été remis.</summary>
        public bool EstTerminée => _restants <= 0;

        /// <summary>
        /// Retourne le premier type de formulaire encore attendu, ou null si la séquence est terminée.
        /// Utilisé par ValiderAvecPassePartout pour identifier le prochain type à valider.
        /// </summary>
        public FormulaireType? ProchainTypeRestant()
        {
            foreach (var kv in _comptesRuntime)
            {
                if (kv.Value > 0)
                    return kv.Key;
            }
            return null;
        }

        /// <summary>
        /// Charge la demande runtime depuis les données de session sauvegardées (demande du barrage précédent).
        /// Si aucune demande n'est sauvegardée, charge depuis <paramref name="demandeAléatoire"/>
        /// en la régénérant avec le bon palier de difficulté.
        /// Ne modifie jamais les données sérialisées de l'asset.
        /// </summary>
        /// <param name="donnees">Session courante — contient prochaineDemandeBarrage et nombreBarragesComplétés.</param>
        /// <param name="demandeAléatoire">Utilisé en fallback si aucune demande n'est sauvegardée en session.</param>
        public void ChargerDepuisSession(DonnéesSession donnees, DemandeBarrage demandeAléatoire)
        {
            _comptesRuntime.Clear();
            _restants = 0;

            if (donnees != null && donnees.AUneDemandeSauvegardée)
            {
                Debug.Log($"[ListeAttenteGarde] ChargerDepuisSession — données trouvées dans session " +
                          $"({donnees.prochaineDemandeBarrage.Length} entrées) : " +
                          string.Join(", ", donnees.prochaineDemandeBarrage));

                foreach (var type in donnees.prochaineDemandeBarrage)
                    AjouterType(type);

                // Consommer la demande — le prochain appel utilisera une nouvelle aléatoire.
                donnees.prochaineDemandeBarrage = new FormulaireType[0];

                Debug.Log($"[ListeAttenteGarde] ✓ Demande chargée depuis session. " +
                          $"_restants={_restants}, état : {DescriptionRestants()}");
            }
            else if (demandeAléatoire != null)
            {
                // Transmettre le bon compteur de barrages pour respecter les paliers de difficulté.
                int barragesComplétés = donnees != null ? donnees.nombreBarragesComplétés : 0;

                Debug.LogWarning($"[ListeAttenteGarde] Aucune demande en session (AUneDemandeSauvegardée=false). " +
                                 $"FALLBACK — génération aléatoire via DemandeBarrage (barragesComplétés={barragesComplétés}).");

                demandeAléatoire.Régénérer(barragesComplétés);

                foreach (var type in demandeAléatoire.Formulaires)
                    AjouterType(type);

                Debug.LogWarning($"[ListeAttenteGarde] FALLBACK chargé. " +
                                 $"_restants={_restants}, état : {DescriptionRestants()} " +
                                 "— vérifiez que prochaineDemandeBarrage était bien rempli.");
            }
            else
            {
                Debug.LogError("[ListeAttenteGarde] Aucune demande session ni DemandeBarrage fournie. " +
                               "Séquence de secours sérialisée dans l'asset utilisée.");

                foreach (var type in séquenceFallback)
                    AjouterType(type);

                Debug.LogError($"[ListeAttenteGarde] Fallback asset chargé. " +
                               $"_restants={_restants}, état : {DescriptionRestants()}");
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private void AjouterType(FormulaireType type)
        {
            if (!_comptesRuntime.ContainsKey(type))
                _comptesRuntime[type] = 0;

            _comptesRuntime[type]++;
            _restants++;
        }

        private string DescriptionRestants()
        {
            var parts = new List<string>();
            foreach (var kv in _comptesRuntime)
                if (kv.Value > 0)
                    parts.Add($"{kv.Key}×{kv.Value}");
            return parts.Count > 0 ? string.Join(", ", parts) : "aucun";
        }
    }
}
