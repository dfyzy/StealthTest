using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InteractionPoint : MonoBehaviour
{
	[SerializeField]
	GameObject viewPrefab = null;

	[SerializeField]
	String text = default;

	public event UnityAction onUse;

	InteractionPlayer currentPlayer;

	GameObject view;
	GameObject selectionPanel;

	void Start()
	{
	}

	void OnDestroy()
	{
		if (currentPlayer)
		{
			Hide(currentPlayer);
		}
	}

	public void Show(InteractionPlayer player, Vector2 viewportPos)
	{
		if (currentPlayer && currentPlayer != player)
		{
			//
		}

		currentPlayer = player;

		if (!view)
		{
			view = player.GetView(viewPrefab);
			Transform selectionTransform = view.transform.Find("Selection");
			selectionPanel = selectionTransform.gameObject;

			Text selectionText = selectionTransform.Find("Text").GetComponent<Text>();
			selectionText.text = text;
		}

		RectTransform rect = view.GetComponent<RectTransform>();
		rect.anchorMin = viewportPos;
		rect.anchorMax = viewportPos;
	}

	public void Hide(InteractionPlayer player)
	{
		if (currentPlayer != player)
			return;

		currentPlayer = null;

		if (view)
		{
			HideSelection(player);
			player.ReleaseView(view);
			view = null;
			selectionPanel = null;
		}
	}

	public void ShowSelection(InteractionPlayer player)
	{
		if (!selectionPanel)
			return;

		selectionPanel.SetActive(true);
		LayoutRebuilder.ForceRebuildLayoutImmediate(selectionPanel.GetComponent<RectTransform>());
	}

	public void HideSelection(InteractionPlayer player)
	{
		if (!selectionPanel)
			return;

		selectionPanel.SetActive(false);
	}

	public void Use()
	{
		onUse();
	}

}
