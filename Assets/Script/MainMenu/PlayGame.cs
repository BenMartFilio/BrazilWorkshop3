using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayGame : MonoBehaviour
{
    [SerializeField] private GameObject serrure;
    public Vector3 rotationAmount = new Vector3(0, 0, 120); 
    public float duration = 1.5f; 
    public void LaunchGame()
    {
        StartCoroutine(RoutineRotation(rotationAmount, duration));

    }

    IEnumerator RoutineRotation(Vector3 rotation, float time)
    {
        Quaternion startRotation = serrure.transform.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(rotation);

        float elapsed = 0f;

        while (elapsed < time)
        {
            serrure.transform.rotation = Quaternion.Slerp(startRotation, endRotation, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }

        serrure.transform.rotation = endRotation;
        ChangeLevel(1);
    }

    public void ChangeLevel(int level)
    {
       SceneManager.LoadScene(level);
    }
}
