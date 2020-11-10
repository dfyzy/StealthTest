using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AppButton : MonoBehaviour
{
	public GameObject appPrefab;
	Button button;

	public void Start()
	{
		button = gameObject.GetComponent<Button>();
		button.onClick.AddListener(OnClick);
	}

	void OnClick()
	{
		
	}

}
