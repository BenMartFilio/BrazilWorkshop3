using UnityEngine;
using UnityEngine.UI;

/// <summary>Pont entre le Button Unity et InventaireMenuUI.OnObjetClique().</summary>
public class InventaireObjetButton : MonoBehaviour
{
    [SerializeField] private InventaireMenuUI menuUI;
    [SerializeField] private Image icone;
    [SerializeField] private string identifiant;
    [SerializeField] private AudioEventDispatcher _audioEventDispatcher;
    [SerializeField] private AudioType _open;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            _audioEventDispatcher?.PlayAudio(_open);
            menuUI.OnObjetClique(icone, identifiant);
        });
    }
}