using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using Shapes;
using UnityEditor;

public class FieldLinesVisualisation : MonoBehaviour
{

	[SerializeField] private int _numLines;
	[SerializeField] private int _pointsPerLine = 100;
	[SerializeField] private float[] _startRadii = new float[] { 1.0f };
	[SerializeField] private float _lineWidth = 0.05f;
	[SerializeField] private float _lineFadeDistanceBehindMagnet = 1.0f;

	[SerializeField] private Gradient _colourByTheta;
	[SerializeField] private Vector2 _alphaByR;

	[SerializeField] private List<Polyline> _lines = new List<Polyline>();


	[Button]
	public void Reset()
	{
		ClearLines();
		GenerateLines();
	}

	[Button]
	public void Clear()
	{
		ClearLines();
	}

	private void ClearLines()
	{
		for( int i = _lines.Count - 1; i >= 0; --i )
		{
			if( Application.isPlaying )
			{
				Destroy( _lines[i]?.gameObject );
			}
			else
			{
				DestroyImmediate( _lines[i]?.gameObject );
			}
		}

		_lines.Clear();
	}

	void Update()
	{
		Transform cameraTransform = Camera.main.transform;
		float distanceToMagnetPlane = Vector3.Dot( transform.position - cameraTransform.position, cameraTransform.forward );
		
		// Exaggerate thickness fade-out behind the magnet for better perspective
		for( int lineIdx = 0; lineIdx < _lines.Count; ++lineIdx )
		{
			List<PolylinePoint> points = _lines[lineIdx].points;
			Transform lineTransform = _lines[lineIdx].transform;
			
			for( int i = 0; i < points.Count; ++i )
			{
				float distanceToPoint = Vector3.Dot(lineTransform.TransformPoint( points[i].point) - cameraTransform.position, cameraTransform.forward);
				float k = Mathf.InverseLerp( distanceToMagnetPlane,
					distanceToMagnetPlane + _lineFadeDistanceBehindMagnet, distanceToPoint );
				
				// Apply fade out
				float widthMultiplier = Mathf.SmoothStep( 1.0f, 0.0f, k );
				_lines[lineIdx].SetPointThickness( i, widthMultiplier );
			}
		}
		
		// // Optional (TODO: Testing this)
		// // Rotate towards player camera
		// if( !Mathf.Approximately( Mathf.Abs( cameraTransform.forward.y ), 1.0f ) )
		// {
		// 	Vector3 currentRotation = transform.rotation.eulerAngles;
		// 	currentRotation.y = cameraTransform.rotation.eulerAngles.y + 180.0f;
		// 	transform.rotation = Quaternion.Euler( currentRotation );
		// }
	}

	private void GenerateLines()
	{
		if( _pointsPerLine < 2 )
		{
			return;
		}

		(Vector3[] localPoints, Color[] pointColours)[] pointTemplates = new (Vector3[], Color[])[_startRadii.Length];
		
		const float kPi = Mathf.PI;
		const float kTau = kPi / 2.0f;
		for( int layerIdx = 0; layerIdx < _startRadii.Length; ++layerIdx )
		{
			pointTemplates[layerIdx].localPoints = new Vector3[_pointsPerLine];
			pointTemplates[layerIdx].pointColours = new Color[_pointsPerLine];
			
			for( int pointIdx = 0; pointIdx < _pointsPerLine; ++pointIdx )
			{
				float lineProgress = pointIdx / (float)_pointsPerLine;
				float theta = kPi * lineProgress;

				float sinTheta = Mathf.Sin( theta );
				pointTemplates[layerIdx].localPoints[pointIdx].x = _startRadii[layerIdx] * sinTheta * sinTheta * Mathf.Cos( kTau - theta );
				pointTemplates[layerIdx].localPoints[pointIdx].y = _startRadii[layerIdx] * sinTheta * sinTheta * Mathf.Sin( kTau - theta );

				pointTemplates[layerIdx].pointColours[pointIdx] = _colourByTheta.Evaluate( lineProgress );
				float alphaT = Mathf.InverseLerp( _alphaByR.x, _alphaByR.y, pointTemplates[layerIdx].localPoints[pointIdx].magnitude );
				pointTemplates[layerIdx].pointColours[pointIdx].a *= Mathf.SmoothStep( 1.0f, 0.0f, alphaT );
			}
		}

		// Symmetry: rotation around y-axis for other lines
		for( int lineIdx = 0; lineIdx < _numLines; ++lineIdx )
		{
			for( int layerIdx = 0; layerIdx < _startRadii.Length; ++layerIdx )
			{
				GameObject newLineObject = new GameObject()
				{
					name = $"Line{lineIdx}",
					transform =
					{
						parent = transform,
						localPosition = Vector3.zero,
						localScale = Vector3.one,
						localRotation = Quaternion.Euler( 0.0f, 360.0f * lineIdx / (float)_numLines, 0.0f ),
					}
				};

				Polyline polyline = newLineObject.AddComponent<Polyline>();
				polyline.Closed = true;
				polyline.Thickness = _lineWidth;
				polyline.Color = Color.cyan;
				polyline.Geometry = PolylineGeometry.Billboard;
				polyline.SetPoints( pointTemplates[layerIdx].localPoints );

				for( int pointIdx = 0; pointIdx < _pointsPerLine; ++pointIdx )
				{
					polyline.SetPointColor( pointIdx, pointTemplates[layerIdx].pointColours[pointIdx] );
				}

				_lines.Add( polyline );
			}
		}
	}
}
