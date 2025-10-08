using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Similar to UIEventDispatcher - TODO could generalise this
public class UISensorRadioDispatcher : MonoBehaviour
{
	[System.Serializable]
	public class UISensorToggle
	{
		[SerializeField] public Toggle _toggle;
		[SerializeField] public SO_PlanetConfig.ESensorType _sensorType;
	}

	[SerializeField] private List<UISensorToggle> _toggles = new List<UISensorToggle>();

	[NaughtyAttributes.Button("Auto-Populate")]
	private void Editor_FillToggleListWithChildren()
	{
#if UNITY_EDITOR
		if( !Application.isPlaying )
		{
			Undo.RecordObject( this, "Auto-Populated Toggle List" );
			List<UISensorToggle> newToggles = new List<UISensorToggle>();
			Toggle[] foundToggles = GetComponentsInChildren<Toggle>( true );
			for( int foundIdx = 0; foundIdx < foundToggles.Length; ++foundIdx )
			{
				bool bCopiedExisting = false;
				if( _toggles != null )
				{
					for( int existingIdx = 0; existingIdx < _toggles.Count; ++existingIdx )
					{
						if( _toggles[existingIdx]._toggle != null &&
						    foundToggles[foundIdx] == _toggles[existingIdx]._toggle )
						{
							newToggles.Add( _toggles[existingIdx] );
							bCopiedExisting = true;
						}
					}
				}

				if( !bCopiedExisting )
				{
					newToggles.Add( new UISensorToggle()
					{
						_toggle = foundToggles[foundIdx],
						_sensorType = SO_PlanetConfig.ESensorType.INVALID,
					} );
				}
			}

			_toggles = newToggles;
		}
#endif
	}
	
	// Use compiled dictionary rather than the input list in case the list has been changed in the inspector at runtime
	// This is the source of truth for the event listener bookkeeping
	// It also ensures uniqueness of input button objects and flags any errors
	private Dictionary<Button, List<EventBus.EEventType>> _eventRuntimeDictionary;

	private void OnToggleValueChanged( UISensorToggle inToggle )
	{
		if( inToggle?._toggle && inToggle._toggle.isOn )
		{
			// TEMP - FIX THIS! We need to be able to toggle off as well. TODO
			// TODO also use the ui event instead of the gamemanager one
			EventBus.Invoke( this, EventBus.EEventType.UI_ChangeActiveSensorType, inToggle._sensorType );
		}
	}

	private void StartListeners()
	{
		if( _toggles != null )
		{
			for( int i = 0; i < _toggles.Count; ++i )
			{
				if( _toggles[i]?._toggle != null )
				{
					int pinnedToggleIdx = i;
					_toggles[i]._toggle.onValueChanged.AddListener( delegate
					{
						OnToggleValueChanged( _toggles[pinnedToggleIdx] );
					} );
				}
			}
		}
	}

	private void StopListeners()
	{
		// TODO I'm leaving this for now because it would require caching all of the delegates, and would be a
		// redundant failsafe (I think). When this object is inactive, so are the child toggles, and there are
		// no callback event invocations anyway.
	}

	private void Start()
	{
		StartListeners();

		// TODO THIS IS TEMP FIX
		_toggles[0]._toggle.isOn = true;
		EventBus.Invoke( this, EventBus.EEventType.UI_ChangeActiveSensorType, _toggles[0]._sensorType );
		EventBus.Invoke( this, EventBus.EEventType.UI_NextPlanet, _toggles[0]._sensorType );

	}
}