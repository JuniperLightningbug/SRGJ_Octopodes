using System;
using System.Collections;
using MM;
using NaughtyAttributes;
using Shapes;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

public class GameManager : StandaloneSingletonBase<GameManager>
{
	
	// Child singletons
	[SerializeField] private PlanetManager _planetManager;
	public PlanetManager PlanetManagerInstance => _planetManager;
	
	[SerializeField] private SatelliteManager _satelliteManager;
	public SatelliteManager SatelliteManagerInstance => _satelliteManager;

	[SerializeField, Expandable] private SO_SatelliteDeck _satelliteDeck;
	
	[SerializeField] private TutorialInstance _tutorialInstance;
	private bool BTutorialIsActive => _tutorialInstance != null && _tutorialInstance._bIsActive;

	[SerializeField] private Object _tutorialPrefab;

	[SerializeField] private bool _bActivateOnStart = true;
	[SerializeField] private float _progressWinThreshold = 0.8f;
	[SerializeField] private bool _bPlanetIsCompleted = false;
	private Coroutine _drawCardsCoroutine = null;
	private const float kCardDrawDelay = 3.0f;

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

		_bPlanetIsCompleted = false;
	}

	private void OnDrawGizmos()
	{
		if( _bDrawStormGizmos && _stormTimer != null )
		{
			_stormTimer.DrawGizmos();
		}
	}

	public void DrawNextCards()
	{
		_satelliteDeck?.DrawSatellites();
	}

	public void DrawNextCardsDelayed( float delay = kCardDrawDelay )
	{
		if( _drawCardsCoroutine != null )
		{
			StopCoroutine( _drawCardsCoroutine );
		}

		_drawCardsCoroutine = StartCoroutine( DrawCardsDelayedCoroutine( delay ) );
	}

	public IEnumerator DrawCardsDelayedCoroutine( float delay )
	{
		yield return new WaitForSeconds( delay );
		DrawNextCards();
	}

	private void TryCreatePlanet()
	{
		if( _planetManager )
		{
			Planet newPlanet = _planetManager.TryCreatePlanet();

			if( newPlanet?.PlanetConfig )
			{
				_satelliteDeck = newPlanet.PlanetConfig._satelliteDeck;
				_satelliteDeck?.Reset();
				_satelliteDeck?.Shuffle();

				if( newPlanet.PlanetConfig._bActivateTutorial && _tutorialPrefab )
				{
					_tutorialInstance?.Cleanup();
					GameObject newTutorialInstance = Instantiate( _tutorialPrefab, transform) as GameObject;
					_tutorialInstance = newTutorialInstance?.GetComponent<TutorialInstance>();
					_tutorialInstance?.Initialise();
					_tutorialInstance?.StartTutorial();
				}

				_satelliteDeck = newPlanet.PlanetConfig._satelliteDeck;
				_satelliteDeck?.Shuffle();
				if( !BTutorialIsActive )
				{
					DrawNextCardsDelayed();
				}

				if( _stormTimer != null )
				{
					_stormTimer.Reset( newPlanet );

					if( BTutorialIsActive )
					{
						_stormTimer.Deactivate();
					}
					else
					{
						_stormTimer.Start();
					}
				}
			}
		}
	}

	private void ClearActivePlanet()
	{
		if( _stormTimer != null )
		{
			_stormTimer.Clear();
		}

		if( _tutorialInstance )
		{
			MM.ComponentUtils.DestroyPlaymodeSafe( _tutorialInstance?.gameObject );
		}
		
		_planetManager?.ClearActivePlanet();

		_bPlanetIsCompleted = false;
		
		EventBus.Invoke( EventBus.EEventType.PostClearActivePlanet );
	}

	private void GoToNextPlanet()
	{
		ClearActivePlanet();
		_planetManager?.GoToNextPlanet();
		TryCreatePlanet(); // Try to create immediately
	}

	private void GoToFirstPlanet()
	{
		ClearActivePlanet();
		_planetManager?.GoToFirstPlanet();
		TryCreatePlanet(); // Try to create immediately
	}

#region Runtime Debug Inspector Inputs
	
	[Button( "Draw from next progress value" )]
	private void Inspector_DrawNextSet()
	{
		_satelliteDeck?.TryDrawCardsFromProgress( 2.0f );
	}

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
		GoToNextPlanet();
	}
	
	private void OnGlobalEvent_UIPauseTime( EventBus.EventContext context, object obj = null )
	{
		Time.timeScale = 0.0f;
	}
	
	private void OnGlobalEvent_UIUnpauseTime( EventBus.EventContext context, object obj = null )
	{
		Time.timeScale = _normalTimeScale;
	}
	
	private void OnGlobalEvent_TUTDrawCards( EventBus.EventContext context, object obj = null )
	{
		if( obj is int num )
		{
			_satelliteDeck?.DrawSatellites( num ); // Skip the progress try draw so we can safely resume after the tutorial
		}
	}

	private void OnGlobalEvent_TUTStartFirstStormWarning( EventBus.EventContext context, object obj = null )
	{
		_stormTimer?.ChangeState( StormTimer.EStormState.Warning );
	}

	private void OnGlobalEvent_StormEnded( EventBus.EventContext context, object obj = null )
	{
		if( !BTutorialIsActive )
		{
			DrawNextCardsDelayed();
		}
	}
	
	private void OnGlobalEvent_OnChangedPlanetProgress( EventBus.EventContext context, object obj = null )
	{
		if( !_bPlanetIsCompleted && obj is float newProgress )
		{
			if( _planetManager?.ActivePlanet?.PlanetConfig != null && newProgress > _progressWinThreshold )
			{
				_bPlanetIsCompleted = true;
				EventBus.Invoke( this, EventBus.EEventType.PlanetCompleted,
					_planetManager.ActivePlanet.PlanetConfig );
			}
			_satelliteDeck?.TryDrawCardsFromProgress( newProgress );
		}
	}

	private void OnGlobalEvent_DebugCompletePlanet( EventBus.EventContext context, object obj = null )
	{
		OnGlobalEvent_OnChangedPlanetProgress( context, 2.0f );
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
		EventBus.StartListening( EventBus.EEventType.TUT_DrawCards, OnGlobalEvent_TUTDrawCards );
		EventBus.StartListening( EventBus.EEventType.TUT_StartFirstStormWarning, OnGlobalEvent_TUTStartFirstStormWarning );
		EventBus.StartListening( EventBus.EEventType.OnChanged_PlanetProgress, OnGlobalEvent_OnChangedPlanetProgress );
		EventBus.StartListening( EventBus.EEventType.DEBUG_CompletePlanet, OnGlobalEvent_DebugCompletePlanet );
		EventBus.StartListening( EventBus.EEventType.StormEnded, OnGlobalEvent_StormEnded );
	}

	void OnDisable()
	{
		EventBus.StopListening( EventBus.EEventType.UI_ClearActivePlanet, OnGlobalEvent_UIClearActivePlanet );
		EventBus.StopListening( EventBus.EEventType.UI_CreatePlanet, OnGlobalEvent_UICreatePlanet );
		EventBus.StopListening( EventBus.EEventType.UI_NextPlanet, OnGlobalEvent_UIGoToNextPlanet );
		EventBus.StopListening( EventBus.EEventType.UI_PauseTime, OnGlobalEvent_UIPauseTime );
		EventBus.StopListening( EventBus.EEventType.UI_UnpauseTime, OnGlobalEvent_UIUnpauseTime );
		EventBus.StopListening( EventBus.EEventType.TUT_DrawCards, OnGlobalEvent_TUTDrawCards );
		EventBus.StopListening( EventBus.EEventType.TUT_StartFirstStormWarning, OnGlobalEvent_TUTStartFirstStormWarning );
		EventBus.StopListening( EventBus.EEventType.OnChanged_PlanetProgress, OnGlobalEvent_OnChangedPlanetProgress );
		EventBus.StopListening( EventBus.EEventType.DEBUG_CompletePlanet, OnGlobalEvent_DebugCompletePlanet );
		EventBus.StopListening( EventBus.EEventType.StormEnded, OnGlobalEvent_StormEnded );
	}

	void Update()
	{
		if( !_bPauseStormTimer )
		{
			_stormTimer?.Update( Time.deltaTime );
		}

		if( Keyboard.current.backslashKey.wasPressedThisFrame )
		{
			DrawNextCards();
		}
	}

	void Start()
	{
		if( _bActivateOnStart )
		{
			GoToFirstPlanet();
		}
	}

#endregion

}
