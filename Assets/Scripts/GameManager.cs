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
	[SerializeField] private StormTimer _stormTimer = new StormTimer();

	private void ResetStormTimer( SO_StormTimings newTimings )
	{
		if( _stormTimer == null )
		{
			_stormTimer = new StormTimer();
		}

		if( !newTimings )
		{
			if( _stormTimer.BStormIsActive )
			{
				EventBus.Invoke( this, EventBus.EEventType.StormEnded );
			}
			_stormTimer.Deactivate();
		}
		else
		{
			_stormTimer.Reset( newTimings );
		}
	}

	private void TryCreatePlanet()
	{
		if( _planetManager )
		{
			Planet newPlanet = _planetManager.TryCreatePlanet();
			if( newPlanet )
			{
				ResetStormTimer( newPlanet.PlanetConfig._stormTimings );
			}
		}
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
	
	[SerializeField, ReadOnly] private float _normalTimeScale = 1.0f;
	
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
		TryCreatePlanet();
	}

	private void OnGlobalEvent_UIGoToNextPlanet( EventBus.EventContext context, object obj = null )
	{
		if( _planetManager )
		{
			_satelliteManager.ClearSatellites();
			_planetManager.GoToNextPlanet();
			TryCreatePlanet(); // Try to create immediately
		}
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

	void Awake()
	{
		_normalTimeScale = Time.timeScale;
	}

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
		if( _stormTimer != null && _stormTimer.UpdateTimer( Time.deltaTime ) )
		{
			switch( _stormTimer.CurrentState )
			{
				case StormTimer.EStormState.Calm:
				{
					break;
				}
				case StormTimer.EStormState.Warning:
				{
					EventBus.Invoke( this, EventBus.EEventType.StormWarning, _stormTimer.StateTimeRemaining );
					break;
				}
				case StormTimer.EStormState.Active:
				{
					EventBus.Invoke( this, EventBus.EEventType.StormActive, _stormTimer.StateTimeRemaining );
					break;
				}
			}

			if( _stormTimer.PreviousState == StormTimer.EStormState.Active )
			{
				EventBus.Invoke( this, EventBus.EEventType.StormEnded );
			}

			Debug.Log( _stormTimer.CurrentState + ": " + _stormTimer.StateTimeRemaining );

		}
	}

#endregion

}
