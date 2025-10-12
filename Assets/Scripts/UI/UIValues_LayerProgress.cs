using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIValues_LayerProgress : UIValuesBase, IPointerEnterHandler, IPointerExitHandler
{

	protected override bool BUpdateWhenDisabled => true;

	public Slider _slider;
	public SO_PlanetConfig.ESensorType _type;
	public float _discoveryProgress;
	[SerializeField] public Outline _outline;
	[SerializeField] public Toggle _toggle;

	protected override void UpdateDataFromEvent( EventBus.EventContext context, object obj = null )
	{
		if( context._eventType == EventBus.EEventType.OnChanged_LayerDiscovery &&
		    obj is Dictionary<SO_PlanetConfig.ESensorType, float> newValues )
		{
			newValues.TryGetValue( _type, out _discoveryProgress );
		}
		else if( context._eventType == EventBus.EEventType.UI_CreatePlanet )
		{
			_discoveryProgress = 0.0f;
		}
	}
	
	private void OnGlobalEvent_TUTActivateLayer( EventBus.EventContext context, object obj = null )
	{
		if( obj is SO_PlanetConfig.ESensorType type && _toggle != null )
		{
			_toggle.isOn = true;
		}
	}
	
	private void OnGlobalEvent_SatelliteCardSelected( EventBus.EventContext context, object obj = null )
	{
		// If a satellite card is selected, switch to the corresponding view type
		if( obj is SO_Satellite satellite && satellite._sensorType == _type && _toggle != null )
		{
			_toggle.isOn = true;
		}
	}

	private void OnGlobalEvent_TUTHideAllLayers( EventBus.EventContext context, object obj = null )
	{
		_toggle.isOn = false;
		gameObject.SetActive( false );
	}

	private void OnGlobalEvent_TUTShowLayer( EventBus.EventContext context, object obj = null )
	{
		if( obj is SO_PlanetConfig.ESensorType type && _toggle != null && type == _type )
		{
			_toggle.isOn = false;
			gameObject.SetActive( true );
		}
	}
	
	private void OnToggleValueChanged( bool bOn )
	{
		if( _toggle )
		{
			if( bOn )
			{
				EventBus.Invoke( this, EventBus.EEventType.UI_ActivateSensorView, _type );
			}
			else
			{
				EventBus.Invoke( this, EventBus.EEventType.UI_DeactivateSensorView, _type );
			}
		}
	}

	protected override void UpdateDisplayInternal()
	{
		if( _slider )
		{
			_slider.value = _discoveryProgress;
		}
	}


	public void OnPointerEnter( PointerEventData eventData )
	{
		if( _outline )
		{
			_outline.enabled = true;
		}
	}

	public void OnPointerExit( PointerEventData eventData )
	{
		if( _outline )
		{
			_outline.enabled = false;
		}
	}
	
	void Awake()
	{
		if( _outline )
		{
			_outline.enabled = false;
		}

		if( !_toggle )
		{
			_toggle = GetComponent<Toggle>();
		}
	}
	
	public void Initialise()
	{
		if( _toggle )
		{
			_toggle.onValueChanged.AddListener( OnToggleValueChanged );
		}
		
		// Also start listening for tutorial control overrides
		EventBus.StartListening( EventBus.EEventType.TUT_HideAllLayers, OnGlobalEvent_TUTHideAllLayers );
		EventBus.StartListening( EventBus.EEventType.TUT_ShowLayer, OnGlobalEvent_TUTShowLayer );
		EventBus.StartListening( EventBus.EEventType.TUT_ActivateLayer, OnGlobalEvent_TUTActivateLayer );
		EventBus.StartListening( EventBus.EEventType.SatelliteCardSelected, OnGlobalEvent_SatelliteCardSelected );

	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		
		if( _toggle )
		{
			_toggle.onValueChanged.RemoveListener( OnToggleValueChanged );
		}
		
		EventBus.StopListening( EventBus.EEventType.TUT_HideAllLayers, OnGlobalEvent_TUTHideAllLayers );
		EventBus.StopListening( EventBus.EEventType.TUT_ShowLayer, OnGlobalEvent_TUTShowLayer );
		EventBus.StopListening( EventBus.EEventType.TUT_ActivateLayer, OnGlobalEvent_TUTActivateLayer );
		EventBus.StopListening( EventBus.EEventType.SatelliteCardSelected, OnGlobalEvent_SatelliteCardSelected );
	}
}
