using UnityEngine;

namespace MM
{
	public static class ComponentUtils
	{
		public static Material GetMaterialSafe( MeshRenderer inRenderer )
		{
			if( inRenderer )
			{
				return Application.isPlaying ? inRenderer.material : inRenderer.sharedMaterial;
			}

			return null;
		}

		public static void DestroyPlaymodeSafe( GameObject gameObject )
		{
			if( gameObject )
			{
				if( Application.isPlaying )
				{
					Object.Destroy( gameObject );
				}
#if UNITY_EDITOR
				else
				{
					Object.DestroyImmediate( gameObject );
				}
#endif
			}
		}
	}
}