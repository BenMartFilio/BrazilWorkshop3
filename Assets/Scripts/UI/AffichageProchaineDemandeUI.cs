using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Barrage.Formulaires;

namespace Barrage.UI
{
    /// <summary>
    /// Orchestre l'affichage de la prochaine demande de barrage.
    /// L'affichage ne se déclenche QUE lorsque le joueur valide le barrage courant
    /// (via l'événement OnBarrageValidé de MainDuGardeUI).
    /// Les icônes apparaissent une à une avec un délai, accompagnées du nombre requis par type.
    /// </summary>
    public class AffichageProchaineDemandeUI : MonoBehaviour
    {
        [Header("Slots d'icônes (dans l'ordre d'affichage)")]
        [Tooltip("Les quatre IconFormulaireUI dans la scène, dans l'ordre gauche→droite.")]
        [SerializeField] private List<IconFormulaireUI> slots = new();

        [Header("Données")]
        [Tooltip("La demande de barrage à générer et afficher.")]
        [SerializeField] private DemandeBarrage demande;
        [Tooltip("Les FormulaireData pour associer chaque type à sa texture.")]
        [SerializeField] private List<FormulaireData> formulairesData = new();
        [Tooltip("MainDuGardeUI dont l'événement OnBarrageValidé déclenche l'affichage.")]
        [SerializeField] private MainDuGardeUI mainDuGarde;

        [Header("Animation")]
        [Tooltip("Délai entre l'apparition de chaque icône (secondes).")]
        [SerializeField] private float délaiEntreIcones = 0.15f;

        private readonly Dictionary<FormulaireType, FormulaireData> _dataParType = new();
        private Coroutine _affichage;

        private void Awake()
        {
            foreach (var data in formulairesData.Where(d => d != null))
                _dataParType[data.type] = data;

            // Masquer tous les slots au démarrage — rien ne s'affiche avant la validation
            foreach (var slot in slots)
                slot.Masquer();
        }

        private void OnEnable()
        {
            if (mainDuGarde != null)
                mainDuGarde.OnBarrageValidé += OnBarrageValidé;
        }

        private void OnDisable()
        {
            if (mainDuGarde != null)
                mainDuGarde.OnBarrageValidé -= OnBarrageValidé;
        }

        private void OnBarrageValidé()
        {
            // Générer une nouvelle demande aléatoire puis l'afficher
            demande?.Régénérer();

            if (_affichage != null) StopCoroutine(_affichage);
            _affichage = StartCoroutine(AfficherIconesUneParUne());
        }

        /// <summary>
        /// Affiche les icônes une à une en regroupant par type :
        /// chaque slot reçoit un type distinct avec la quantité totale de ce type dans la demande.
        /// L'ordre des slots suit l'ordre d'apparition des types dans la demande.
        /// </summary>
        private IEnumerator AfficherIconesUneParUne()
        {
            foreach (var slot in slots)
                slot.Masquer();

            var typesOrdrés = new List<FormulaireType>();
            var comptes     = new Dictionary<FormulaireType, int>();

            foreach (var type in demande.Formulaires)
            {
                if (!comptes.ContainsKey(type))
                {
                    typesOrdrés.Add(type);
                    comptes[type] = 0;
                }
                comptes[type]++;
            }

            int nbSlots = Mathf.Min(typesOrdrés.Count, slots.Count);

            for (int i = 0; i < nbSlots; i++)
            {
                FormulaireType type = typesOrdrés[i];

                if (!_dataParType.TryGetValue(type, out var data))
                {
                    Debug.LogWarning($"[AffichageProchaineDemandeUI] Aucun FormulaireData pour : {type}");
                    continue;
                }

                Texture2D texture = data.ExtraireTexture();
                if (texture == null)
                {
                    Debug.LogWarning($"[AffichageProchaineDemandeUI] Aucune texture dans le prefab de : {type}");
                    continue;
                }

                slots[i].Afficher(texture, comptes[type]);
                yield return new WaitForSeconds(délaiEntreIcones);
            }

            _affichage = null;
        }
    }
}
