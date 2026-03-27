using System.Collections;
using UnityEngine;

/// <summary>
/// Effet d'explosion procédural à base de cercles sprites, déclenché à la position souhaitée.
/// Composé de 4 couches simultanées :
///   1. Cercles noirs (débris) — ~10 petits cercles qui partent dans toutes les directions.
///   2. Cercles rouges (coeur) — gros cercles qui scintillent au centre puis disparaissent.
///   3. Cercles oranges (anneau) — cercles moyens qui spawnent sur le bord du rouge.
///   4. Cercles jaunes (étincelles) — petits cercles qui spawnent sur le bord des oranges.
/// </summary>
public class ExplosionCircles : MonoBehaviour
{
    // ── Sprites ────────────────────────────────────────────────────────────────
    [Header("Sprites")]
    [SerializeField] private Sprite spriteNoir;
    [SerializeField] private Sprite spriteRouge;
    [SerializeField] private Sprite spriteOrange;
    [SerializeField] private Sprite spriteJaune;

    // ── Paramètres débris noirs ────────────────────────────────────────────────
    [Header("Débris noirs")]
    [SerializeField] private int   nombreDebrisNoirs    = 10;
    [SerializeField] private float tailleDebrisNoir     = 0.12f;
    [SerializeField] private float vitesseDebrisMin     = 2.5f;
    [SerializeField] private float vitesseDebrisMax     = 5.5f;
    [SerializeField] private float dureeDebris          = 0.55f;

    // ── Paramètres cercles rouges ──────────────────────────────────────────────
    [Header("Cœur rouge")]
    [SerializeField] private int   nombreRouges         = 3;
    [SerializeField] private float tailleRougeMin       = 0.55f;
    [SerializeField] private float tailleRougeMax       = 1.0f;
    [SerializeField] private float dureeRouge           = 0.45f;
    [SerializeField] private float frequenceScintille   = 22f;  // Hz

    // ── Paramètres cercles oranges ─────────────────────────────────────────────
    [Header("Anneau orange")]
    [SerializeField] private int   nombreOranges        = 6;
    [SerializeField] private float tailleOrange         = 0.28f;
    [SerializeField] private float rayonAnneau          = 0.45f;  // rayon sur lequel ils spawnent
    [SerializeField] private float vitesseOrangeMin     = 1.2f;
    [SerializeField] private float vitesseOrangeMax     = 2.5f;
    [SerializeField] private float dureeOrange          = 0.42f;

    // ── Paramètres cercles jaunes ──────────────────────────────────────────────
    [Header("Étincelles jaunes")]
    [SerializeField] private int   nombreJaunesParOrange = 2;
    [SerializeField] private float tailleJaune           = 0.10f;
    [SerializeField] private float offsetJauneRayon      = 0.20f; // offset depuis le centre de l'orange
    [SerializeField] private float vitesseJauneMin       = 1.8f;
    [SerializeField] private float vitesseJauneMax       = 3.2f;
    [SerializeField] private float dureeJaune            = 0.30f;

    // ── Sorting layer ──────────────────────────────────────────────────────────
    [Header("Rendu")]
    [SerializeField] private string sortingLayerName = "Gameplay";
    [SerializeField] private int    sortingOrder     = 10;

    // ── API publique ───────────────────────────────────────────────────────────

    /// <summary>
    /// Déclenche l'explosion à la position indiquée, puis détruit le GameObject à la fin.
    /// </summary>
    public void Jouer(Vector3 position)
    {
        transform.position = position;
        StartCoroutine(JouerRoutine());
    }

    // ── Coroutine principale ───────────────────────────────────────────────────

    private IEnumerator JouerRoutine()
    {
        // Toutes les couches démarrent en même temps
        LancerDebrisNoirs();
        LancerCoeurRouge();
        LancerAnneauOrange();

        float dureeMax = Mathf.Max(dureeDebris, dureeRouge, dureeOrange, dureeJaune);
        yield return new WaitForSeconds(dureeMax + 0.05f);

        Destroy(gameObject);
    }

    // ── Couche 1 : débris noirs ────────────────────────────────────────────────

    private void LancerDebrisNoirs()
    {
        for (int i = 0; i < nombreDebrisNoirs; i++)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            float   vitesse   = Random.Range(vitesseDebrisMin, vitesseDebrisMax);
            float   taille    = tailleDebrisNoir * Random.Range(0.7f, 1.3f);

            GameObject go = CreerCercle("DebrriNoir", spriteNoir, taille);
            StartCoroutine(AnimerDebris(go, direction * vitesse, dureeDebris));
        }
    }

    private IEnumerator AnimerDebris(GameObject go, Vector2 velocite, float duree)
    {
        if (go == null) yield break;

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        float t = 0f;

        while (t < duree && go != null)
        {
            t += Time.deltaTime;
            float ratio = t / duree;

            go.transform.localPosition += (Vector3)(velocite * Time.deltaTime);
            // ralentissement
            velocite = Vector2.Lerp(velocite, Vector2.zero, Time.deltaTime * 4f);

            Color c = sr.color;
            c.a = Mathf.SmoothStep(1f, 0f, ratio);
            sr.color = c;

            yield return null;
        }

        if (go != null) Destroy(go);
    }

    // ── Couche 2 : cœur rouge (scintille) ────────────────────────────────────

    private void LancerCoeurRouge()
    {
        for (int i = 0; i < nombreRouges; i++)
        {
            float taille = Random.Range(tailleRougeMin, tailleRougeMax);
            Vector2 offset = Random.insideUnitCircle * 0.15f;

            GameObject go = CreerCercle("CoeurRouge", spriteRouge, taille);
            go.transform.localPosition = (Vector3)offset;

            StartCoroutine(AnimerScintille(go, dureeRouge, frequenceScintille));
        }
    }

    private IEnumerator AnimerScintille(GameObject go, float duree, float frequence)
    {
        if (go == null) yield break;

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        float t = 0f;

        while (t < duree && go != null)
        {
            t += Time.deltaTime;
            float ratio = t / duree;

            // Scintillement : oscillation rapide + fade global
            float scintille = 0.5f + 0.5f * Mathf.Sin(t * frequence * Mathf.PI * 2f);
            float fade      = Mathf.SmoothStep(1f, 0f, ratio);

            Color c = sr.color;
            c.a = scintille * fade;
            sr.color = c;

            // légère pulsation de taille
            float scale = Mathf.Lerp(1.1f, 0.6f, ratio) * (1f + 0.08f * Mathf.Sin(t * frequence * Mathf.PI * 2f));
            go.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        if (go != null) Destroy(go);
    }

    // ── Couche 3 : anneau orange ──────────────────────────────────────────────

    private void LancerAnneauOrange()
    {
        for (int i = 0; i < nombreOranges; i++)
        {
            float   angle     = (360f / nombreOranges) * i + Random.Range(-15f, 15f);
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            Vector2 spawnPos  = direction * rayonAnneau;
            float   vitesse   = Random.Range(vitesseOrangeMin, vitesseOrangeMax);

            GameObject go = CreerCercle("AnnOrange", spriteOrange, tailleOrange);
            go.transform.localPosition = (Vector3)spawnPos;

            // Lancer les jaunes depuis chaque orange
            LancerJaunesDepuis(spawnPos, direction, angle);

            StartCoroutine(AnimerDebris(go, direction * vitesse, dureeOrange));
        }
    }

    // ── Couche 4 : étincelles jaunes ─────────────────────────────────────────

    private void LancerJaunesDepuis(Vector2 centreOrange, Vector2 directionOrange, float angleBase)
    {
        for (int j = 0; j < nombreJaunesParOrange; j++)
        {
            float   spread     = Random.Range(-30f, 30f);
            float   angleJaune = angleBase + spread;
            Vector2 dirJaune   = new Vector2(Mathf.Cos(angleJaune * Mathf.Deg2Rad), Mathf.Sin(angleJaune * Mathf.Deg2Rad));
            Vector2 spawnPos   = centreOrange + dirJaune * offsetJauneRayon;
            float   vitesse    = Random.Range(vitesseJauneMin, vitesseJauneMax);

            GameObject go = CreerCercle("EtincelleJaune", spriteJaune, tailleJaune);
            go.transform.localPosition = (Vector3)spawnPos;

            StartCoroutine(AnimerDebris(go, dirJaune * vitesse, dureeJaune));
        }
    }

    // ── Utilitaires ───────────────────────────────────────────────────────────

    /// <summary>Crée un GameObject enfant avec SpriteRenderer configuré.</summary>
    private GameObject CreerCercle(string nom, Sprite sprite, float taille)
    {
        GameObject go = new GameObject(nom);
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * taille;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder     = sortingOrder;

        return go;
    }
}
