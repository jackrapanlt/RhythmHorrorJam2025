using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneBoss : MonoBehaviour
{
    [SerializeField] private string bossSceneName = "Boss";

    void Start()
    {
        //SceneManager.LoadScene(bossSceneName);
        GameOverController.Instance?.TriggerGameOver();
    }
}
