using System.Collections;
using ObjetsSpeciaux;
using TMPro;
using UnityEngine;

/// <summary>
/// Affiche un texte "Dodge !" en espace monde au-dessus du joueur chaque fois que
/// le Gâteau Chinois esquive un obstacle (<see cref="EffetsObjetsSpeciaux.OnEsquiveDeclenchee"/>).
/// Le texte monte et disparaît progressivement en fondu.
/// Placer ce composant sur n'importe quel GameObject de la scène MapRoad.
/// Assigner <see cref="_texte"/> (TextMeshPro world-space, initialement inactif)
/// et <see cref="_transformJoueur"/> dans l'Inspector.
/// </summary>
public class FeedbackEsquiveUI : MonoBehaviour
{
    [SerializeField] private EffetsObjetsSpeciaux _effets;
    [SerializeField] private Transform            _transformJoueur;
    [SerializeField] private TextMeshPro          _texte;

    private const float DUREE_ANIMATION  = 0.9f;
    private const float HAUTEUR_MONTEE   = 1.4f;
    private const float OFFSET_Y_DEPART  = 0.6f;   // au-dessus du centre de la voiture
    private const float SEUIL_FONDU_ENTRANT  = 0.15f;  // fraction de DUREE pour le fade-in
    private const float SEUIL_FONDU_SORTANT  = 0.45f;  // fraction à partir de laquelle le fade-out commence

    private static readonly Color COULEUR = new Color(0.25f, 1f, 0.35f, 1f);  // vert vif

    private Coroutine _coroutineAnimation;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (_effets == null)
        {
            Debug.LogWarning("[FeedbackEsquiveUI] _effets non assigné -- le feedback 'Dodge !' ne s'affichera pas.");
            return;
        }

        if (_texte == null)
        {
            Debug.LogWarning("[FeedbackEsquiveUI] _texte non assigné -- créer un TextMeshPro world-space et l'assigner.");
            return;
        }

        _texte.gameObject.SetActive(false);
        _effets.OnEsquiveDeclenchee += AfficherDodge;
    }

    private void OnDestroy()
    {
        if (_effets != null)
            _effets.OnEsquiveDeclenchee -= AfficherDodge;
    }

    // ── Déclenchement ─────────────────────────────────────────────────────────

    private void AfficherDodge()
    {
        if (_texte == null || _transformJoueur == null) return;

        if (_coroutineAnimation != null)
            StopCoroutine(_coroutineAnimation);

        _coroutineAnimation = StartCoroutine(AnimerDodge());
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    private IEnumerator AnimerDodge()
    {
        _texte.text = "Dodge !";
        _texte.gameObject.SetActive(true);

        Vector3 posDepart = _transformJoueur.position + Vector3.up * OFFSET_Y_DEPART;
        _texte.transform.position = posDepart;

        float elapsed = 0f;

        while (elapsed < DUREE_ANIMATION)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / DUREE_ANIMATION);

            // Position : montée linéaire
            _texte.transform.position = posDepart + Vector3.up * (HAUTEUR_MONTEE * t);

            // Alpha : fondu entrant puis sortant
            float alpha;
            if (t < SEUIL_FONDU_ENTRANT)
                alpha = t / SEUIL_FONDU_ENTRANT;
            else if (t > SEUIL_FONDU_SORTANT)
                alpha = 1f - (t - SEUIL_FONDU_SORTANT) / (1f - SEUIL_FONDU_SORTANT);
            else
                alpha = 1f;

            _texte.color = new Color(COULEUR.r, COULEUR.g, COULEUR.b, alpha);

            yield return null;
        }

        _texte.gameObject.SetActive(false);
        _coroutineAnimation = null;
    }
}
