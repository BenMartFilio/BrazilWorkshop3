using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToTuto : MonoBehaviour
{
    public void ChangeLevel(int level)
    {
        SceneManager.LoadScene(level);
    }
}
