using NaughtyAttributes;
using UnityEngine;

public class DEBUG_EventBus : MonoBehaviour
{
	private bool _bPrintEventsInternal = false;
	[SerializeField, OnValueChanged("Inspector_UpdateBPrintEvents")] private bool _bPrintEvents = false;

	[SerializeField] private EventBus.EEventType _eventToRaise = EventBus.EEventType.INVALID;

	[Button( "Raise Event" )]
	public void Inspector_RaiseEvent()
	{
		Debug.LogFormat( "Raising event: {0} (# listeners: {1})",
			_eventToRaise.ToString(),
			EventBus.GetNumListeners( _eventToRaise ) );
		EventBus.Invoke( this, _eventToRaise );
	}
	
#region Singleton
	public static DEBUG_EventBus Instance { get; private set; }

	private void TryInitSingleton()
	{
		if( Instance == null )
		{
			Instance = this;
			DontDestroyOnLoad( gameObject );
		}
		else if( Instance != this )
		{
			Destroy( gameObject );
		}
	}
#endregion

	private void Inspector_UpdateBPrintEvents()
	{
		if( _bPrintEventsInternal != _bPrintEvents )
		{
			_bPrintEventsInternal = _bPrintEvents;
			ToggleListening_All( _bPrintEventsInternal );
		}
	}
	
#region Listeners

	private void OnEnable()
	{
/*
 * The internal switches always start at false, and we didn't reset the serialised
 * versions on disable. We can let 'Update()' turn on the event listeners.
 */
	}

	private void OnDisable()
	{
// Set the internal parameters to false - when enabled, in 'Update()', these will switch back to their
// serialised states automatically.
		_bPrintEventsInternal = false;

		ToggleListening_All( _bPrintEventsInternal );
	}

	private void ToggleListening_All( bool toggleOn )
	{
		for( int i = 0; i < (int)EventBus.EEventType.COUNT; ++i )
		{
			if( toggleOn )
			{
				EventBus.StartListening( (EventBus.EEventType)i, OnEvent_Any );
			}
			else
			{
				EventBus.StopListening( (EventBus.EEventType)i, OnEvent_Any );
			}
		}
	}

#endregion

#region Callbacks

	private void OnEvent_Any( EventBus.EventContext context, object obj )
	{
		string additionalInfo = obj != null ? obj.ToString() : "[No data]";
		Debug.LogFormat( "{0}: EventBus event invoked. '{1}' : '{2}'",
			context._fromObject != null ? context._fromObject.GetType().ToString() : "NULL OBJECT", 
			context._eventType.ToString(),
			additionalInfo );
	}

#endregion
}
