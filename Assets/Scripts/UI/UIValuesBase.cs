using System.Collections.Generic;
using TMPro;
using UnityEngine;

/**
 * Generic class for handling simple data visualisations in the UI.
 * Reduces boilerplate code, but still a manual process.
 *
 * To implement:
 * - Extend this class
 * - Override UpdateOnEvents with any events that signal a change in data (add these events if needed)
 * - (optionally) Override BUpdateWhenDisabled = true if the data isn't globally accessible, and is cached locally
 * -- In the above case, use UpdateDataFromEvent to cache data after changes are made
 * - Override UpdateDisplayInternal with any logic needed for displaying data
 */
public abstract class UIValuesBase : MonoBehaviour
{
	protected virtual EventBus.EEventType[] UpdateOnEvents
	{
		get;
	}

	protected virtual bool BUpdateWhenDisabled => false;

#region Event Listeners Bookkeeping
	public virtual void OnEnable()
	{
		if( !BUpdateWhenDisabled )
		{
			StartEventListeners();
		}
	}

	public virtual void OnDisable()
	{
		if( !BUpdateWhenDisabled )
		{
			StopEventListeners();
		}
	}

	public virtual void Start()
	{
		if( BUpdateWhenDisabled )
		{
			StartEventListeners();
		}
	}

	public virtual void OnDestroy()
	{
		if( BUpdateWhenDisabled )
		{
			StopEventListeners();
		}
	}

	protected void StartEventListeners()
	{
		if( UpdateOnEvents != null )
		{
			for( int i = 0; i < UpdateOnEvents.Length; ++i )
			{
				EventBus.StopListening( UpdateOnEvents[i], OnGlobalEvent_UpdateDisplay );
			}
		}
		UpdateDisplay();
	}

	protected void StopEventListeners()
	{
		if( UpdateOnEvents != null )
		{
			for( int i = 0; i < UpdateOnEvents.Length; ++i )
			{
				EventBus.StopListening( UpdateOnEvents[i], OnGlobalEvent_UpdateDisplay );
			}
		}
	}
#endregion

#region Display Handling

	

#endregion
	
	private void OnGlobalEvent_UpdateDisplay( EventBus.EventContext context, object obj = null )
	{
		UpdateDataFromEvent( context, obj );
		UpdateDisplay();
	}

	public void Toggle( bool on )
	{
		UpdateDisplay( true );
		gameObject.SetActive( on );
	}
	
	public void UpdateDisplay( bool bForce = false )
	{
		if( bForce || gameObject.activeInHierarchy )
		{
			UpdateDisplayInternal();
		}
	}

	protected virtual void UpdateDataFromEvent( EventBus.EventContext context, object obj = null )
	{
		
	}

	protected virtual void UpdateDisplayInternal()
	{

	}

	protected static bool TryFillTMPList( List<TextMeshProUGUI> inList_TMP, List<string> inList_Text )
	{
		// Add individual line TMP text
		for( int i = 0; i < inList_TMP.Count; ++i )
		{
			if( inList_TMP[i] )
			{
				if( i < inList_Text.Count )
				{
					// Add the data to the TMP component
					inList_TMP[i].SetText( inList_Text[i] );
					inList_TMP[i].gameObject.SetActive( true );
				}
				else
				{
					// There's no data to add - hide the TMP component
					inList_TMP[i].SetText( "" );
					inList_TMP[i].gameObject.SetActive( false );
				}
			}
		}

		return inList_TMP.Count >= inList_Text.Count;
	}
}
