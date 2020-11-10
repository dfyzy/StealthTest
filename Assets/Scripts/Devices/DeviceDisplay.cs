using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeviceDisplay : MonoBehaviour
{
	public Color onColor = Color.green;
	public Color offColor = Color.red;

	new Renderer renderer;

	void Awake()
	{
		renderer = gameObject.GetComponent<Renderer>();
		OnDeviceTurnOff();
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
