using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeRoot : MonoBehaviour
{
	//public float lengthFactor = 1.2f;
	public int rootTailIndexOffset = 0;
	public float paramPointDistance = 0.1f;

	public Vector3 localUp = Vector3.forward;
	public Vector3 localForward;

	List<Transform> chain = new List<Transform>();
	int lastChainIndex = 0;

	Quaternion localToWorldRotation;

	void Awake()
	{
		Transform target = transform;
		while (target)
		{
			chain.Add(target);
			target = target.childCount > 0 ? target.GetChild(0) : null;
		}
		if (chain.Count < 2 + rootTailIndexOffset)
		{
			return;
		}

		lastChainIndex = chain.Count - 1 - rootTailIndexOffset;

		localForward = (Quaternion.Inverse(chain[0].rotation)*(chain[1].position - chain[0].position)).normalized;
		localToWorldRotation = Quaternion.Inverse(Quaternion.LookRotation(localForward, localUp));

		/*
		length = 0.0f;
		Vector3 lastPos = transform.position;
		for (int i = 1; i <= lastChainIndex; i++)
		{
			length += (chain[i].position - lastPos).magnitude;
			lastPos = chain[i].position;
		}
		length *= lengthFactor;*/
	}

	public Quaternion GetChainToWorldRotation()
	{
		return localToWorldRotation;
	}

	public Transform GetTail()
	{
		return chain[lastChainIndex];
	}

	public Vector3 GetFirstControlPoint(float factor = 1.0f)
	{
		return chain[0].position + chain[0].rotation*(localForward*paramPointDistance*factor);
	}

	public Vector3 GetLastControlPoint(float factor = 1.0f)
	{
		Transform tail = GetTail();
		return tail.position - tail.rotation*(localForward*paramPointDistance*factor);
	}

	void ApplyRopePosition(Func<float, Vector3> func)
	{
		for (int i = chain.Count - 1; i >= 1; i--)
			chain[i].SetParent(null);

		float max = lastChainIndex;
		for (int i = 1; i < lastChainIndex; i++)
		{
			chain[i].position = func(i/max);
		}

		Vector3 headUp = chain[0].rotation*localUp;
		Vector3 tailUp = chain[lastChainIndex].rotation*localUp;

		if (Vector3.Dot(headUp, tailUp) < 0.0f)
		{
			chain[lastChainIndex].rotation = Quaternion.LookRotation(chain[lastChainIndex].rotation*localForward, -tailUp)*localToWorldRotation;
			tailUp = -tailUp;
		}

		for (int i = 1; i < lastChainIndex; i++)
		{
			Vector3 next = chain[i + 1].position - chain[i].position;
			Vector3 up = Vector3.Lerp(headUp, tailUp, i/max).normalized;
			chain[i].rotation = Quaternion.LookRotation(next, up)*localToWorldRotation;
		}

		for (int i = 1; i < chain.Count; i++)
			chain[i].localScale = chain[0].lossyScale;

		for (int i = 1; i < chain.Count; i++)
			chain[i].SetParent(chain[i - 1]);
	}

	void LateUpdate()
	{
		if (chain.Count < 3)
		{
			return;
		}

		Transform tail = GetTail();
		/*Vector3 diff = tail.position - transform.position;
		float diffSqrMagnitude = diff.sqrMagnitude;

		if (diffSqrMagnitude >= (length - 0.001f)*(length - 0.001f))
		{
			ApplyRopePosition(f => transform.position + diff*f);
		}
		else*/
		{
			Vector3 point1 = GetFirstControlPoint();
			Vector3 point2 = GetLastControlPoint();

			ApplyRopePosition(f => StolenMathf.Bezier.Calc3(f, transform.position, point1, point2, tail.position));
		}
	}

}
