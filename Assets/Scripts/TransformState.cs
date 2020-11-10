using System;
using UnityEngine;

struct TransformState
{
	public Vector3 position;
	public Quaternion rotation;

	public TransformState(Vector3 position, Quaternion rotation)
	{
		this.position = position;
		this.rotation = rotation;
	}

	public void Apply(Transform transform)
	{
		transform.position = position;
		transform.rotation = rotation;
	}

	public static TransformState Interpolate(TransformState state0, TransformState state1, float t)
	{
		return new TransformState(
			Vector3.Lerp(state0.position, state1.position, t),
			Quaternion.Lerp(state0.rotation, state1.rotation, t)
		);
	}

	public static TransformState InterpolateDefered(Func<TransformState> state0, Func<TransformState> state1, float t)
	{
		if (t <= 0.0001f)
			return state0();
		else if (t >= 0.9999f)
			return state1();

		return Interpolate(state0(), state1(), t);
	}

}
