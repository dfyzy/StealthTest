using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeviceDisplay : MonoBehaviour
{
	public Color onColor = Color.green;
	public Color offColor = Color.red;

	new Renderer renderer;
	Transform cableAnchor;

	void Awake()
	{
		renderer = gameObject.GetComponent<Renderer>();
		cableAnchor = transform.Find("CableAnchor");

		OnDeviceTurnOff();
	}

	public Transform GetCableAnchor()
	{
		return cableAnchor;
	}

	void OnDeviceTurnOn()
	{
		renderer.material.color = onColor;
	}

	void OnDeviceTurnOff()
	{
		renderer.material.color = offColor;
	}

}
