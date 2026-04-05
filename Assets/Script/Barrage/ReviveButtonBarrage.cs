using TMPro;
using UnityEngine;

namespace Barrage.UI
{
    /// <summary>
    /// Bouton de revive pour la scène Barrage.
    /// Fonctionne comme ReviveButtonPremium mais appelle LightEndManager.Revive()
    /// qui déclenche la victoire du barrage à la place d'un simple redémarrage.
    /// </summary>
    public class ReviveButtonBarrage : MonoBehaviour
    {
        [SerializeField] private LightEndManager _lightEndManager;
        [SerializeField] private TMP_Text _textPrice;
        [SerializeField] private SO_PlayerDatas _playerDatas;

        private int _price = 1;

        private void OnEnable()
        {
            int revive = _lightEndManager.reviveCounter;
            _price = 2 * revive + 1;
            _textPrice.text = _price.ToString();
        }

        /// <summary>Tente d'acheter un revive avec la monnaie premium. Déclenche LightEndManager.Revive() si possible.</summary>
        public void Revival()
        {
            if (_playerDatas.premiumMonney >= _price)
            {
                _playerDatas.premiumMonney -= _price;
                _playerDatas.SaveDatas();
                _lightEndManager.Revive();
            }
            else
            {
                Debug.Log("[ReviveButtonBarrage] Monnaie premium insuffisante.");
                // TODO : afficher la boutique premium
            }
        }
    }
}
