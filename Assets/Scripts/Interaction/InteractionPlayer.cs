using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

enum InteractionVisibilty
{
	None,
	Visible,
	Selectable
}

public class InteractionPlayer : MonoBehaviour
{
	[SerializeField]
	InputActionReference useAction = default;

	[SerializeField]
	float visibleRadius = 2.0f;
	[SerializeField]
	float selectRadius = 1.0f;
	[SerializeField]
	float selectViewCone = 0.75f;

	[SerializeField]
	GameObject viewPanel = null;
	Stack<GameObject> viewPool = new Stack<GameObject>();

	InteractionPoint currentSelection;

	[SerializeField]
	LayerMask interactionLayerMask = default;

	void Start()
	{
		InputAction actionObj = useAction.action;
		if (actionObj != null)
		{
			actionObj.performed += UseCallback;
		}
	}

	public GameObject GetView(GameObject prefab)
	{
		if (viewPool.Count == 0)
		{
			return Instantiate(prefab, viewPanel.transform);
		}

		GameObject view = viewPool.Pop();
		view.SetActive(true);
		return view;
	}

	public void ReleaseView(GameObject view)
	{
		if (view.activeSelf)
		{
			view.SetActive(false);
			viewPool.Push(view);
		}
	}

	void UseCallback(InputAction.CallbackContext context)
	{
		if (currentSelection)
		{
			currentSelection.Use();
		}
	}

	void Update()
	{
		InteractionPoint bestNewSelection = null;
		float bestNewDot = 0.0f;

		GameObject[] pointObjects = GameObject.FindGameObjectsWithTag("InteractionPoint");
		foreach (GameObject pointObject in pointObjects)
		{
			InteractionPoint point = pointObject.GetComponent<InteractionPoint>();

			Vector3 viewportPos;
			InteractionVisibilty visibility = GetPointVisibility(point, out viewportPos);
			if (visibility != InteractionVisibilty.None)
			{
				point.Show(this, new Vector2(viewportPos.x, viewportPos.y));

				if (visibility == InteractionVisibilty.Selectable && (bestNewSelection == null || viewportPos.z > bestNewDot))
				{
					bestNewSelection = point;
					bestNewDot = viewportPos.z;
				}
			}
			else
			{
				point.Hide(this);
			}
		}

		if (currentSelection != bestNewSelection)
		{
			if (currentSelection)
				currentSelection.HideSelection(this);

			currentSelection = bestNewSelection;

			if (currentSelection)
				currentSelection.ShowSelection(this);
		}
	}

	InteractionVisibilty GetPointVisibility(InteractionPoint point, out Vector3 viewportPos)
	{
		viewportPos = new Vector3();
		if (!point.enabled)
			return InteractionVisibilty.None;

		Vector3 originPos = Camera.main.transform.position;
		Vector3 originForward = Camera.main.transform.forward;
		Vector3 pointPos = point.transform.position;
		Vector3 diff = pointPos - originPos;

		float sizeSqr = diff.sqrMagnitude;
		if (sizeSqr > visibleRadius*visibleRadius)
			return InteractionVisibilty.None;

		viewportPos = Camera.main.WorldToViewportPoint(pointPos);
		if (viewportPos.z < 0.0f
			|| viewportPos.x < 0.0f || viewportPos.x > 1.0f
			|| viewportPos.y < 0.0f || viewportPos.y > 1.0f)
		{
			return InteractionVisibilty.None;
		}

		RaycastHit hit;
		if (!Physics.Raycast(originPos, diff, out hit, Mathf.Sqrt(sizeSqr), interactionLayerMask, QueryTriggerInteraction.Ignore))
			return InteractionVisibilty.None;

		if (hit.collider.transform != point.transform)
			return InteractionVisibilty.None;

		if (sizeSqr > selectRadius*selectRadius)
			return InteractionVisibilty.Visible;

		float dot = Vector3.Dot(diff.normalized, originForward);
		viewportPos.z = dot;

		if (dot < selectViewCone)
			return InteractionVisibilty.Visible;

		return InteractionVisibilty.Selectable;
	}

}
