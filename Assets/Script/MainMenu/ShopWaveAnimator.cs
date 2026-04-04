using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopWaveAnimator : MonoBehaviour
{
    // Seuil en pixels world — ajuste si tes boutons sont très petits ou très grands
    private const float ROW_THRESHOLD = 0.1f;

    [Header("Timing")]
    [SerializeField] private float delayBetweenRows = 0.20f;
    [SerializeField] private float delayBetweenColumns = 0.04f;
    [SerializeField] private float revealDuration = 0.35f;

    [Header("Scroll")]
    [SerializeField] private ScrollRect scrollRect;

    private void OnEnable()
    {
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;

        StartCoroutine(PlayWaveNextFrame());
    }

    private IEnumerator PlayWaveNextFrame()
    {
        // Deux frames : une pour le layout, une pour que le ScrollRect se repositionne
        yield return null;
        yield return null;
        Canvas.ForceUpdateCanvases();
        PlayWave();
    }

    private void PlayWave()
    {
        var buttons = new List<ShopButtonRevealEffect>();
        GetComponentsInChildren(false, buttons);
        if (buttons.Count == 0) return;

        foreach (var btn in buttons) btn.Init();

        // Tri par position world Y décroissant (haut en premier), puis X croissant
        buttons.Sort((a, b) =>
        {
            float ay = a.transform.position.y;
            float by = b.transform.position.y;
            if (Mathf.Abs(ay - by) > ROW_THRESHOLD)
                return by.CompareTo(ay);
            return a.transform.position.x.CompareTo(b.transform.position.x);
        });

        // Grouper par lignes
        var rows = new List<List<ShopButtonRevealEffect>>();
        var currentRow = new List<ShopButtonRevealEffect>();
        float lastY = float.MaxValue;

        foreach (var btn in buttons)
        {
            float y = btn.transform.position.y;
            if (currentRow.Count > 0 && Mathf.Abs(y - lastY) > ROW_THRESHOLD)
            {
                rows.Add(new List<ShopButtonRevealEffect>(currentRow));
                currentRow.Clear();
            }
            currentRow.Add(btn);
            lastY = y;
        }
        if (currentRow.Count > 0) rows.Add(currentRow);

        // Déclencher en vague
        float rowDelay = 0f;
        foreach (var row in rows)
        {
            float colDelay = 0f;
            foreach (var btn in row)
            {
                btn.PlayReveal(rowDelay + colDelay, revealDuration);
                colDelay += delayBetweenColumns;
            }
            rowDelay += delayBetweenRows;
        }
    }
}