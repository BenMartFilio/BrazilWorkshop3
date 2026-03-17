using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayGame : MonoBehaviour
{
    private int LevelNext = 1;

    public void ChangeLevel(int level)
    {
       SceneManager.LoadScene(level);
    } //Mettre fondu en noir, ou effet original pour changer écran chargement
}
