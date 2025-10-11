using UnityEngine;
using UnityEditor;

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
		//Change scene code goes here
    }

    public void QuitGame()
    {
		Application.Quit();
#if UNITY_EDITOR
		EditorApplication.ExitPlaymode();
#endif
	}

}
