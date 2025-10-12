using UnityEngine;
using UnityEngine.UI;

public class UISensorToggle : MonoBehaviour
{
	[SerializeField] private SO_PlanetConfig.ESensorType _type;
	[SerializeField] private Toggle _toggle;

	private void OnToggleValueChanged( bool value )
	{
		if( value )
		{
			EventBus.Invoke( this, EventBus.EEventType.UI_ActivateSensorView, _type );
		}
		else
		{
			EventBus.Invoke( this, EventBus.EEventType.UI_DeactivateSensorView, _type );
		}
	}

	void Initialise()
	{
		if( !_toggle )
		{
			_toggle = GetComponent<Toggle>();
		}

		if( _toggle )
		{
			_toggle.onValueChanged.AddListener( OnToggleValueChanged );
		}
		
		EventBus.StartListening( EventBus.EEventType.TUT_HideAllLayers, OnGlobalEvent_TUTHideAllLayers );
		EventBus.StartListening( EventBus.EEventType.TUT_ShowLayer, OnGlobalEvent_TUTShowLayer );
		EventBus.StartListening( EventBus.EEventType.TUT_ActivateLayer, OnGlobalEvent_TUTActivateLayer );
		EventBus.StartListening( EventBus.EEventType.SatelliteCardSelected, OnGlobalEvent_SatelliteCardSelected );
	}

	void OnDestroy()
	{
		if( _toggle )
		{
			_toggle.onValueChanged.RemoveListener( OnToggleValueChanged );
		}
		
		EventBus.StopListening( EventBus.EEventType.TUT_HideAllLayers, OnGlobalEvent_TUTHideAllLayers );
		EventBus.StopListening( EventBus.EEventType.TUT_ShowLayer, OnGlobalEvent_TUTShowLayer );
		EventBus.StopListening( EventBus.EEventType.TUT_ActivateLayer, OnGlobalEvent_TUTActivateLayer );
		EventBus.StopListening( EventBus.EEventType.SatelliteCardSelected, OnGlobalEvent_SatelliteCardSelected );
	}
	
	private void OnGlobalEvent_TUTHideAllLayers( EventBus.EventContext context, object obj = null )
	{
		gameObject.SetActive( false );
	}
	
	private void OnGlobalEvent_TUTShowLayer( EventBus.EventContext context, object obj = null )
	{
		if( obj is SO_PlanetConfig.ESensorType type && type == _type )
		{
			gameObject.SetActive( true );
		}
	}

	private void OnGlobalEvent_TUTActivateLayer( EventBus.EventContext context, object obj = null )
	{
		if( obj is SO_PlanetConfig.ESensorType type && _toggle && type == _type )
		{
			_toggle.isOn = true;
		}
	}

	private void OnGlobalEvent_SatelliteCardSelected( EventBus.EventContext context, object obj = null )
	{
		// If a satellite card is selected, switch to the corresponding view type
		if( obj is SO_PlanetConfig.ESensorType type && _toggle && type == _type )
		{
			_toggle.isOn = true;
		}
	}
}
