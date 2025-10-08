using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIValues_LayerProgress : UIValuesBase
{
	protected override EventBus.EEventType[] UpdateOnEvents
	{
		get
		{
			return new EventBus.EEventType[]
			{
				EventBus.EEventType.OnChanged_LayerDiscovery,
			};
		}
	}

	protected override bool BUpdateWhenDisabled => true;

	public Slider _slider;
	public SO_PlanetConfig.ESensorType _type;
	public float _discoveryProgress;

	protected override void UpdateDataFromEvent( EventBus.EventContext context, object obj = null )
	{
		if( obj != null )
		{
			Dictionary<SO_PlanetConfig.ESensorType, float> newValues =
				(Dictionary<SO_PlanetConfig.ESensorType, float>)obj;
			newValues.TryGetValue( _type, out _discoveryProgress );
		}
	}

	protected override void UpdateDisplayInternal()
	{
		if( _slider )
		{
			_slider.value = _discoveryProgress;
		}
	}


}
