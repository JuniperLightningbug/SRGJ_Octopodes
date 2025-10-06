using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NaughtyAttributes;
using UnityEditor;

/**
 * Collection of serialised SO_CompiledHexgridMesh assets stored on disc
 */
[CreateAssetMenu(fileName = "CompiledPlanetLayerMeshes", menuName = "Scriptable Objects/Compiled Planet Layer Meshes")]
public class SO_CompiledPlanetLayerMeshes : ScriptableObject
{
	[System.Serializable]
	public class HexgridMeshDataTuple
	{
		// Keys
		[SerializeField] public Mesh _mesh;

		// Values
		[SerializeField, ReadOnly, AllowNesting] public HexgridMeshData _hexgridMeshData;

		public void Reset( Mesh inMesh )
		{
			_mesh = inMesh;
			RefreshMeshData();
		}

		public void RefreshMeshData( bool bForce = false )
		{
			if( _hexgridMeshData == null )
			{
				_hexgridMeshData = new HexgridMeshData();
			}
			_hexgridMeshData.InitialiseFromMesh( _mesh, bForce );
		}
	}
	
	[SerializeField]
	public List<Mesh> _inputMeshAssets = new List<Mesh>();
	
	// Serialised compiled meshes
	[SerializeField, ReadOnly, Label("Compiled Meshes"), AllowNesting]
	public List<HexgridMeshDataTuple> _outputHexgridMeshes = new List<HexgridMeshDataTuple>();
	
	// Deserialised compiled meshes (lazily init this on poll at runtime)
	private Dictionary<string, HexgridMeshData> _hexgridMeshDataBySharedMeshName;

#region Inspector Interface

	[Button( "Add inputs and compile" )]
	[Tooltip( "Adds all input meshes to the compiled set, skipping existing valid entries" )]
	public void Editor_Output_AddAndCompile()
	{
#if UNITY_EDITOR
		AddAndCompileInputs();
		EditorUtility.SetDirty( this );
#endif
	}

	[Button( "Overwrite with inputs and compile" )]
	[Tooltip( "Clears existing compiled set and recalculates the inputs from scratch" )]
	public void Editor_Output_ReplaceAndCompile()
	{
#if UNITY_EDITOR
		ClearOutputs();
		AddAndCompileInputs();
		EditorUtility.SetDirty( this );
#endif
	}

	[Button( "Input: Clear" )]
	public void Editor_Input_Clear()
	{
#if UNITY_EDITOR
		ClearInputs();
		EditorUtility.SetDirty( this );
#endif
	}

	[Button( "Output: Clear" )]
	public void Editor_Output_Clear()
	{
#if UNITY_EDITOR
		ClearOutputs();
		EditorUtility.SetDirty( this );
#endif
	}

	[Button( "Output: Remove Invalid" )]
	public void Editor_Output_RemoveInvalid()
	{
#if UNITY_EDITOR
		RemoveUncompiledOutputs();
		EditorUtility.SetDirty( this );
#endif
	}

	[Button( "Output: Recompile Invalid" )]
	public void Editor_Output_RecompileInvalid()
	{
#if UNITY_EDITOR
		RemoveInvalidOutputs();
		TryRecompileOutputs( false );
		RemoveUncompiledOutputs();
		EditorUtility.SetDirty( this );
#endif
	}
	
	[Button( "Output: Recompile All" )]
	public void Editor_Output_RecompileAll()
	{
#if UNITY_EDITOR
		RemoveInvalidOutputs();
		TryRecompileOutputs( true );
		RemoveUncompiledOutputs();
		EditorUtility.SetDirty( this );
#endif
	}
	
#endregion

#region Edit-Time Operations
	
	// Ignore duplicates, nulls, etc.
	private void SanitiseInputs()
	{
		for( int i = _inputMeshAssets.Count - 1; i >= 0; --i )
		{
			bool bRemove = false;
			if( !_inputMeshAssets[i] )
			{
				bRemove = true;
			}
			else
			{
				for( int j = i - 1; j >= 0; --j )
				{
					if( _inputMeshAssets[j] && _inputMeshAssets[j] == _inputMeshAssets[i] )
					{
						bRemove = true;
					}
				}
			}

			if( bRemove )
			{
				_inputMeshAssets.RemoveAt( i );
			}
		}
	}

	private void AddAndCompileInputs()
	{
		SanitiseInputs();
		AddInputsNoCompile();
		RemoveOutputDuplicates();
		TryRecompileOutputs( false );
	}

	private void AddInputsNoCompile()
	{
		for( int inIdx = 0; inIdx < _inputMeshAssets.Count; ++inIdx )
		{
			// Update existing if relevant
			bool bFound = false;
			for( int outIdx = 0; outIdx < _outputHexgridMeshes.Count; ++outIdx )
			{
				if( _inputMeshAssets[inIdx] == _outputHexgridMeshes[outIdx]._mesh )
				{
					bFound = true;
					break;
				}
			}

			if( !bFound )
			{
				_outputHexgridMeshes.Add( new HexgridMeshDataTuple()
				{
					_mesh = _inputMeshAssets[inIdx],
				} );
			}
		}
	}

	private void RemoveOutputsNotInInputs()
	{
		for( int outIdx = _outputHexgridMeshes.Count - 1; outIdx >= 0; --outIdx )
		{
			bool bFound = false;
			for( int inIdx = 0; inIdx < _inputMeshAssets.Count; ++inIdx )
			{
				if( _inputMeshAssets[inIdx] == _outputHexgridMeshes[outIdx]._mesh )
				{
					bFound = true;
				}
			}

			if( !bFound )
			{
				_outputHexgridMeshes.RemoveAt( outIdx );
			}
		}
	}
	
	private void RemoveOutputDuplicates()
	{
		for( int i = _outputHexgridMeshes.Count - 1; i > 0; --i )
		{
			for( int j = i - 1; j >= 0; --j )
			{
				if( _outputHexgridMeshes[i]._mesh == _outputHexgridMeshes[j]._mesh )
				{
					_outputHexgridMeshes.RemoveAt( i );
					break;
				}
			}
		}
	}
	
	private void RemoveInvalidOutputs()
	{
		for( int i = _outputHexgridMeshes.Count - 1; i >= 0; --i )
		{
			if( _outputHexgridMeshes[i]._mesh == null )
			{
				_outputHexgridMeshes.RemoveAt( i );
			}
		}
	}
	
	private void RemoveUncompiledOutputs()
	{
		for( int i = _outputHexgridMeshes.Count - 1; i >= 0; --i )
		{
			if( _outputHexgridMeshes[i]._mesh == null ||
			    _outputHexgridMeshes[i]._hexgridMeshData == null ||
			    !_outputHexgridMeshes[i]._hexgridMeshData._bInitialised )
			{
				_outputHexgridMeshes.RemoveAt( i );
			}
		}
	}
	
	private void TryRecompileOutputs( bool bForce )
	{
		for( int i = 0; i < _outputHexgridMeshes.Count; ++i )
		{
			_outputHexgridMeshes[i].RefreshMeshData( bForce );

			if( !_outputHexgridMeshes[i]._mesh )
			{
				Debug.LogWarningFormat( "Hexgrid data at [{0}] has not been assigned a mesh", i );
			}
			else if( _outputHexgridMeshes[i]._hexgridMeshData == null || !_outputHexgridMeshes[i]._hexgridMeshData._bInitialised )
			{
				Debug.LogWarningFormat( "Hexgrid data [{0}] has not successfully compiled a hexgrid mesh",
					_outputHexgridMeshes[i]._mesh.name );
			}
		}
	}

	private void ClearInputs()
	{
		_inputMeshAssets.Clear();
	}

	private void ClearOutputs()
	{
		_outputHexgridMeshes.Clear();
	}

#endregion

#region Runtime Interface
	
	public bool TryGetMeshData( Mesh inSharedMesh, out HexgridMeshData outLayerData )
	{
		return TryGetMeshData( inSharedMesh.name, out outLayerData );
	}
	
	public bool TryGetMeshData( string inSharedMeshName, out HexgridMeshData outLayerData )
	{
		LazyInitRuntimeMeshData();
		return _hexgridMeshDataBySharedMeshName.TryGetValue( inSharedMeshName, out outLayerData );
	}

	public void LazyInitRuntimeMeshData()
	{
		if( _hexgridMeshDataBySharedMeshName == null )
		{
			// De-serialize to a dictionary (for the game jam, let's do it as a lazy init at runtime)
			Debug.LogFormat( "Initialising {0} runtime mesh dictionary entries", _outputHexgridMeshes.Count );
			_hexgridMeshDataBySharedMeshName = new Dictionary<string, HexgridMeshData>();
			for( int i = 0; i < _outputHexgridMeshes.Count; ++i )
			{
				if( !_hexgridMeshDataBySharedMeshName.TryAdd(
					   _outputHexgridMeshes[i]._mesh.name,
					   _outputHexgridMeshes[i]._hexgridMeshData ) )
				{
					Debug.LogErrorFormat( "Can't deserialise hexgrid layer mesh data for: {0}",
						_outputHexgridMeshes[i]._mesh.name );
				}
			}
		}
	}
	
#endregion

}
