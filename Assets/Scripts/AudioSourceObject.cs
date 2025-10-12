using System;
using UnityEngine;

public class AudioSourceObject : MonoBehaviour
{
	public static AudioSourceObject _instance;

	public AudioSource _buttonAudioSource;
	public AudioSource _musicAudioSource;

	void Awake()
	{
		if( _instance )
		{
			Destroy( gameObject );
		}
		else
		{
			_instance = this;
		}
		
		DontDestroyOnLoad( gameObject );
	}

	private void OnGlobalEvent_PlayButtonSound( EventBus.EventContext context, object obj = null )
	{
		if( _buttonAudioSource )
		{
			if( _buttonAudioSource.isPlaying )
			{
				_buttonAudioSource.Stop();
			}
			_buttonAudioSource.Play();
		}
	}

	private void OnEnable()
	{
		EventBus.StartListening( EventBus.EEventType.PlayButtonSound, OnGlobalEvent_PlayButtonSound );
	}
	
	private void OnDisable()
	{
		EventBus.StopListening( EventBus.EEventType.PlayButtonSound, OnGlobalEvent_PlayButtonSound );
	}

	void Start()
	{
		if( _instance == this )
		{
			_musicAudioSource?.Play();
		}
	}
	
	

}
