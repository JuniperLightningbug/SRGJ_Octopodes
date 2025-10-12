using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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
		if( inToggle?._toggle )
		{
			if( inToggle._toggle.isOn )
			{
				EventBus.Invoke( this, EventBus.EEventType.UI_ActivateSensorView, inToggle._sensorType );
			}
			else
			{
				EventBus.Invoke( this, EventBus.EEventType.UI_DeactivateSensorView, inToggle._sensorType );
			}
		}
	}

	private void StartToggleListeners()
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

	private void StopToggleListeners()
	{
		// TODO I'm leaving this for now because it would require caching all of the delegates, and would be a
		// redundant failsafe (I think). When this object is inactive, so are the child toggles, and there are
		// no callback event invocations anyway.
	}
	
	private void OnGlobalEvent_TUTHideAllLayers( EventBus.EventContext context, object obj = null )
	{
		if( _toggles != null )
		{
			for( int i = 0; i < _toggles.Count; ++i )
			{
				_toggles[i]._toggle.gameObject.SetActive( false );
			}
		}
	}
	
	private void OnGlobalEvent_TUTShowLayer( EventBus.EventContext context, object obj = null )
	{
		if( obj is SO_PlanetConfig.ESensorType type && _toggles != null )
		{
			for( int i = 0; i < _toggles.Count; ++i )
			{
				if( _toggles[i]._sensorType == type )
				{
					_toggles[i]._toggle.gameObject.SetActive( true );
				}
			}
		}
	}
	
	private void OnGlobalEvent_TUTActivateLayer( EventBus.EventContext context, object obj = null )
	{
		if( obj is SO_PlanetConfig.ESensorType type && _toggles != null )
		{
			ActivateLayer( type );
		}
	}
	
	private void OnGlobalEvent_SatelliteCardSelected( EventBus.EventContext context, object obj = null )
	{
		// If a satellite card is selected, switch to the corresponding view type
		if( obj is SO_Satellite selection && _toggles != null )
		{
			ActivateLayer( selection._sensorType );
		}
	}

	private void ActivateLayer( SO_PlanetConfig.ESensorType type )
	{
		if( _toggles != null )
		{
			for( int i = 0; i < _toggles.Count; ++i )
			{
				if( _toggles[i]._sensorType == type && _toggles[i]._toggle.gameObject.activeInHierarchy )
				{
					_toggles[i]._toggle.isOn = true;
				}
			}
		}
	}

	private void Start()
	{
		StartToggleListeners();
		
		// Also start listening for tutorial control overrides
		EventBus.StartListening( EventBus.EEventType.TUT_HideAllLayers, OnGlobalEvent_TUTHideAllLayers );
		EventBus.StartListening( EventBus.EEventType.TUT_ShowLayer, OnGlobalEvent_TUTShowLayer );
		EventBus.StartListening( EventBus.EEventType.TUT_ActivateLayer, OnGlobalEvent_TUTActivateLayer );
		EventBus.StartListening( EventBus.EEventType.SatelliteCardSelected, OnGlobalEvent_SatelliteCardSelected );

	}

	private void OnDestroy()
	{
		EventBus.StopListening( EventBus.EEventType.TUT_HideAllLayers, OnGlobalEvent_TUTHideAllLayers );
		EventBus.StopListening( EventBus.EEventType.TUT_ShowLayer, OnGlobalEvent_TUTShowLayer );
		EventBus.StopListening( EventBus.EEventType.TUT_ActivateLayer, OnGlobalEvent_TUTActivateLayer );
		EventBus.StopListening( EventBus.EEventType.SatelliteCardSelected, OnGlobalEvent_SatelliteCardSelected );
	}
}