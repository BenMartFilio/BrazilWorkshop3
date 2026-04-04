// NouveauRecord.cs — corrige l'allocation vertices chaque frame
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

    private Mesh _mesh;
    private Vector3[] _vertices; // réutilisé — plus d'allocation par frame

    private void Awake()
    {
        if (!playerDatas.isAnHighScore)
        {
            gameObject.SetActive(false);
            return;
        }
        playerDatas.isAnHighScore = false;
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        while (true)
        {
            textMesh.ForceMeshUpdate();
            _mesh = textMesh.mesh;

            // Récupère le tableau UNE fois et le réutilise
            // (on le copie seulement si la taille a changé)
            Vector3[] fresh = _mesh.vertices;
            if (_vertices == null || _vertices.Length != fresh.Length)
                _vertices = fresh;
            else
                System.Array.Copy(fresh, _vertices, fresh.Length);

            int patternLength = wavePattern.Length;
            float waveOffset = Time.time * waveSpeed;

            for (int i = 0; i < textMesh.textInfo.characterCount; i++)
            {
                var charInfo = textMesh.textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int index = charInfo.vertexIndex;

                float pos = i - waveOffset;
                float wrappedPos = pos % patternLength;
                if (wrappedPos < 0) wrappedPos += patternLength;

                int indexA = Mathf.FloorToInt(wrappedPos);
                int indexB = (indexA + 1) % patternLength;
                float t = wrappedPos - indexA;

                float yOffset = Mathf.Lerp(wavePattern[indexA], wavePattern[indexB], t) * waveHeight;
                Vector3 offset = new Vector3(0, yOffset, 0);

                _vertices[index + 0] += offset;
                _vertices[index + 1] += offset;
                _vertices[index + 2] += offset;
                _vertices[index + 3] += offset;

                // Couleur
                float charCount = textMesh.textInfo.characterCount;
                float normalizedWave = (Time.time * colorSpeed % charCount + charCount) % charCount;
                float dist = Mathf.Abs(i - normalizedWave);
                if (dist > charCount * 0.5f) dist = charCount - dist;
                float te = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - dist / colorWidth));

                Color32 col = gradient.Evaluate(te);
                var colors = textMesh.textInfo.meshInfo[charInfo.materialReferenceIndex].colors32;
                colors[index + 0] = col;
                colors[index + 1] = col;
                colors[index + 2] = col;
                colors[index + 3] = col;
            }

            _mesh.vertices = _vertices;
            textMesh.canvasRenderer.SetMesh(_mesh);
            textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            yield return null;
        }
    }
}