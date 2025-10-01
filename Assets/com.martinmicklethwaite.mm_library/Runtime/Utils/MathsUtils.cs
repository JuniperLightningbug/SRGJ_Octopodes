using System.Collections.Generic;
using UnityEngine;

namespace MM
{
	public static class MathsUtils
	{
#region Constants
		// Maths constants
		public const float kOneOverRootTwo = 0.7071f;
		
		// Utility constants
		public const float kFloatEpsilon = 0.0001f;
#endregion
		
#region Comparison
		public static float Difference( float a, float b ) =>
			Mathf.Abs( a - b );
		
		public static bool Approximately( float a, float b, float epsilon = kFloatEpsilon ) =>
			Mathf.Abs( a - b ) <= epsilon;
		
		public static bool Approximately( Vector2 a, Vector2 b, float epsilon = kFloatEpsilon ) =>
			Difference(a.x, b.x) +
			Difference(a.y, b.y) < kFloatEpsilon;
		
		public static bool ApproximatelyAll( Vector2 a, Vector2 b, float epsilon = kFloatEpsilon ) =>
			Approximately(a.x, b.x, kFloatEpsilon) &&
			Approximately(a.y, b.y, kFloatEpsilon);
		
		public static bool Approximately( Vector3 a, Vector3 b, float epsilon = kFloatEpsilon ) =>
			Difference(a.x, b.x) +
			Difference(a.y, b.y) +
			Difference(a.z, b.z) < kFloatEpsilon;		
		
		public static bool ApproximatelyAll( Vector3 a, Vector3 b, float epsilon = kFloatEpsilon ) =>
			Approximately(a.x, b.x, kFloatEpsilon) &&
			Approximately(a.y, b.y, kFloatEpsilon) &&
			Approximately(a.z, b.z, kFloatEpsilon);		
		
		public static bool Approximately( Quaternion a, Quaternion b, float epsilon = kFloatEpsilon ) =>
			Difference(a.x, b.x) +
			Difference(a.y, b.y) +
			Difference(a.z, b.z) +
			Difference(a.w, b.w) < kFloatEpsilon;
		
		public static bool ApproximatelyAll( Quaternion a, Quaternion b, float epsilon = kFloatEpsilon ) =>
			Approximately(a.x, b.x, kFloatEpsilon) &&
			Approximately(a.y, b.y, kFloatEpsilon) &&
			Approximately(a.z, b.z, kFloatEpsilon) &&
			Approximately(a.w, b.w, kFloatEpsilon);

		public static bool Approximately( Matrix4x4 a, Matrix4x4 b, float epsilon = kFloatEpsilon ) =>
			Difference( a.m00, b.m00 ) +
			Difference( a.m01, b.m01 ) +
			Difference( a.m02, b.m02 ) +
			Difference( a.m03, b.m03 ) +
			Difference( a.m10, b.m10 ) +
			Difference( a.m11, b.m11 ) +
			Difference( a.m12, b.m12 ) +
			Difference( a.m13, b.m13 ) +
			Difference( a.m20, b.m20 ) +
			Difference( a.m21, b.m21 ) +
			Difference( a.m22, b.m22 ) +
			Difference( a.m23, b.m23 ) +
			Difference( a.m30, b.m30 ) +
			Difference( a.m31, b.m31 ) +
			Difference( a.m32, b.m32 ) +
			Difference( a.m33, b.m33 ) < epsilon;
		
		public static bool ApproximatelyAll( Matrix4x4 a, Matrix4x4 b, float epsilon = kFloatEpsilon ) =>
			Approximately( a.m00, b.m00, kFloatEpsilon ) &&
			Approximately( a.m01, b.m01, kFloatEpsilon ) &&
			Approximately( a.m02, b.m02, kFloatEpsilon ) &&
			Approximately( a.m03, b.m03, kFloatEpsilon ) &&
			Approximately( a.m10, b.m10, kFloatEpsilon ) &&
			Approximately( a.m11, b.m11, kFloatEpsilon ) &&
			Approximately( a.m12, b.m12, kFloatEpsilon ) &&
			Approximately( a.m13, b.m13, kFloatEpsilon ) &&
			Approximately( a.m20, b.m20, kFloatEpsilon ) &&
			Approximately( a.m21, b.m21, kFloatEpsilon ) &&
			Approximately( a.m22, b.m22, kFloatEpsilon ) &&
			Approximately( a.m23, b.m23, kFloatEpsilon ) &&
			Approximately( a.m30, b.m30, kFloatEpsilon ) &&
			Approximately( a.m31, b.m31, kFloatEpsilon ) &&
			Approximately( a.m32, b.m32, kFloatEpsilon ) &&
			Approximately( a.m33, b.m33, kFloatEpsilon );
		
		public static float Max( Vector3 v ) => Mathf.Max(v.x, v.y, v.z);
#endregion
		
	}
}