using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
//using UnityEngine.InputSystem.Controls;
//using UnityEngine.InputSystem.LowLevel;
//using UnityEngine.InputSystem.Utilities;
using UnityEngine.Serialization;

class PointerButtonData
{
	public PointerEventData eventData;
	private bool pressed = false;
	private bool released = false;

	public void InitEventData(EventSystem eventSystem, PointerEventData.InputButton button)
	{
		eventData = new PointerEventData(eventSystem);
		eventData.button = button;
	}

	public bool WasPressed()
	{
		return pressed;
	}

	public bool WasReleased()
	{
		return released;
	}

	public void SetPressed(bool b, int pointerId)
	{
		if (b)
		{
			if (!released)	pressed = true;
		}
		else
		{
			released = true;
		}

		eventData.pointerId = pointerId;
	}

	public void PostFrame()
	{
		pressed = false;
		released = false;
	}

	public void CopyMoveEventData(PointerEventData moveEvent)
	{
		eventData.position = moveEvent.position;
		//eventData.delta = moveEvent.delta;
		eventData.scrollDelta = moveEvent.scrollDelta;
		eventData.pointerCurrentRaycast = moveEvent.pointerCurrentRaycast;
		eventData.pointerEnter = moveEvent.pointerEnter;
		eventData.hovered = moveEvent.hovered;
	}

};

public class WorldSpaceUIInputModule : BaseInputModule
{
	[SerializeField] protected InputActionReference leftClickAction;
	[SerializeField] protected InputActionReference middleClickAction;
	[SerializeField] protected InputActionReference rightClickAction;
	[SerializeField] protected InputActionReference scrollWheelAction;

	[SerializeField] protected bool deselectOnBackgroundClick = false;
	[SerializeField] protected float clickSpeed = 0.3f;

	private (InputActionReference, Action<InputAction.CallbackContext>)[] inputActions;
	private bool hookedActions = false;

	private Dictionary<PointerEventData.InputButton, PointerButtonData> buttons
		= new Dictionary<PointerEventData.InputButton, PointerButtonData>();
	private Vector2 scrollDelta;//TODO: scroll


	public override void ActivateModule()
	{
		base.ActivateModule();
	}

	protected override void OnEnable()
	{
		base.OnEnable();

		buttons.Add(PointerEventData.InputButton.Left, new PointerButtonData());
		buttons.Add(PointerEventData.InputButton.Middle, new PointerButtonData());
		buttons.Add(PointerEventData.InputButton.Right, new PointerButtonData());

		foreach (KeyValuePair<PointerEventData.InputButton, PointerButtonData> button in buttons)
			button.Value.InitEventData(eventSystem, button.Key);

		inputActions = new (InputActionReference, Action<InputAction.CallbackContext>)[]
			{ (leftClickAction,		OnLeftClick)
			, (middleClickAction,	OnMiddleClick)
			, (rightClickAction,		OnRightClick)
			, (scrollWheelAction,	OnScrollWheel)
		};

		HookActions();
	}

	protected override void OnDisable()
	{
		base.OnDisable();

		UnhookActions();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		UnhookActions();
	}

	void HookActions()
	{
		if (hookedActions)
			return;

		for (int i = 0; i < inputActions.Length; i++)
		{
			InputAction action = inputActions[i].Item1?.action;
			if (action != null)
			{
				action.performed += inputActions[i].Item2;
				action.canceled += inputActions[i].Item2;
			}
		}

		hookedActions = true;
	}

	void UnhookActions()
	{
		if (!hookedActions)
			return;

		for (int i = 0; i < inputActions.Length; i++)
		{
			InputAction action = inputActions[i].Item1?.action;
			if (action != null)
			{
				action.performed -= inputActions[i].Item2;
				action.canceled -= inputActions[i].Item2;
			}
		}

		hookedActions = false;
	}

	void OnLeftClick(InputAction.CallbackContext context)
	{
		buttons[PointerEventData.InputButton.Left].SetPressed(context.ReadValueAsButton(), context.control.device.deviceId);
	}

	void OnMiddleClick(InputAction.CallbackContext context)
	{
		buttons[PointerEventData.InputButton.Middle].SetPressed(context.ReadValueAsButton(), context.control.device.deviceId);
	}

	void OnRightClick(InputAction.CallbackContext context)
	{
		buttons[PointerEventData.InputButton.Right].SetPressed(context.ReadValueAsButton(), context.control.device.deviceId);
	}

	void OnScrollWheel(InputAction.CallbackContext context)
	{
		const float kPixelPerLine = 20;

		// The old input system reported scroll deltas in lines, we report pixels.
		// Need to scale as the UI system expects lines.
		scrollDelta = context.ReadValue<Vector2>() * (1 / kPixelPerLine);
		//changedThisFrame = true;
	}

	public override bool IsPointerOverGameObject(int pointerOrTouchId)
	{
		return buttons.ContainsKey(PointerEventData.InputButton.Left)
			&& buttons[PointerEventData.InputButton.Left].eventData.pointerEnter != null;
	}

	private void ProcessPointerMove(PointerEventData eventData)
	{
		eventData.position = new Vector2(Screen.width*0.5f, Screen.height*0.5f);

		// Raycast from current position.
		{
			eventSystem.RaycastAll(eventData, m_RaycastResultCache);
			eventData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
			m_RaycastResultCache.Clear();
		}

		GameObject pointerTarget = eventData.pointerCurrentRaycast.gameObject;
		HandlePointerExitAndEnter(eventData, pointerTarget);
	}

	private void ProcessPointerButton(PointerEventData eventData, bool pressed, bool released)
	{
		GameObject currentOverGo = eventData.pointerCurrentRaycast.gameObject;

		//if (currentOverGo != null && PointerShouldIgnoreTransform(currentOverGo.transform))
		//	return;

		// Button press.
		if (pressed)
		{
			eventData.pressPosition = eventData.position;
			eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;
			eventData.eligibleForClick = true;

			GameObject selectHandler = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
			if (selectHandler != eventSystem.currentSelectedGameObject && (selectHandler != null || deselectOnBackgroundClick))
			{
				eventSystem.SetSelectedGameObject(null, eventData);
			}

			// Invoke OnPointerDown, if present.
			GameObject newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, eventData, ExecuteEvents.pointerDownHandler);

			// Detect clicks.
			// NOTE: StandaloneInputModule does this *after* the click handler has been invoked -- which doesn't seem to
			//       make sense. We do it *before* IPointerClickHandler.
			float time = Time.unscaledTime;
			if (newPressed == eventData.lastPress && (time - eventData.clickTime) < clickSpeed)
			{
				++eventData.clickCount;
			}
			else
			{
				eventData.clickCount = 1;
			}
			eventData.clickTime = time;

			// We didn't find a press handler, so we turn it into a click.
			if (newPressed == null)
			{
				newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
			}

			eventData.pointerPress = newPressed;
			eventData.rawPointerPress = currentOverGo;
		}

		// Button release.
		if (released)
		{
			ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);

			GameObject pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

			if (eventData.pointerPress == pointerUpHandler && eventData.eligibleForClick)
			{
				ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerClickHandler);
			}

			eventData.eligibleForClick = false;
			eventData.pointerPress = null;
			eventData.rawPointerPress = null;
		}
	}

	public override void Process()
	{
		//if (eventSystem.isFocused)

		// Clear the 'used' flag.
		foreach (KeyValuePair<PointerEventData.InputButton, PointerButtonData> button in buttons)
			button.Value.eventData.Reset();

		PointerEventData mainEventData = buttons[PointerEventData.InputButton.Left].eventData;

		ProcessPointerMove(mainEventData);
		foreach (KeyValuePair<PointerEventData.InputButton, PointerButtonData> button in buttons)
		{
			if (button.Key != PointerEventData.InputButton.Left)
				button.Value.CopyMoveEventData(mainEventData);
		}

		foreach (KeyValuePair<PointerEventData.InputButton, PointerButtonData> button in buttons)
			ProcessPointerButton(button.Value.eventData, button.Value.WasPressed(), button.Value.WasReleased());

		foreach (KeyValuePair<PointerEventData.InputButton, PointerButtonData> button in buttons)
			button.Value.PostFrame();
	}
}
