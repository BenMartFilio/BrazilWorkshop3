using System;
using UnityEngine;
using Barrage.Formulaires;

namespace Barrage.UI
{
    /// <summary>
    /// Zone de dépôt représentant la main du garde.
    /// Déclenche un événement lorsqu'un formulaire y est déposé.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MainDuGardeUI : MonoBehaviour
    {
        public RectTransform RectTransform { get; private set; }

        /// <summary>Déclenché lorsqu'un formulaire est remis au garde, avec son type.</summary>
        public event Action<FormulaireType> OnFormulaireRemis;

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
        }

        /// <summary>Reçoit un formulaire du système poche (FormulaireUI).</summary>
        public void RecevoirFormulaire(FormulaireUI formulaire)
            => RecevoirInterne(formulaire.Type, formulaire.gameObject);

        /// <summary>Reçoit un formulaire du système libre (FormulaireLibre).</summary>
        public void RecevoirFormulaire(FormulaireLibre formulaire)
            => RecevoirInterne(formulaire.Type, formulaire.gameObject);

        private void RecevoirInterne(FormulaireType type, GameObject go)
        {
            Destroy(go);
            OnFormulaireRemis?.Invoke(type);
            Debug.Log($"[MainDuGarde] Formulaire remis : {type}");
        }
    }
}
