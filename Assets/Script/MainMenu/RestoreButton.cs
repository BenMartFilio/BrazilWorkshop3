using UnityEngine;
using UnityEngine.UI;

/// <summary>Triggers IAP restore for non-consumable purchases. iOS only.</summary>
public class RestoreButton : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnButtonClicked);

        // Cache le bouton sur Android, il est inutile
#if !UNITY_IOS
        gameObject.SetActive(false);
#endif
    }

    private void OnButtonClicked()
    {
        IAPManager.Instance?.RestorePurchases();
    }

    private void OnDestroy()
    {
        _button.onClick.RemoveListener(OnButtonClicked);
    }
}
