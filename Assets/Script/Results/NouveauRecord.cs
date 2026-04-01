using System.Collections;
using TMPro;
using UnityEngine;

public class NouveauRecord : MonoBehaviour
{
    [SerializeField] private SO_PlayerDatas playerDatas;
    public TextMeshProUGUI textMesh;

    [Header("Wave Pattern")]
    public float[] wavePattern = new float[] { 0.5f, 1.5f, 3f, 1.5f, 0.8f, 0.8f, 0.8f, 0.8f };
    public float waveSpeed = 5f;
    public float waveHeight = 15f;

    [Header("Color")]
    public Gradient gradient;
    public float colorSpeed = 2f;
    public float colorWidth = 3f;

    private Mesh mesh;
    private Vector3[] vertices;

    void Awake()
    {
        if (playerDatas.isAnHighScore == false)
        {
            gameObject.SetActive(false);
            return;
        }
        playerDatas.isAnHighScore = false;
        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        while (true)
        {
            textMesh.ForceMeshUpdate();
            mesh = textMesh.mesh;
            vertices = mesh.vertices;

            float waveOffset = Time.time * waveSpeed;
            int patternLength = wavePattern.Length;

            for (int i = 0; i < textMesh.textInfo.characterCount; i++)
            {
                var charInfo = textMesh.textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int index = charInfo.vertexIndex;

                // === POSITION DANS LE WAVE PATTERN ===
                float pos = i - waveOffset;
                float wrappedPos = pos % patternLength;
                if (wrappedPos < 0) wrappedPos += patternLength;

                int indexA = Mathf.FloorToInt(wrappedPos);
                int indexB = (indexA + 1) % patternLength;
                float t = wrappedPos - indexA;

                float height = Mathf.Lerp(wavePattern[indexA], wavePattern[indexB], t);
                float yOffset = height * waveHeight;
                Vector3 offset = new Vector3(0, yOffset, 0);

                vertices[index + 0] += offset;
                vertices[index + 1] += offset;
                vertices[index + 2] += offset;
                vertices[index + 3] += offset;

                float wavePos = Time.time * colorSpeed;
                float charCount = textMesh.textInfo.characterCount;
                float normalizedWave = (wavePos % charCount + charCount) % charCount;
                float dist = Mathf.Abs(i - normalizedWave);
                // Prend le chemin le plus court dans la boucle
                if (dist > charCount / 2f) dist = charCount - dist;
                float te = Mathf.Clamp01(1f - dist / colorWidth);
                te = Mathf.SmoothStep(0f, 1f, te);

                Color32 col = gradient.Evaluate(te); 

                var colors = textMesh.textInfo.meshInfo[charInfo.materialReferenceIndex].colors32;
                colors[index + 0] = col;
                colors[index + 1] = col;
                colors[index + 2] = col;
                colors[index + 3] = col;
            }

            mesh.vertices = vertices;
            textMesh.canvasRenderer.SetMesh(mesh);

            textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            yield return null;
        }
    }
}