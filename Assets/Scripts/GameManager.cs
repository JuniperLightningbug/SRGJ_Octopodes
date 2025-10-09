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

	// Static accessor
	public static SO_PlanetConfig.ESensorType TryGetCurrentSensorViewType()
	{
		Planet currentPlanet = InstanceOrNull?._planetManager?.ActivePlanet;
		return currentPlanet ?
			currentPlanet.CurrentSensorType :
			SO_PlanetConfig.ESensorType.INVALID;
	}

#region Runtime Debug Inspector Inputs

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
		EventBus.StartListening( EventBus.EEventType.UI_ClearActivePlanet, OnGlobalEvent_UIClearActivePlanet );
		EventBus.StartListening( EventBus.EEventType.UI_CreatePlanet, OnGlobalEvent_UICreatePlanet );
		EventBus.StartListening( EventBus.EEventType.UI_NextPlanet, OnGlobalEvent_UIGoToNextPlanet );
	}

	void OnDisable()
	{
		EventBus.StopListening( EventBus.EEventType.UI_ClearActivePlanet, OnGlobalEvent_UIClearActivePlanet );
		EventBus.StopListening( EventBus.EEventType.UI_CreatePlanet, OnGlobalEvent_UICreatePlanet );
		EventBus.StopListening( EventBus.EEventType.UI_NextPlanet, OnGlobalEvent_UIGoToNextPlanet );
	}

#endregion

}
