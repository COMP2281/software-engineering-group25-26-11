using UnityEngine;
using UnityEngine.SceneManagement;

public class SubmitButtonHandler : MonoBehaviour
{
    // Set this in the Inspector or hard-code the scene name
    public string nextSceneName;

    public void OnSubmit()
    {
        // Optional: add any checks before switching
        SceneManager.LoadScene(nextSceneName);
    }
}
