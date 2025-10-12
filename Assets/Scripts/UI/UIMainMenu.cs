using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIMainMenu : MonoBehaviour
{

	public string _gameSceneName = "PlanetManager";

	// UI Event
	public void UI_StartGame()
	{
		UI_StartGame( _gameSceneName );
	}
	public void UI_StartGame(string inSceneName)
	{
		Scene activeScene = SceneManager.GetActiveScene();
		if( activeScene.name != inSceneName )
		{
			SceneManager.LoadScene( inSceneName, LoadSceneMode.Single );
		}
	}
	
	// UI Event
	[Button( "Quit Game" )]
	public void UI_QuitGame()
	{
		Application.Quit();
#if UNITY_EDITOR
		EditorApplication.ExitPlaymode();
#endif
	}
	
}
