using System;
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatTable
{
	private float[][] data;

	public FloatTable(params float[] inData)
	{
		data = new float[inData.Length/2][];
		for (int i = 0; i < data.Length; i++)
		{
			data[i] = new float[2] {inData[i*2], inData[i*2 + 1]};
		}

		Array.Sort(data, (float[] lhs, float[] rhs) => lhs[0].CompareTo(rhs[0]));
	}

	public float GetValue(float key)
	{
		if (data.Length == 0)	return 0.0f;
		if (data.Length == 1)	return data[0][1];

		int start = 0;
		int end = data.Length;

		if (key < data[0][0])			return data[0][1];
		if (key > data[end - 1][0])	return data[end - 1][1];

		while (start < end)
		{
			int m = (start + end)/2;
			if (data[m][0] < key)
				start = m + 1;
			else
				end = m;
		}

		if (start >= data.Length - 1)
		{
			Debug.LogError("FloatTable: binary search failed");
			return data[data.Length - 1][0];
		}

		float[] pair0 = data[start];
		float[] pair1 = data[start + 1];

		return Mathf.Lerp(pair0[1], pair1[1], (key - pair0[0])/(pair1[0] - pair0[0]));
	}

}
