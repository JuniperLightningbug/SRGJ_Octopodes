using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

/**
 * Sets up collection of popups and initialises their event listeners
 */
public class UITutorialPopupWrapper : MonoBehaviour
{
	[SerializeField] private List<UITutorialPopup> _tutorialPopups = new List<UITutorialPopup>();

	[Button( "Update tutorial popups list (Edit-time)" )]
	private void Editor_UpdateTutorialPopupList()
	{
#if UNITY_EDITOR
		if( Application.isPlaying )
		{
			return;
		}

		Undo.RecordObject( this, "Auto-populating tutorial popups list" );
		UITutorialPopup[] tutorialPopups = GetComponentsInChildren<UITutorialPopup>( true );
		_tutorialPopups.Clear();
		_tutorialPopups.AddRange( tutorialPopups );
#endif
	}

	void OnEnable()
	{
		if( _tutorialPopups != null )
		{
			for( int i = 0; i < _tutorialPopups.Count; ++i )
			{
				_tutorialPopups[i]?.Initialise();
			}
		}
	}

	void OnDisable()
	{
		if( _tutorialPopups != null )
		{
			for( int i = 0; i < _tutorialPopups.Count; ++i )
			{
				_tutorialPopups[i]?.CleanUp();
			}
		}
	}
	
}
