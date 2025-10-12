using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIValues_PlanetProgress : UIValuesBase
{
	protected override EventBus.EEventType[] UpdateOnEvents
	{
		get
		{
			return new EventBus.EEventType[]
			{
				EventBus.EEventType.OnChanged_PlanetProgress,
				EventBus.EEventType.PostClearActivePlanet,
				EventBus.EEventType.PostSpawnNewPlanet,
			};
		}
	}
	
	protected override bool BUpdateWhenDisabled => true;

	public Slider _slider;
	public float _discoveryProgress;

	protected override void UpdateDataFromEvent( EventBus.EventContext context, object obj = null )
	{
		if( context._eventType == EventBus.EEventType.OnChanged_PlanetProgress &&
		    obj is float newProgressValue )
		{
			_discoveryProgress = newProgressValue / GameManager.kProgressWinThreshold;
		}
		else if( context._eventType == EventBus.EEventType.PostClearActivePlanet ||
		         context._eventType == EventBus.EEventType.PostSpawnNewPlanet )
		{
			_discoveryProgress = 0.0f;
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
