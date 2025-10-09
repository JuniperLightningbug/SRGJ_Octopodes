using MM;
using NaughtyAttributes;
using UnityEngine;

public class GameManager : StandaloneSingletonBase<GameManager>
{
	// Child singletons
	[SerializeField] private PlanetManager _planetManager;
	public PlanetManager PlanetManagerInstance => _planetManager;
	
	[SerializeField] private SatelliteManager _satelliteManager;
	public SatelliteManager SatelliteManagerInstance => _satelliteManager;
	
	[SerializeField] private SatelliteDeck _satelliteDeck = new SatelliteDeck();
	
	[SerializeField, OnValueChanged( "Inspector_ChangedCurrentSensorType" )]
	private SO_PlanetConfig.ESensorType _inspectorInputSensorType = SO_PlanetConfig.ESensorType.INVALID;
	[SerializeField, ReadOnly]
	private SO_PlanetConfig.ESensorType _currentSensorType = SO_PlanetConfig.ESensorType.INVALID;
	
	public SO_PlanetConfig.ESensorType CurrentSensorType
	{
		get => _currentSensorType;
		private set
		{
			if( _currentSensorType != value )
			{
				_currentSensorType = value;
				EventBus.Invoke( this, EventBus.EEventType.ActiveSensorTypeChanged, _currentSensorType );
				EventBus.Invoke( this, EventBus.EEventType.PostActiveSensorTypeChanged, _currentSensorType );
			}
		}
	}

	// Static accessor
	public static SO_PlanetConfig.ESensorType TryGetCurrentSensorType()
	{
		GameManager gameManagerInstance = GameManager.InstanceOrNull;
		return gameManagerInstance ?
			gameManagerInstance.CurrentSensorType :
			SO_PlanetConfig.ESensorType.INVALID;
	}

#region Runtime Debug Inspector Inputs
	
	private void Inspector_ChangedCurrentSensorType()
	{
		CurrentSensorType = _inspectorInputSensorType;
	}

	[Button( "Draw 1" )]
	private void DrawOne()
	{
		if( _satelliteDeck != null )
		{
			_satelliteDeck.DrawSatellites( 1 );
		}
	}
	
	[Button( "Draw 3" )]
	private void DrawThree()
	{
		if( _satelliteDeck != null )
		{
			_satelliteDeck.DrawSatellites( 3 );
		}
	}
	
#endregion

#region Callbacks (mostly relays from UI events)

	private void OnGlobalEvent_UIChangeActiveSensorType( EventBus.EventContext context, object obj = null )
	{
		if( obj != null )
		{
			CurrentSensorType = (SO_PlanetConfig.ESensorType)obj;
			// This posts new events automatically
		}
	}

	private void OnGlobalEvent_UIClearActivePlanet( EventBus.EventContext context, object obj = null )
	{
		if( _planetManager )
		{
			_satelliteManager.ClearSatellites();
			_planetManager.ClearActivePlanet();
		}
	}

	private void OnGlobalEvent_UICreatePlanet( EventBus.EventContext context, object obj = null )
	{
		if( _planetManager )
		{
			_planetManager.TryCreatePlanet();
		}
	}

	private void OnGlobalEvent_UIGoToNextPlanet( EventBus.EventContext context, object obj = null )
	{
		if( _planetManager )
		{
			_satelliteManager.ClearSatellites();
			_planetManager.GoToNextPlanet( true, false );
		}
	}

#endregion

#region MonoBehaviour

	void OnEnable()
	{
		EventBus.StartListening( EventBus.EEventType.UI_ChangeActiveSensorType, OnGlobalEvent_UIChangeActiveSensorType );
		EventBus.StartListening( EventBus.EEventType.UI_ClearActivePlanet, OnGlobalEvent_UIClearActivePlanet );
		EventBus.StartListening( EventBus.EEventType.UI_CreatePlanet, OnGlobalEvent_UICreatePlanet );
		EventBus.StartListening( EventBus.EEventType.UI_NextPlanet, OnGlobalEvent_UIGoToNextPlanet );
	}

	void OnDisable()
	{
		EventBus.StopListening( EventBus.EEventType.UI_ChangeActiveSensorType, OnGlobalEvent_UIChangeActiveSensorType );
		EventBus.StopListening( EventBus.EEventType.UI_ClearActivePlanet, OnGlobalEvent_UIClearActivePlanet );
		EventBus.StopListening( EventBus.EEventType.UI_CreatePlanet, OnGlobalEvent_UICreatePlanet );
		EventBus.StopListening( EventBus.EEventType.UI_NextPlanet, OnGlobalEvent_UIGoToNextPlanet );
	}

#endregion

}
