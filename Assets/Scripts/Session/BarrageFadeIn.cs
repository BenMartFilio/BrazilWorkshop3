using UnityEngine;

/// <summary>
/// Déclenche le fade-in depuis le noir au démarrage de la scène Barrage.
/// À placer sur n'importe quel GameObject actif de la scène Barrage.
/// Le fade-in est géré par <see cref="FondeurTransitionScène"/> via <see cref="SessionManager"/>.
/// </summary>
[DefaultExecutionOrder(100)] // Après tous les systèmes Barrage — la scène est prête quand on révèle.
public class BarrageFadeIn : MonoBehaviour
{
    private void Start()
    {
        if (SessionManager.Instance != null)
            SessionManager.Instance.LancerFadeInBarrage();
        else
            FondeurTransitionScène.Instance?.FondreDepuisNoir(); // fallback si SessionManager absent
    }
}
