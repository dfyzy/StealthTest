using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GuardBehaviour : MonoBehaviour
{
	public List<GuardWaypoint> patrolRoute;
	public float patrolStoppingDistance = 1.5f;

	public float offMeshPosMin = 0.1f;

	NavMeshAgent agent;
	int patrolTargetIndex = 0;

	DoorDevice currentDoor;

	void Start()
	{
		agent = gameObject.GetComponent<NavMeshAgent>();

		EnterPatrolMode();
	}

	void Update()
	{
		if (!agent.pathPending && agent.remainingDistance <= (agent.stoppingDistance + 0.01f))
		{
			SetNextPatrolTarget();
		}

		if (agent.isOnOffMeshLink)
		{
			OffMeshLinkData linkData = agent.currentOffMeshLinkData;

			if (!currentDoor)
			{
				currentDoor = linkData.offMeshLink.gameObject.GetComponent<DoorDevice>();
				if (!currentDoor)
				{
					Debug.Log("didn't find a door on an OffMeshLink");
					agent.CompleteOffMeshLink();
					return;
				}

				if (currentDoor.CanOpen())
				{
					currentDoor.Open();
					//play animation or smth
				}
			}

			if (currentDoor && currentDoor.IsOpen())
			{
				Vector3 diff = linkData.endPos - transform.position;
				diff.y = 0.0f;
				float diffSize = diff.magnitude;

				if (diffSize > offMeshPosMin)
				{
					agent.Move((diff/diffSize)*(Time.deltaTime*agent.speed));
				}
				else
				{
					agent.CompleteOffMeshLink();
					currentDoor = null;
				}
			}
		}
	}

	void EnterPatrolMode()
	{
		agent.stoppingDistance = patrolStoppingDistance;
		agent.autoBraking = false;

		SetNextPatrolTarget();
	}

	void SetNextPatrolTarget()
	{
		agent.SetDestination(patrolRoute[patrolTargetIndex].transform.position);
		patrolTargetIndex = (patrolTargetIndex + 1) % patrolRoute.Count;
	}

}
