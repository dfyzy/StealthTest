using System;
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBehaviour : MonoBehaviour
{
	public float gravity = 1.0f;

	public Vector2 moveSpeed = new Vector2(10.0f, 10.0f);
	public float accelerationTime = 0.5f;
	public Vector2 rotationSpeed = new Vector2(120.0f, 120.0f);

	public float minYCamera = -90;
	public float maxYCamera = 90;

	[Header("Sprint")]
	public float sprintMoveThreshold = 0.25f;
	public Vector2 sprintMoveSpeed = new Vector2(6.0f, 6.0f);
	public float sprintFOV = 90.0f;
	public float sprintFadeInTime = 1.0f;
	public float sprintFadeOutTime = 0.1f;

	[Header("Crouch")]
	public float crouchHeight = 1.0f;
	public Vector2 crouchMoveSpeed = new Vector2(2.0f, 2.0f);
	public TimeCurve crouchCurve;

	[Header("Slide")]
	public float slideSpeed = 6.0f;
	public float slideHeight = 0.7f;
	public TimeCurve slideCurve;
	public TimeCurve slideHeightCurve;

	[Header("Lean")]
	public float leanAngle = 30.0f;
	public Vector2 leanOffset = new Vector2(1.0f, 0.0f);
	public float leanTime = 0.5f;
	public AnimationCurve leanCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

	[Header("Head Bob")]
	public float headBobFadeInTime = 0.5f;
	public float headBobFadeOutTime = 0.5f;
	public float headBobFactor = 1.0f;
	public float headBobSprintFactor = 2.0f;
	public Vector2 headBobOffset = new Vector2(0.02f, -0.04f);
	public TimeCurve headBobCurve;

	[Header("Zoom")]
	public float zoomFOVFactor = 0.75f;
	public float zoomCurveBackSpeed = -5.0f;
	public TimeCurve zoomCurve;

	[Header("Tablet")]
	public Vector3 tabletAnchor = new Vector3(0.0f, 0.5f, 0.0f);
	public Vector3 defaultTabletOffset = new Vector3(0.0f, -0.3f, 0.5f);
	public float tabletRotationSpeed = 180.0f;
	public float tabletRotationSpeedFactor = 2.0f;
	public Vector2 tabletRotationArea = new Vector2(80.0f, 50.0f);
	public float tabletRotationMinY = 36.0f;
	public TimeCurve tabletHideCurve;
	public Vector3 hiddenTabletOffset = new Vector3(0.0f, -0.3f, 0.25f);
	public Quaternion hiddenTabletRotation = new Quaternion();

	[Header("Cable")]
	public float cableDefaultLength = 0.4f;
	public Quaternion cableTailDefaultRotation = new Quaternion();
	public float cableTailMaxRotOffset = 30.0f;
	public float cableTailAngleRotSpeed = 5.0f;
	public float cableTailForceFactor = 1.0f;

	public TimeCurve cableConnectCurve;

	PlayerInput playerInput;
	CharacterController controller;
	Camera targetCamera;

	GameObject tablet;
	RopeRoot cable;

	InputAction moveAction;
	InputAction lookAction;
	InputAction leanAction;
	InputAction zoomAction;

	Vector2 cameraRotation;
	Vector3 defaultCameraPosition;

	Vector2 currentMoveSpeed;
	float currentAcceleration = 0.0f;

	float currentSprint = 0.0f;

	float defaultFOV;

	float defaultHeight;
	float startHeight;
	float targetHeight;

	Quaternion slideTargetRotation;

	bool wantsToSprint = false;
	bool wantsToToggleCrouch = false;

	bool sprinting = false;
	bool crouching = false;

	float currentLean = 0.0f;
	float headBobWeight = 0.0f;

	int cameraLayerMask = 0;

	Vector3 tabletRotation = Vector3.zero;
	bool tabletHideMode = false;

	bool cableTailInit = false;
	Vector3 cableTailPrevPosition = Vector3.zero;
	Quaternion cableTailAngleRotOffset = Quaternion.identity;

	bool cableConnected = false;

	void Start()
	{
		playerInput = gameObject.GetComponent<PlayerInput>();
		controller = gameObject.GetComponent<CharacterController>();
		targetCamera = Camera.main;

		tablet = GameObject.FindWithTag("Tablet");
		tabletRotation.x = tabletRotationMinY;

		cable = tablet.transform.Find("Cable").GetComponentInChildren<RopeRoot>();

		moveAction = playerInput.actions["Move"];
		lookAction = playerInput.actions["Look"];
		leanAction = playerInput.actions["Lean"];
		zoomAction = playerInput.actions["Zoom"];

		Cursor.lockState = CursorLockMode.Locked;

		defaultCameraPosition = targetCamera.transform.localPosition;
		defaultFOV = targetCamera.fieldOfView;
		currentMoveSpeed = moveSpeed;
		defaultHeight = controller.height;

		int playerLayer = LayerMask.NameToLayer("Player");
		for (int i = 0; i < 32; i++)
		{
			if (!Physics.GetIgnoreLayerCollision(playerLayer, i))
			{
				cameraLayerMask |= 1 << i;
			}
		}

		crouchCurve.SetRelativeTime(1.0f);
	}

	void OnJump()
	{
		//Debug.Log("jump");
	}

	void OnSprint(InputValue value)
	{
		wantsToSprint = value.isPressed;
	}

	void OnCrouch(InputValue value)
	{
		if (value.isPressed)
		{
			//we cache "wanting" to toggle crouch instead of toggling it atm because this input can also cause slide
			wantsToToggleCrouch = !wantsToToggleCrouch;
		}
	}

	void OnTabletHide()
	{
		tabletHideMode = !tabletHideMode;
	}

	void SetHeight(float newHeight)
	{
		float oldHeight = controller.height;

		controller.height = newHeight;
		transform.Translate(0.0f, 0.5f*(newHeight - oldHeight), 0.0f, Space.World);
	}

	void Update()
	{
		Vector3 cameraPosition = defaultCameraPosition;
		Vector3 cameraEuler = Vector3.zero;

		Vector3 characterSpeed = Vector3.zero;

		float leanInput = slideCurve.IsPlaying() ? 0.0f : leanAction.ReadValue<float>();
		Quaternion lastFrameRotation = transform.rotation;

//		camera Input
		{
			Vector2 mouseInput = lookAction.ReadValue<Vector2>() * rotationSpeed;

			cameraRotation += mouseInput*Time.deltaTime;
			cameraRotation.x = StolenMathf.UnwindAngleDegrees(cameraRotation.x);
			cameraRotation.y = Mathf.Clamp(cameraRotation.y, minYCamera, maxYCamera);

			transform.eulerAngles = new Vector3(0.0f, cameraRotation.x, 0.0f);
			cameraEuler.x += cameraRotation.y;
		}

//		sprint
		{
			bool accumulatedWantsToSprint = slideCurve.IsPlaying()
				|| (wantsToSprint && (Quaternion.Inverse(lastFrameRotation)*controller.velocity).z > sprintMoveThreshold);

			if (sprinting != accumulatedWantsToSprint)
			{
				sprinting = !sprinting;

				if (sprinting && crouching)
				{
					wantsToToggleCrouch = true;
				}
			}

			currentSprint += Time.deltaTime/(sprinting ? sprintFadeInTime : -sprintFadeOutTime);
			currentSprint = Mathf.Clamp(currentSprint, 0.0f, 1.0f);

			float vSprintFOV = Camera.HorizontalToVerticalFieldOfView(sprintFOV, targetCamera.aspect);
			targetCamera.fieldOfView = Mathf.Lerp(defaultFOV, vSprintFOV, currentSprint);
		}

//		slide
		{
			if (wantsToToggleCrouch && sprinting && !crouching && !slideCurve.IsPlaying())
			{
				wantsToToggleCrouch = false;
				if (currentSprint >= 1.0f && currentAcceleration >= 1.0f)
				{
					startHeight = controller.height;
					slideTargetRotation = transform.rotation;

					slideCurve.PlayFromStart();
					slideHeightCurve.PlayFromStart();
				}
			}

			slideCurve.Update((float factor) =>
			{
				float moveSpeed = Mathf.Lerp(slideSpeed, crouchMoveSpeed.y, factor);
				characterSpeed += slideTargetRotation*new Vector3(0.0f, 0.0f, moveSpeed);

				if (slideCurve.HasStoppedAtEnd())
				{
					wantsToToggleCrouch = true;
					sprinting = false;
					currentAcceleration = 0.0f;
				}
			});

			slideHeightCurve.Update((float factor) =>
			{
				SetHeight(Mathf.Lerp(startHeight, slideHeight, factor));
			});
		}

//		crouch
		if (!slideCurve.IsPlaying())
		{
			if (wantsToToggleCrouch)
			{
				wantsToToggleCrouch = false;
				crouching = !crouching;

				startHeight = controller.height;
				targetHeight = crouching ? crouchHeight : defaultHeight;
				crouchCurve.PlayFromStart();
			}
			crouchCurve.Update((float factor) =>
			{
				SetHeight(Mathf.Lerp(startHeight, targetHeight, factor));
			});
		}

//		move Input
		{
			Vector2 moveInput = moveAction.ReadValue<Vector2>();
			if (moveInput.sqrMagnitude < 0.1f || leanInput != 0.0f || slideCurve.IsPlaying())
			{
				currentAcceleration = 0.0f;
			}
			else
			{
				currentAcceleration = Mathf.Min(1.0f, currentAcceleration + Time.deltaTime/accelerationTime);

				Vector2 currentMoveSpeed = Vector2.Lerp(
					Vector2.Lerp(moveSpeed, crouchMoveSpeed, crouching ? crouchCurve.GetCurrentValue() : 1.0f - crouchCurve.GetCurrentValue()),
					sprintMoveSpeed,
					currentSprint);

				Vector2 moveInputSpeed = moveInput * currentMoveSpeed * currentAcceleration;
				characterSpeed += transform.rotation*new Vector3(moveInputSpeed.x, 0.0f, moveInputSpeed.y);
			}

			controller.Move((characterSpeed + new Vector3(0.0f, -gravity, 0.0f))*Time.deltaTime);
		}

//		lean
		Vector3 currentLeanOffset;
		{
			float leanDrag = 0.0f;
			if (leanInput == 0.0f && currentLean != 0.0f)
			{
				leanDrag = -Mathf.Sign(currentLean);
			}

			currentLean = Mathf.Clamp(
				currentLean + (leanInput + leanDrag)*Time.deltaTime/leanTime,
				leanDrag < 0.0f ? 0.0f : -1.0f,
				leanDrag > 0.0f ? 0.0f : 1.0f
			);

			float currentLeanFactor = leanCurve.Evaluate(Mathf.Abs(currentLean))*Mathf.Sign(currentLean);

			currentLeanOffset = new Vector3(leanOffset.x, leanOffset.y, 0.0f)*currentLeanFactor;
			cameraPosition += currentLeanOffset;
			cameraEuler.z += -leanAngle*currentLeanFactor;
		}

//		headbob
		if (!slideCurve.IsPlaying() && headBobFactor > 0.0f)
		{
			bool bMoving = controller.velocity.sqrMagnitude > 1.0f;
			headBobWeight += Time.deltaTime/(bMoving ? headBobFadeInTime : -headBobFadeOutTime);
			headBobWeight = Mathf.Clamp(headBobWeight, 0.0f, 1.0f);

			headBobCurve.SetSpeed(headBobWeight*Mathf.Lerp(1.0f, headBobSprintFactor, currentSprint));
			headBobCurve.Update((float factor) =>
			{
				Vector3 offset = new Vector3((headBobCurve.GetLap()/2)%2 == 0 ? headBobOffset.x : -headBobOffset.x, headBobOffset.y, 0.0f);
				cameraPosition += headBobWeight*headBobFactor*factor*offset;
			});
		}

//		camera collision
		{
			Vector3 origin = transform.TransformPoint(defaultCameraPosition);
			Vector3 dest = transform.TransformPoint(cameraPosition);

			Vector3 diff = dest - origin;
			float diffSize = diff.magnitude;
			if (diffSize > 0.05f)
			{
				Vector3 diffNormalized = diff/diffSize;
				float radius = controller.radius;

				RaycastHit hit;
				if (Physics.SphereCast(origin, radius, diffNormalized, out hit, diffSize, cameraLayerMask, QueryTriggerInteraction.Ignore))
				{
					if (hit.distance < diffSize)
					{
						cameraPosition = transform.InverseTransformPoint(origin + diffNormalized*hit.distance);
						currentLeanOffset = currentLeanOffset*(hit.distance/diffSize);
					}
				}
			}
		}

		//zoom
		{
			zoomCurve.SetSpeed(zoomAction.ReadValue<float>() > 0.0f ? 1.0f : zoomCurveBackSpeed);
			zoomCurve.Update((float factor) =>
			{
				targetCamera.fieldOfView = targetCamera.fieldOfView*Mathf.Lerp(1.0f, zoomFOVFactor, factor);
			});
		}

		targetCamera.transform.localPosition = cameraPosition;
		targetCamera.transform.localEulerAngles = cameraEuler;

		//update tablet position
		bool shouldHideTablet = sprinting || slideCurve.IsPlaying();
		{
			tabletHideCurve.SetSpeed(shouldHideTablet || tabletHideMode ? 1.0f : -1.0f);
			tabletHideCurve.Update((float factor) => {});
			//bool tabletIsActive = !tabletHideCurve.HasStoppedAtEnd();
			//tablet.SetActive(tabletIsActive);

			Func<TransformState> defaultState = () =>
			{
				float xDiff = StolenMathf.UnwindAngleDegrees(cameraRotation.x - tabletRotation.y);
				float tabletTargetRotationX = tabletRotation.y + Mathf.Sign(xDiff)*Mathf.Max(0.0f, Mathf.Abs(xDiff) - tabletRotationArea.x);
				float tabletTargetRotationY = Mathf.Min(tabletRotationMinY, cameraRotation.y + tabletRotationArea.y);

				tabletRotation = new Vector3(tabletTargetRotationY, tabletTargetRotationX, 0.0f);
				Quaternion tabletRotationQ = Quaternion.Euler(tabletRotation);

				Vector3 tabletOffset = Vector3.Lerp(defaultTabletOffset, hiddenTabletOffset, tabletHideCurve.GetCurrentValue());

				return new TransformState(
					transform.position + tabletAnchor + tabletRotationQ*tabletOffset,
					tabletRotationQ
				);
			};

			Func<TransformState> hideState = () =>
			{
				Quaternion hiddenTabletGlobalRotation = Quaternion.Euler(new Vector3(0.0f, cameraRotation.x, 0.0f));
				Quaternion targetTabletRotation = hiddenTabletRotation*hiddenTabletGlobalRotation;

				if (tabletHideCurve.HasStoppedAtEnd())
				{
					tabletRotation = targetTabletRotation.eulerAngles;
				}

				Vector3 tabletLocalPosition = hiddenTabletGlobalRotation*hiddenTabletOffset + transform.rotation*currentLeanOffset;

				return new TransformState(
					transform.position + tabletAnchor + tabletLocalPosition,
					targetTabletRotation
				);
			};

			TransformState result = TransformState.InterpolateDefered(defaultState, hideState, tabletHideCurve.GetCurrentValue());
			result.Apply(tablet.transform);
		}

		//cable stuff
		{
			//cableTransitionCurve.SetSpeed(cableMode && !shouldHideTablet ? 1.0f : -1.0f);
			//cableTransitionCurve.Update((float factor) => {});

			Transform tail = cable.GetTail();

			Func<TransformState> defaultState = () =>
			{
				Quaternion localRotation = Quaternion.Euler(new Vector3(0.0f, tablet.transform.eulerAngles.y, 0.0f));
				//Quaternion rotation = localRotation*cableTailDefaultRotation;

				Vector3 radius = new Vector3(0.0f, -cableDefaultLength, 0.0f);
				Vector3 projectedPosition = cable.GetFirstControlPoint(0.5f) + radius;

				if (!cableTailInit)
				{
					cableTailInit = true;
					cableTailPrevPosition = projectedPosition;
				}

				Vector3 force = cableTailPrevPosition - projectedPosition;
				force.y = 0.0f;

				Vector3 rotationAxis = Vector3.Cross(radius, force)/(radius.sqrMagnitude*Time.deltaTime);
				rotationAxis = StolenMathf.MinSize(rotationAxis*cableTailForceFactor, cableTailMaxRotOffset*Mathf.Deg2Rad);

				Quaternion rotOffset = StolenMathf.FromAngleAxis(rotationAxis);
				cableTailAngleRotOffset = Quaternion.Lerp(cableTailAngleRotOffset, rotOffset, Time.deltaTime*cableTailAngleRotSpeed);

				Quaternion rotation = cableTailAngleRotOffset*localRotation*cableTailDefaultRotation;

				cableTailPrevPosition = projectedPosition;

				return new TransformState(
					cable.GetFirstControlPoint(0.5f) + rotation*new Vector3(0.0f, 0.0f, cableDefaultLength),
					rotation*cable.GetChainToWorldRotation()
				);
			};

			Func<TransformState> connectedState = () =>
			{
				DeviceDisplay display = null;//interactionHit.collider.GetComponent<DeviceDisplay>();
				if (display)
				{
					Transform anchor = display.GetCableAnchor();
					/*return new TransformState(
						anchor.position - anchor.forward*cableTailAimLockOffset,
						anchor.rotation*cable.GetChainToWorldRotation()
					);*/
				}

				//TODO: change this?
				return new TransformState(
					cable.transform.position,
					cable.transform.rotation
				);
			};

			TransformState result = TransformState.InterpolateDefered(defaultState, connectedState, cableConnectCurve.GetCurrentValue());
			result.Apply(tail.transform);
		}
		//
	}

}
