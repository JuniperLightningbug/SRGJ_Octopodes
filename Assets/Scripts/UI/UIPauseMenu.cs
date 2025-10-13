using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class UIPauseMenu : MonoBehaviour
{
	private void OnEnable()
	{
		InputModeManager.Instance?.AddUIBlockingObject(gameObject);
	}

	private void OnDisable()
	{
		InputModeManager.Instance?.RemoveUIBlockingObject(gameObject);
	}

	public void GoToMainMenu()
    {
	    SceneManager.LoadScene( "MainMenu" );
    }

    public void QuitGame()
    {
		Application.Quit();
#if UNITY_EDITOR
		EditorApplication.ExitPlaymode();
#endif
	}

}
