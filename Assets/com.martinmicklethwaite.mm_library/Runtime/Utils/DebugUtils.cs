using UnityEngine;

namespace MM
{
	public static class DebugUtils
	{
		public const string kStringInvalid = "_NONE_";
		public static string GetNameSafe( GameObject obj ) => obj ? obj.name : kStringInvalid;
	}
}