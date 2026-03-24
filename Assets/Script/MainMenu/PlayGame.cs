using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayGame : MonoBehaviour
{
    public void ChangeLevel(int level)
    {
       SceneManager.LoadScene(level);
    }
}
