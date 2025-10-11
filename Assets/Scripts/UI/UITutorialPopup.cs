using UnityEngine;

public class UITutorialPopup : MonoBehaviour
{
	[SerializeField] public EventBus.EEventType _displayEvent;

	public void Initialise()
	{
		EventBus.StartListening( _displayEvent, OnGlobalEvent_PopupEvent );
		EventBus.StartListening( EventBus.EEventType.TUT_Popup_HideAll, OnGlobalEvent_PopupHideAll );
	}

	public void CleanUp()
	{
		EventBus.StopListening( _displayEvent, OnGlobalEvent_PopupEvent );
		EventBus.StopListening( EventBus.EEventType.TUT_Popup_HideAll, OnGlobalEvent_PopupHideAll );
	}

	public void OnDestroy()
	{
		CleanUp();
	}

	private void OnGlobalEvent_PopupEvent( EventBus.EventContext context, object obj = null )
	{
		if( obj is bool bPauseTimeWhenActive )
		{
			if( bPauseTimeWhenActive )
			{
				InputModeManager.Instance?.AddUIBlockingObject( gameObject );
			}
		}
		gameObject.SetActive( true );
	}
	private void OnGlobalEvent_PopupHideAll( EventBus.EventContext context, object obj = null )
	{
		gameObject.SetActive( false );
	}

	private void OnDisable()
	{
		InputModeManager.Instance?.RemoveUIBlockingObject( gameObject );
	}
}
