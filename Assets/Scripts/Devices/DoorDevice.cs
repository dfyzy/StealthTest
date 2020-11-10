using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DoorDevice : BaseDevice
{

	public TimeCurve openCurve;
	public Vector3 doorOpenPosition;

	public float openTime = 5.0f;

	bool open = false;
	float openCurrentTime = 0.0f;

	Transform doorTransform;
	Vector3 doorDefaultPosition;
	HashSet<Collider> collidersInTrigger = new HashSet<Collider>();

	public bool IsOpen() { return open; }
	public bool CanOpen() { return !open && openCurve.GetSpeed() <= 0.0f; }

	void Start()
	{
		doorTransform = transform.Find("MovingDoor");
		doorDefaultPosition = doorTransform.localPosition;
	}

	public void Open()
	{
		if (!CanOpen())
		{
			return;
		}

		openCurve.SetSpeed(1.0f);
		gameObject.BroadcastMessage("OnDeviceTurnOn");

		openCurrentTime = 0.0f;
	}

	public void Close()
	{
		open = false;
		openCurve.SetSpeed(-1.0f);
		gameObject.BroadcastMessage("OnDeviceTurnOff");
	}

	void OnTriggerEnter(Collider other)
	{
		collidersInTrigger.Add(other);
	}

	void OnTriggerExit(Collider other)
	{
		collidersInTrigger.Remove(other);
	}

	void Update()
	{
		if (open && openCurve.GetSpeed() == 0.0f)
		{
			openCurrentTime += Time.deltaTime;
			if (openCurrentTime >= openTime && collidersInTrigger.Count == 0)
			{
				Close();
			}
		}

		openCurve.Update((float factor) =>
		{
			doorTransform.localPosition = Vector3.Lerp(doorDefaultPosition, doorOpenPosition, factor);

			if (openCurve.HasStoppedAtEnd())
			{
				open = true;
			}
			else if (openCurve.HasStoppedAtStart())
			{
			}
		});
	}

}
