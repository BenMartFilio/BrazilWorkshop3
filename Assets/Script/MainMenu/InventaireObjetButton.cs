using UnityEngine;
using UnityEngine.UI;

/// <summary>Pont entre le Button Unity et InventaireMenuUI.OnObjetClique().</summary>
public class InventaireObjetButton : MonoBehaviour
{
    [SerializeField] private InventaireMenuUI menuUI;
    [SerializeField] private Image icone;       // le Icone enfant direct
    [SerializeField] private string identifiant;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
            menuUI.OnObjetClique(icone, identifiant));
    }
}
