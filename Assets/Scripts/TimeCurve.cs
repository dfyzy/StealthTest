using System;
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TimeCurve
{
	[SerializeField]
	AnimationCurve curve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
	[SerializeField]
	bool stopOnClamp = true;

	float speed = 0.0f;
	float lapTime = 0.0f;
	int lap = 0;

	float lastValue = 0.0f;
	float value = 0.0f;

	bool IsValid() { return curve.length != 0; }

	public float GetSpeed() { return speed; }
	public void SetSpeed(float f)
	{
		if (!IsValid())
		{
			return;
		}

		speed = f;
	}

	public float GetLapLength() { return IsValid() ? curve[curve.length - 1].time - curve[0].time : 0.0f; }

	public float GetLapTime() { return lapTime; }
	public int GetLap() { return lap; }
	public float GetGlobalTime() { return IsValid() ? curve[0].time + lapTime + GetLapLength() * lap : 0.0f; }

	public bool HasStoppedAtStart() { return speed == 0.0f && lapTime == 0.0f; }
	public bool HasStoppedAtEnd() { return speed == 0.0f && lapTime == GetLapLength(); }

	public bool IsPlaying() { return speed != 0.0f; }

	public void SetGlobalTime(float f)
	{
		if (!IsValid())
		{
			return;
		}

		lapTime = f - curve[0].time;
		lap = 0;
		OnCurveUpdated();
	}

	public void SetRelativeTime(float f)
	{
		SetGlobalTime(f*GetLapLength());
	}

	public void PlayFromGlobal(float f)
	{
		SetGlobalTime(f);
		SetSpeed(1.0f);
	}

	public void PlayFromStart()
	{
		PlayFromGlobal(0.0f);
	}

	public float GetCurrentValue() { return value; }
	public float GetCurrentDelta() { return value - lastValue; }

	public bool IsCurrentLapReversed() { return IsLapReversed(lap); }
	public bool IsLapReversed(int inLap)
	{
		WrapMode wrap = (inLap > 0 ? curve.postWrapMode : curve.preWrapMode);
		return wrap == WrapMode.PingPong && inLap % 2 != 0;
	}

	void OnCurveUpdated()
	{
		float lapLength = GetLapLength();
		if (lapTime < 0.0f || lapTime > lapLength)
		{
			int lapIncr = Mathf.FloorToInt(lapTime/lapLength);
			lapTime -= lapIncr*lapLength;
			lap += lapIncr;

			if (stopOnClamp && (lapIncr > 0 ? curve.postWrapMode : curve.preWrapMode) == WrapMode.ClampForever)
			{
				lapTime = lapIncr > 0 ? lapLength : 0.0f;
				lap = 0;
				speed = 0.0f;
			}
		}

		lastValue = value;
		value = curve.Evaluate(GetGlobalTime());
	}

	public void Update(Action<float> callback)
	{
		if (!IsValid() || speed == 0.0f)
			return;

		lapTime += speed*Time.deltaTime;
		OnCurveUpdated();

		callback(value);
	}

}
