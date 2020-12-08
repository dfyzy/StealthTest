using System;
using UnityEngine;

public class InteractionPlayer : MonoBehaviour
{
	int interactionLayerMask = 1 << 25;

	void Update()
	{
		GameObject[] pointObjects = GameObject.FindGameObjectsWithTag("InteractionPoint");
		foreach (GameObject pointObject in pointObjects)
		{
			InteractionPoint point = pointObject.GetComponent<InteractionPoint>();

		}
	}
}
