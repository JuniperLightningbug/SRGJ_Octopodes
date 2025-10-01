using UnityEngine;

namespace MM
{
	public interface IControlled
	{
	}

	public interface IControlledUpdate : IControlled
	{
		public void ControlledUpdate( float deltaTime );
	}

	public interface IControlledLateUpdate : IControlled
	{
		public void ControlledLateUpdate( float deltaTime );
	}

	public interface IControlledFixedUpdate : IControlled
	{
		public void ControlledFixedUpdate( float fixedDeltaTime );
	}

	public interface IControlledDebugUpdate : IControlled
	{
		public void ControlledDebugUpdate( float deltaTime );
	}

}