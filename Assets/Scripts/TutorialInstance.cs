using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * When the tutorial is active, this class takes control of some events to handle orchestration.
 * It should be a component of GameManager, as a layer between the normal orchestration and the event outputs.
 */
[System.Serializable]
public class TutorialInstance : MonoBehaviour
{
	public bool _bIsActive = false;

	// Input data
	public List<SO_PlanetConfig.ESensorType> _tutorialSensorsOrder = new List<SO_PlanetConfig.ESensorType>()
	{
		SO_PlanetConfig.ESensorType.Aurora,
		SO_PlanetConfig.ESensorType.MassSpec,
		SO_PlanetConfig.ESensorType.Radar,
	};

	public List<int> _tutorialCardDrawQuantities = new List<int>()
	{
		1,
		2,
		1,
		3,
	};
	
	// Runtime flags
	public int _numSatellitesCreated = 0;
	public bool _bFirstStormPassed = false;
	public bool _bWasCameraOrbitTutorialCompleted = false;
	public bool _bHasSatelliteEverBeenSelected = false;

	private Coroutine _tutorialCoroutine;

	public void Initialise()
	{
		if( _bIsActive )
		{
			Cleanup();
		}
		_numSatellitesCreated = 0;
		_bFirstStormPassed = false;
		_bIsActive = true;
		_bWasCameraOrbitTutorialCompleted = false;
		_bHasSatelliteEverBeenSelected = false;
	}

	public void StartTutorial()
	{
		if( _tutorialCoroutine != null )
		{
			StopCoroutine( _tutorialCoroutine );
		}

		_tutorialCoroutine = StartCoroutine( TutorialSequence() );
	}

	public void Cleanup()
	{
		_bIsActive = false;
		if( _tutorialCoroutine != null )
		{
			StopCoroutine( _tutorialCoroutine );
		}
		EventBus.Invoke( this, EventBus.EEventType.TUT_Popup_HideAll );
	}

	void OnDestroy()
	{
		Cleanup();
	}

	private void OnEnable()
	{
		EventBus.StartListening( EventBus.EEventType.LaunchedSatellite, OnGlobalEvent_LaunchedSatellite );
		EventBus.StartListening( EventBus.EEventType.StormEnded, OnGlobalEvent_StormEnded );
		EventBus.StartListening( EventBus.EEventType.TUT_Callback_CameraControlsComplete, OnGlobalEvent_TUTCallbackCameraControlsComplete );
		EventBus.StartListening( EventBus.EEventType.SatelliteCardSelected, OnGlobalEvent_TUTCallbackSatelliteChosen );
	}
	private void OnDisable()
	{
		EventBus.StopListening( EventBus.EEventType.LaunchedSatellite, OnGlobalEvent_LaunchedSatellite );
		EventBus.StopListening( EventBus.EEventType.StormEnded, OnGlobalEvent_StormEnded );
		EventBus.StopListening( EventBus.EEventType.TUT_Callback_CameraControlsComplete, OnGlobalEvent_TUTCallbackCameraControlsComplete );
		EventBus.StopListening( EventBus.EEventType.SatelliteCardSelected, OnGlobalEvent_TUTCallbackSatelliteChosen );
	}

#region Callbacks

	private void OnGlobalEvent_LaunchedSatellite( EventBus.EventContext context, object obj = null )
	{
		_numSatellitesCreated++;
	}
	
	private void OnGlobalEvent_StormEnded( EventBus.EventContext context, object obj = null )
	{
		_bFirstStormPassed = true;
	}
	
	private void OnGlobalEvent_TUTCallbackCameraControlsComplete( EventBus.EventContext context, object obj = null )
	{
		_bWasCameraOrbitTutorialCompleted = true;
	}
	
	private void OnGlobalEvent_TUTCallbackSatelliteChosen( EventBus.EventContext context, object obj = null )
	{
		_bHasSatelliteEverBeenSelected = true;
	}

#endregion

	/*
	 * Note on this implementation:
	 * Tutorial popups always set timescale to 0 while active.
	 * This means we shouldn't have to track when the popups are closed - we just yield return waitforseconds after
	 * each one.
	 */
	public IEnumerator TutorialSequence()
	{
		// One big coroutine to rule them all
		yield return new WaitForEndOfFrame();
		
		// Hide Layers
		EventBus.Invoke( this, EventBus.EEventType.TUT_HideAllLayers );
		
		// Disable deck interaction
		EventBus.Invoke( this, EventBus.EEventType.TUT_DisableDeckInteraction );
		
		// Wait: 1 second
		yield return new WaitForSeconds( 1.0f );
		
		// POPUP: Camera controls
		EventBus.Invoke( this, EventBus.EEventType.TUT_Popup_CameraControls, false );
		
		// Wait: 3 seconds
		yield return new WaitForSeconds( 4.0f );

		yield return new WaitUntil( () => _bWasCameraOrbitTutorialCompleted );
		EventBus.Invoke( this, EventBus.EEventType.TUT_Popup_HideAll );

		// Wait: 3 seconds
		yield return new WaitForSeconds( 1.0f );

		// Draw first card
		EventBus.Invoke( this, EventBus.EEventType.TUT_DrawCards, 1 );
		
		// Wait: 0.5 second
		yield return new WaitForSeconds( 0.5f );
			
		// Enable deck interaction
		EventBus.Invoke( this, EventBus.EEventType.TUT_EnableDeckInteraction );
	
		// POPUP: Satellites
		EventBus.Invoke( this, EventBus.EEventType.TUT_Popup_Satellites, false );

		yield return new WaitUntil( () => _bHasSatelliteEverBeenSelected );
		
		// POPUP: Orbitals
		EventBus.Invoke( this, EventBus.EEventType.TUT_Popup_HideAll );
		EventBus.Invoke( this, EventBus.EEventType.TUT_Popup_Orbitals, false );

		// Wait until first orbital created
		yield return new WaitUntil( () => _numSatellitesCreated > 0 );
		
		// Show first layer
		if( _tutorialSensorsOrder.Count > 0 )
		{
			EventBus.Invoke( this, EventBus.EEventType.TUT_ShowLayer, _tutorialSensorsOrder[0] );
			EventBus.Invoke( this, EventBus.EEventType.TUT_ActivateLayer, _tutorialSensorsOrder[0] );
		}
		
		// Wait 0.5 seconds
		yield return new WaitForSeconds( 3.0f );
		
		// Draw new cards
		EventBus.Invoke( this, EventBus.EEventType.TUT_DrawCards, 2 );
		
		// Show second layer
		if( _tutorialSensorsOrder.Count > 1 )
		{
			EventBus.Invoke( this, EventBus.EEventType.TUT_ShowLayer, _tutorialSensorsOrder[1] );
			
			// Wait 3 seconds
			yield return new WaitForSeconds( 3.0f );
			
			// POPUP: Layers
			EventBus.Invoke( this, EventBus.EEventType.TUT_Popup_HideAll );
			EventBus.Invoke( this, EventBus.EEventType.TUT_Popup_Layers, false );
		}
				
		// Wait for at least 2 second
		yield return new WaitForSeconds( 2.0f );
		
		// Wait until 3 orbitals created
		yield return new WaitUntil( () => _numSatellitesCreated > 2 );
		
		// Wait for at least 3 seconds
		yield return new WaitForSeconds( 2.0f );
		
		// Draw new cards
		EventBus.Invoke( this, EventBus.EEventType.TUT_DrawCards, 1 );
		
		// Wait for at least 3 seconds
		yield return new WaitForSeconds( 2.0f );
		
		// Wait until 4 orbitals created
		yield return new WaitUntil( () => _numSatellitesCreated > 3 );
				
		// Wait for at least 3 seconds
		yield return new WaitForSeconds( 4.0f );
		
		// POPUP: Storm
		EventBus.Invoke( this, EventBus.EEventType.TUT_Popup_HideAll );
		EventBus.Invoke( this, EventBus.EEventType.TUT_Popup_Storm, true );
		
		// Wait for 0.5 seconds (mainly just a validation that the popup has been closed)
		yield return new WaitForSeconds( 0.5f );

		// Start the storm warning phase
		EventBus.Invoke( this, EventBus.EEventType.TUT_StartFirstStormWarning );
		
		// Wait for storm end
		yield return new WaitUntil( () => _bFirstStormPassed );
		
		// Draw new cards
		EventBus.Invoke( this, EventBus.EEventType.TUT_DrawCards, 3 );
		
		// Show the next layer (should be the final one for the tutorial?)
		if( _tutorialSensorsOrder.Count > 2 )
		{
			EventBus.Invoke( this, EventBus.EEventType.TUT_ShowLayer, _tutorialSensorsOrder[2] );
		}

		// Wait for 1 second
		yield return new WaitForSeconds( 1.0f );
		
		// POPUP: Contextualised objective reminder
		EventBus.Invoke( this, EventBus.EEventType.TUT_Popup_HideAll );
		EventBus.Invoke( this, EventBus.EEventType.TUT_Popup_WinCondition, false );

		_bIsActive = false;

		// End of tutorial - carry on as normal from here
		yield return null;

	}
	
	// usually, sequence is something like:
	// spawn planet
	// wait 3 seconds
	// draw 3 cards
	// on event: draw 3 more cards
	// event ideas: storm end (maybe more if lost more satellites?), discovery %, or time
	
}
