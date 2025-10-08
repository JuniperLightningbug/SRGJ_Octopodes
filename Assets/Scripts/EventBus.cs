using System;
using System.Collections.Generic;

public static class EventBus
{
	[System.Serializable]
	public enum EEventType
	{
		INVALID,
		
		// UI (Usually dispatches through GameManager as source-of-truth)
		UI_ChangeActiveSensorType, // SO_PlanetConfig.ESensorType toSensorType
		UI_ClearActivePlanet,
		UI_CreatePlanet,
		UI_NextPlanet,
		
		// Value Changes
		OnChanged_LayerDiscovery, // Dictionary<SO_PlanetConfig.ESensorType, float>
		
		// Game Manager
		ActiveSensorTypeChanged, // SO_PlanetConfig.ESensorType toSensorType
		SpawnNewPlanet,
		ClearActivePlanet,
		
		// Gameplay Events
		LaunchedSatellite, // (SO_PlanetConfig.ESensorType type, Transform satelliteTransform) newSatellite
		
		// Value late-updates for UI
		PostActiveSensorTypeChanged, // SO_PlanetConfig.ESensorType toSensorType
		
		COUNT,
	}

	public struct EventContext
	{
		public EEventType _eventType;
		public Object _fromObject;

		public static EventContext CreateFromCaller( object fromObject, EEventType type )
		{
			return new EventContext()
			{
				_eventType = type,
				_fromObject = fromObject,
			};
		}
	}

	private static Dictionary<EEventType, Action<EventContext, object>> _events = new Dictionary<EEventType, Action<EventContext, object>>();
	
	public static void StartListening( EEventType eventType, Action<EventContext, object> callback )
	{
		if( !_events.TryAdd( eventType, callback ) )
		{
			if( _events[eventType] == null )
			{
				_events[eventType] = callback;
			}
			else
			{
				_events[eventType] += callback;
			}
		}
	}

	public static void StopListening( EEventType eventType, Action<EventContext, object> callback )
	{
		if( _events.ContainsKey( eventType ) )
		{
			if( _events[eventType] != null )
			{
				_events[eventType] -= callback;
			}
		}
	}

	public static void Invoke( EEventType type, object arg = null )
	{
		Invoke( EventContext.CreateFromCaller( null, type ), arg );
	}

	public static void Invoke( object fromClass, EEventType eventType, object arg = null )
	{
		Invoke( EventContext.CreateFromCaller( fromClass, eventType ), arg );
	}
	
	public static void Invoke( EventContext context, object arg = null )
	{
		if( _events.ContainsKey( context._eventType ) )
		{
			if( _events[context._eventType] != null )
			{
				_events[context._eventType].Invoke( context, arg );
			}
		}
	}

	public static int GetNumListeners( EEventType eventType )
	{
		Action<EventContext, object> callback;
		if( _events.TryGetValue( eventType, out callback ) )
		{
			return callback.GetInvocationList().Length;
		}

		return 0;
	}
}