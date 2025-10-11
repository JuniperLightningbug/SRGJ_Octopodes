using UnityEngine;
using UnityEngine.UI;

/**
 * UI wrapper for the summary display
 */
public class UIPlanetEndSummary : MonoBehaviour
{
	public Object _fallbackSummaryPrefab;
	public GameObject _summaryObjectCreated;
	
	private void OnGlobalEvent_PlanetCompleted( EventBus.EventContext context, object obj = null )
	{
		ClearSummaryObject();
		
		Object summaryPrefab = null;
		if( obj is SO_PlanetConfig planetConfig && planetConfig._summaryUIPrefab )
		{
			summaryPrefab = planetConfig._summaryUIPrefab;
		}
		else if( _fallbackSummaryPrefab )
		{
			summaryPrefab = _fallbackSummaryPrefab;
		}

		if( summaryPrefab )
		{
			ClearSummaryObject();

			_summaryObjectCreated = Instantiate( summaryPrefab, transform ) as GameObject;
			if( _summaryObjectCreated )
			{
				Button nextPlanetButton = _summaryObjectCreated.GetComponentInChildren<Button>();
				nextPlanetButton?.onClick.AddListener( OnClicked_NextPlanet );
			}
		}
	}

	public void Initialise()
	{
		EventBus.StartListening( EventBus.EEventType.PlanetCompleted, OnGlobalEvent_PlanetCompleted );
	}

	void OnDestroy()
	{
		EventBus.StopListening( EventBus.EEventType.PlanetCompleted, OnGlobalEvent_PlanetCompleted );
	}

	public void ClearSummaryObject()
	{
		if( _summaryObjectCreated )
		{
			Destroy( _summaryObjectCreated );
		}
		_summaryObjectCreated = null;
	}

	public void OnClicked_NextPlanet()
	{
		ClearSummaryObject();
		EventBus.Invoke( EventBus.EEventType.UI_NextPlanet );
	}
}
