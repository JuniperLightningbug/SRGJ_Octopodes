using System;
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
	
	// Storm
	[SerializeField] private StormTimer _stormTimer;

	[SerializeField] private bool _bDrawStormGizmos = false;
	[SerializeField] private bool _bPauseStormTimer = false;

	protected override void Initialise()
	{
		_normalTimeScale = Time.timeScale;

		_stormTimer = new StormTimer();
		_stormTimer.Initialise();
	}

	private void OnDrawGizmos()
	{
		if( _bDrawStormGizmos && _stormTimer != null )
		{
			_stormTimer.DrawGizmos();
		}
	}

	private void TryCreatePlanet()
	{
		if( _planetManager )
		{
			Planet newPlanet = _planetManager.TryCreatePlanet();
			
			if( newPlanet && _stormTimer != null )
			{
				_stormTimer.Reset( newPlanet );
				_stormTimer.Start();
			}
		}
	}

	private void ClearActivePlanet()
	{
		if( _stormTimer != null )
		{
			_stormTimer.Clear();
		}
		
		_satelliteManager?.ClearSatellites();
		_planetManager?.GoToNextPlanet();
	}

#region Runtime Debug Inspector Inputs

	[Button( "Draw 1" )]
	private void Inspector_DrawOne()
	{
		_satelliteDeck?.DrawSatellites( 1 );
	}
	
	[Button( "Draw 3" )]
	private void Inspector_DrawThree()
	{
		_satelliteDeck?.DrawSatellites( 3 );
	}

	[Button( "Skip Storm State" )]
	private void Inspector_SkipStormState()
	{
		_stormTimer?.SkipState();
	}

	[Button( "Restart Current Storm Timer" )]
	private void Inspector_RestartCurrentStormTimer()
	{
		if( _stormTimer != null )
		{
			if( _planetManager.ActivePlanet )
			{
				_stormTimer.Reset( _planetManager.ActivePlanet );
			}
			else
			{
				_stormTimer.Deactivate();
				_stormTimer.Start();
			}
		}
	}

	[SerializeField, ReadOnly] private float _normalTimeScale = 1.0f;
	
#endregion

#region Callbacks (mostly relays from UI events)

	private void OnGlobalEvent_UIClearActivePlanet( EventBus.EventContext context, object obj = null )
	{
		ClearActivePlanet();
	}

	private void OnGlobalEvent_UICreatePlanet( EventBus.EventContext context, object obj = null )
	{
		TryCreatePlanet();
	}

	private void OnGlobalEvent_UIGoToNextPlanet( EventBus.EventContext context, object obj = null )
	{
		ClearActivePlanet();
		TryCreatePlanet(); // Try to create immediately
	}
	
	private void OnGlobalEvent_UIPauseTime( EventBus.EventContext context, object obj = null )
	{
		Time.timeScale = 0.0f;
	}
	
	private void OnGlobalEvent_UIUnpauseTime( EventBus.EventContext context, object obj = null )
	{
		Time.timeScale = _normalTimeScale;
	}

#endregion

#region MonoBehaviour

	void OnEnable()
	{
		EventBus.StartListening( EventBus.EEventType.UI_ClearActivePlanet, OnGlobalEvent_UIClearActivePlanet );
		EventBus.StartListening( EventBus.EEventType.UI_CreatePlanet, OnGlobalEvent_UICreatePlanet );
		EventBus.StartListening( EventBus.EEventType.UI_NextPlanet, OnGlobalEvent_UIGoToNextPlanet );
		EventBus.StartListening( EventBus.EEventType.UI_PauseTime, OnGlobalEvent_UIPauseTime );
		EventBus.StartListening( EventBus.EEventType.UI_UnpauseTime, OnGlobalEvent_UIUnpauseTime );
	}

	void OnDisable()
	{
		EventBus.StopListening( EventBus.EEventType.UI_ClearActivePlanet, OnGlobalEvent_UIClearActivePlanet );
		EventBus.StopListening( EventBus.EEventType.UI_CreatePlanet, OnGlobalEvent_UICreatePlanet );
		EventBus.StopListening( EventBus.EEventType.UI_NextPlanet, OnGlobalEvent_UIGoToNextPlanet );
		EventBus.StopListening( EventBus.EEventType.UI_PauseTime, OnGlobalEvent_UIPauseTime );
		EventBus.StopListening( EventBus.EEventType.UI_UnpauseTime, OnGlobalEvent_UIUnpauseTime );
	}

	void Update()
	{
		if( !_bPauseStormTimer )
		{
			_stormTimer?.Update( Time.deltaTime );
		}
	}

#endregion

}
