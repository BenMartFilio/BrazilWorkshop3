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

        /// <summary>Reçoit un formulaire déposé par le joueur, le détruit et notifie les abonnés.</summary>
        public void RecevoirFormulaire(FormulaireUI formulaire)
        {
            FormulaireType type = formulaire.Type;
            Destroy(formulaire.gameObject);
            OnFormulaireRemis?.Invoke(type);
            Debug.Log($"[MainDuGarde] Formulaire remis : {type}");
        }
    }
}
