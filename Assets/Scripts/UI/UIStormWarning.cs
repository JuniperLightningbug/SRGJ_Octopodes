using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class UIStormWarning : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI _messageTMP;
	[SerializeField] private TextMeshProUGUI _timerTMP;
	[SerializeField] private Color _warningColor;
	[SerializeField] private string _warningText;

	[SerializeField] private Color _stormColor;
	[SerializeField] private string _stormText;

	[SerializeField] private Color _clearedColor;
	[SerializeField] private string _clearedText;

	[SerializeField] private Image _icon;
	[SerializeField] private float _clearMessageTime = 3.0f;

	private float _remainingTime = 0.0f;
	private void OnGlobalEvent_StormWarningStarted(EventBus.EventContext context, object obj = null)
	{
		if (obj is float warningTime)
		{
			_remainingTime = warningTime;
			if(_messageTMP)
            {
				_messageTMP.text = _warningText;
				_messageTMP.color = _warningColor;
            }
			if (_icon)
			{
				_icon.gameObject.SetActive(true);
			}
			if (_timerTMP)
			{
				_timerTMP.gameObject.SetActive(true);
			}
			gameObject.SetActive(true);
		}
	}

	private void OnGlobalEvent_StormStarted(EventBus.EventContext context, object obj = null)
	{
		if (obj is float stormTime)
		{
			_remainingTime = stormTime;
			if (_messageTMP)
			{
				_messageTMP.text = _stormText;
				_messageTMP.color = _stormColor;
			}
			if (_icon)
			{
				_icon.gameObject.SetActive(true);
			}
			if (_timerTMP)
			{
				_timerTMP.gameObject.SetActive(true);
			}
			gameObject.SetActive(true);
		}
	}

	private void OnGlobalEvent_StormEnded(EventBus.EventContext context, object obj = null)
	{
		_remainingTime = _clearMessageTime;
		if (_messageTMP)
		{
			_messageTMP.text = _clearedText;
			_messageTMP.color = _clearedColor;
		}
		if(_icon)
        {
			_icon.gameObject.SetActive(false);
        }
		if(_timerTMP)
        {
			_timerTMP.gameObject.SetActive(false);
        }
		gameObject.SetActive(true);
	}

	private void Update()
	{
		if( _remainingTime < 0 )
		{
			gameObject.SetActive( false );
		}
		else if( gameObject.activeSelf == true && _timerTMP )
		{
			_remainingTime -= Time.deltaTime;
			string _timerText = string.Format( "{0}s", Mathf.CeilToInt( _remainingTime ).ToString() );
			_timerTMP.SetText( _timerText );
		}
	}

	private void OnDestroy()
	{
		EventBus.StopListening(EventBus.EEventType.StormWarningStarted, OnGlobalEvent_StormWarningStarted);
		EventBus.StopListening(EventBus.EEventType.StormStarted, OnGlobalEvent_StormStarted);
		EventBus.StopListening(EventBus.EEventType.StormEnded, OnGlobalEvent_StormEnded);
	}

	public void Initialise()
	{
		EventBus.StartListening(EventBus.EEventType.StormWarningStarted, OnGlobalEvent_StormWarningStarted);
		EventBus.StartListening(EventBus.EEventType.StormStarted, OnGlobalEvent_StormStarted);
		EventBus.StartListening(EventBus.EEventType.StormEnded, OnGlobalEvent_StormEnded);
	}
}
