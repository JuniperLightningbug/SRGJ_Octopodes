using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

public class UIEventDispatcher : MonoBehaviour
{
	[System.Serializable]
	public struct UIEvent
	{
		[SerializeField] public Button _button;
		[SerializeField] public List<EventBus.EEventType> _eventList;
	}

	[SerializeField] private List<UIEvent> _buttonClickEvents = new List<UIEvent>();
	
	// Used to ensure buttons are cleared when inspector data changes at runtime
	private readonly List<Button> _buttonsListeningTo = new List<Button>();
	
	// Use compiled dictionary rather than the input list in case the list has been changed in the inspector at runtime
	// This is the source of truth for the event listener bookkeeping
	// It also ensures uniqueness of input button objects and flags any errors
	private Dictionary<Button, List<EventBus.EEventType>> _eventRuntimeDictionary;

	[NaughtyAttributes.Button( "Runtime: Refresh Events Map" )]
	private void Editor_RefreshEvents()
	{
#if UNITY_EDITOR
		if( Application.isPlaying )
		{
			// Will automatically stop listeners in the private cached button list before assigning a new one
			StartListeners();
		}
#endif
	}

	private void OnButtonClicked( List<EventBus.EEventType> inEvents )
	{
		if( inEvents != null )
		{
			for( int i = 0; i < inEvents.Count; ++i )
			{
				EventBus.Invoke( this, inEvents[i] );
			}
		}
	}

	private void StartListeners()
	{
		if( _buttonsListeningTo.Count > 0 )
		{
			StopListeners();
		}

		_buttonsListeningTo.Clear();
		for( int i = 0; i < _buttonClickEvents.Count; ++i )
		{
			if( _buttonClickEvents[i]._button &&
			    _buttonClickEvents[i]._eventList != null &&
			    _buttonClickEvents[i]._eventList.Count > 0 )
			{
				int pinnedButtonIdx = i;
				_buttonClickEvents[i]._button.onClick.AddListener( () => OnButtonClicked( _buttonClickEvents[pinnedButtonIdx]._eventList ) );
				_buttonsListeningTo.Add( _buttonClickEvents[i]._button );
			}
		}
	}


	private void StopListeners()
	{
		for( int i = 0; i < _buttonsListeningTo.Count; ++i )
		{
			if( _buttonsListeningTo[i] )
			{
				_buttonsListeningTo[i].onClick.RemoveAllListeners();
			}
		}
		_buttonsListeningTo.Clear();
	}
	
	void OnEnable()
	{
		StartListeners();
	}

	void OnDisable()
	{
		StopListeners();
	}
}