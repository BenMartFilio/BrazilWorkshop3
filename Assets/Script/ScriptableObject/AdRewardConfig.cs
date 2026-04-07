using UnityEngine;

/// <summary>
/// Décrit la récompense accordée après qu'une publicité récompensée a été regardée.
/// Crée une instance via Assets > Create > Scriptable Objects > AdRewardConfig.
/// </summary>
[CreateAssetMenu(fileName = "AdRewardConfig", menuName = "Scriptable Objects/AdRewardConfig")]
public class AdRewardConfig : ScriptableObject
{
    public enum RewardType
    {
        Coins,
        Gems,
        Cosmetic
    }

    [Tooltip("Type de récompense distribuée après la pub.")]
    public RewardType rewardType = RewardType.Coins;

    [Tooltip("Montant accordé (ignoré si RewardType = Cosmetic).")]
    public int amount = 100;

    [Tooltip("Identifiant du cosmétique à débloquer (uniquement si RewardType = Cosmetic).")]
    public string cosmeticIdentifier;

    [Tooltip("Sprite affiché dans l'animation EarnObjectMenu après l'obtention de la récompense.")]
    public Sprite rewardSprite;

    [Tooltip("Nombre maximum d'utilisations par jour (0 = illimité).")]
    public int dailyLimit = 3;
}
