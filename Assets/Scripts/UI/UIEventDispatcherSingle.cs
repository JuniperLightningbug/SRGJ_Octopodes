using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIEventDispatcherSingle : MonoBehaviour
{
	[SerializeField] public Button _button;
	[SerializeField] public List<EventBus.EEventType> _eventList;

	private void OnButtonClicked()
	{
		if( _eventList != null )
		{
			for( int i = 0; i < _eventList.Count; ++i )
			{
				EventBus.Invoke( this, _eventList[i] );
			}
		}
	}

	private void StartListener()
	{
		if( _button )
		{
			_button.onClick.AddListener( OnButtonClicked );
		}
	}


	private void StopListener()
	{
		if( _button )
		{
			_button.onClick.RemoveListener( OnButtonClicked );
		}
	}
	
	void OnEnable()
	{
		if( !_button )
		{
			_button = GetComponent<Button>();
		}
		StartListener();
	}

	void OnDisable()
	{
		StopListener();
	}
}
