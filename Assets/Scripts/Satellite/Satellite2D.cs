using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Satellite2D : MonoBehaviour
{
	public Image _image;
	public TextMeshProUGUI _nameTMP;
	public TextMeshProUGUI _descriptionTMP;
	public TextMeshProUGUI _typeTMP;
	public Button _button;
	public GameObject _activeHighlightObj;
	
	[Header("Assigned from object builder - debugging only"), Expandable, AllowNesting]
	public SO_Satellite _satelliteData;

	public void Initialise( SO_Satellite inSatellite )
	{
		_satelliteData = inSatellite;
		RefreshVisuals();
	}
	
	public void RefreshVisuals()
	{
		if( _satelliteData == null )
		{
			return;
		}

		if( _image )
		{
			if( _satelliteData._icon )
			{
				_image.sprite = _satelliteData._icon;
				_image.gameObject.SetActive( true );
			}
			else
			{
				_image.gameObject.SetActive( false );
			}
		}

		if( _nameTMP )
		{
			_nameTMP.SetText( _satelliteData.ReadableString() );
		}
		if( _descriptionTMP )
		{
			_descriptionTMP.SetText( _satelliteData._description );
		}
		if( _typeTMP )
		{
			_typeTMP.SetText( _satelliteData.ReadableString() );
		}
		if( _activeHighlightObj )
		{
			_activeHighlightObj.SetActive( false );
		}
		
		
		// TODO other things go here! Colours from sensor type?
	}

	public void SetSelected( bool bSelected )
	{
		if( _activeHighlightObj )
		{
			_activeHighlightObj.SetActive( bSelected );
		}
	}
}
