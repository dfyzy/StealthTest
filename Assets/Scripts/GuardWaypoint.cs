using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuardWaypoint : MonoBehaviour
{
	void Start()
	{

	}

	void Update()
	{

	}

	void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(transform.position, 0.1f);
	}

}
