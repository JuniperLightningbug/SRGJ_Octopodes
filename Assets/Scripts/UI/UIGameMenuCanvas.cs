using UnityEngine;
using UnityEngine.InputSystem;

public class UIGameMenuCanvas : MonoBehaviour
{
	[SerializeField] private GameObject _panel_Debug;
	[SerializeField] private bool _bPanelActive_Debug = false;
	[SerializeField] private UIEncyclopedia _encyclopedia;
	
	void Update()
	{
		if( Keyboard.current.backspaceKey.wasPressedThisFrame && _panel_Debug )
		{
			_bPanelActive_Debug = !_bPanelActive_Debug;
			_panel_Debug.SetActive( _bPanelActive_Debug );
		}
	}

	void Start()
	{
		if( _panel_Debug )
		{
			_panel_Debug.SetActive( _bPanelActive_Debug );
		}

		if( _encyclopedia )
		{
			_encyclopedia.Initialise();
			_encyclopedia.gameObject.SetActive( false );
		}
	}
}
