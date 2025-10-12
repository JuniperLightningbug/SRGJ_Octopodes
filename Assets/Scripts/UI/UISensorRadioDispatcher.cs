using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UISensorRadioDispatcher : MonoBehaviour
{
	[SerializeField] private List<UIValues_LayerProgress> _toggles = new List<UIValues_LayerProgress>();

	private void Awake()
	{
		// They start disabled - initialise listeners here
		if( _toggles != null )
		{
			for( int i = 0; i < _toggles.Count; ++i )
			{
				_toggles[i].Initialise();
			}
		}
	}
}