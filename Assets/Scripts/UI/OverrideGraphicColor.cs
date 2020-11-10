using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class OverrideGraphicColor : MonoBehaviour
{

	OverrideGraphicColorParent parent;
	Graphic graphic;

	void Awake()
	{
		parent = gameObject.GetComponentInParent<OverrideGraphicColorParent>();
		graphic = gameObject.GetComponent<Graphic>();
	}

	void Update()
	{
		if (parent && graphic)
		{
			graphic.color = parent.color;
		}
	}
}
