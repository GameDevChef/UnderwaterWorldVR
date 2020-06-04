using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Jobs;
using Unity.Burst;

public class Flock : MonoBehaviour
{
	[SerializeField] Transform playerTransform;

	[Header("Spawn Setup")]
	[SerializeField] private FlockUnit[] avaliableflockUnitPrefabs;
	[SerializeField] private int flockSize;
	[SerializeField] private Vector3 spawnBounds;

	[Header("Speed Setup")]
	[Range(0, 10)]
	[SerializeField] private float _minSpeed;
	public float minSpeed { get { return _minSpeed; } }
	[Range(0, 10)]
	[SerializeField] private float _maxSpeed;
	public float maxSpeed { get { return _maxSpeed; } }


	[Header("Detection Distances")]

	[Range(0, 10)]
	[SerializeField] private float _cohesionDistance;
	public float cohesionDistance { get { return _cohesionDistance; } }

	[Range(0, 10)]
	[SerializeField] private float _avoidanceDistance;
	public float avoidanceDistance { get { return _avoidanceDistance; } }

	[Range(0, 10)]
	[SerializeField] private float _aligementDistance;
	public float aligementDistance { get { return _aligementDistance; } }

	[Range(0, 10)]
	[SerializeField] private float _obstacleDistance;
	public float obstacleDistance { get { return _obstacleDistance; } }

	[Range(0, 100)]
	[SerializeField] private float _boundsDistance;
	public float boundsDistance { get { return _boundsDistance; } }


	[Header("Behaviour Weights")]

	[Range(0, 10)]
	[SerializeField] private float _cohesionWeight;
	public float cohesionWeight { get { return _cohesionWeight; } }

	[Range(0, 10)]
	[SerializeField] private float _avoidanceWeight;
	public float avoidanceWeight { get { return _avoidanceWeight; } }

	[Range(0, 10)]
	[SerializeField] private float _aligementWeight;
	public float aligementWeight { get { return _aligementWeight; } }

	[Range(0, 100)]
	[SerializeField] private float _boundsWeight;
	public float boundsWeight { get { return _boundsWeight; } }

	[Range(0, 100)]
	[SerializeField] private float _obstacleWeight;
	public float obstacleWeight { get { return _obstacleWeight; } }

	public FlockUnit[] allUnits { get; set; }
	private FlockUnit flockUnitPrefab;

	private void Start()
	{
		SetRandomValues();
		GenerateUnits();
	}

	private void SetRandomValues()
	{
		_boundsDistance = UnityEngine.Random.Range(5f, 30f);
		_cohesionWeight = UnityEngine.Random.Range(1f, 10f);
		_aligementWeight = UnityEngine.Random.Range(1f, 10f);
		_avoidanceWeight = UnityEngine.Random.Range(1f, 10f);
		_minSpeed = UnityEngine.Random.Range(1f, 2f);
		_maxSpeed = UnityEngine.Random.Range(2f, 4f);
	}

	private void Update()
	{
		NativeArray<Vector3> unitForwardDirections = new NativeArray<Vector3>(allUnits.Length, Allocator.TempJob);
		NativeArray<Vector3> unitPositions = new NativeArray<Vector3>(allUnits.Length, Allocator.TempJob);
		NativeArray<Vector3> currentVelocities = new NativeArray<Vector3>(allUnits.Length, Allocator.TempJob);
		NativeArray<Vector3> cohesionNeighbours = new NativeArray<Vector3>(allUnits.Length, Allocator.TempJob);
		NativeArray<Vector3> avoidanceNeighbours = new NativeArray<Vector3>(allUnits.Length, Allocator.TempJob);
		NativeArray<Vector3> aligementNeighbours = new NativeArray<Vector3>(allUnits.Length, Allocator.TempJob);
		NativeArray<float> allUnitsSpeeds = new NativeArray<float>(allUnits.Length, Allocator.TempJob);
		NativeArray<float> neighbourSpeeds = new NativeArray<float>(allUnits.Length, Allocator.TempJob);
		for (int i = 0; i < allUnits.Length; i++)
		{
			unitForwardDirections[i] = allUnits[i].myTransform.forward;
			unitPositions[i] = allUnits[i].myTransform.position;
			currentVelocities[i] = allUnits[i].currentVelocity;
			cohesionNeighbours[i] = Vector3.zero;
			avoidanceNeighbours[i] = Vector3.zero;
			aligementNeighbours[i] = Vector3.zero;
			allUnitsSpeeds[i] = allUnits[i].speed;
			neighbourSpeeds[i] = 0f;

		}
		MoveJob moveJob = new MoveJob
		{
			unitForwardDirections = unitForwardDirections,
			unitPositions = unitPositions,
			currentVelocities = currentVelocities,
			allUnitsSpeeds = allUnitsSpeeds,
			flockPosition = transform.position,
			cohesionDistance = cohesionDistance,
			aligementDistance = aligementDistance,
			avoidanceDistance = avoidanceDistance,
			obstacleDistance = obstacleDistance,
			boundsDistance = boundsDistance,
			cohesionWeight = cohesionWeight,
			aligementWeight = aligementWeight,
			avoidanceWeight = avoidanceWeight,
			obstacleWeight = obstacleWeight,
			boundsWeight = boundsWeight,
			angle = flockUnitPrefab.FOVAngle,
			minSpeed = minSpeed,
			maxSpeed = maxSpeed,
			smoothDamp = flockUnitPrefab.smoothDamp,
			deltaTime = Time.deltaTime,
			cohesionNeighbours = cohesionNeighbours,
			avoidanceNeighbours = avoidanceNeighbours,
			aligementNeighbours = aligementNeighbours,
			neighbourSpeeds = neighbourSpeeds,
			playerPosition = playerTransform.position,
			randomDirection = UnityEngine.Random.insideUnitSphere
		};

		JobHandle jobHandle = moveJob.Schedule(allUnits.Length,100);
		jobHandle.Complete();

		for (int i = 0; i < allUnits.Length; i++)
		{
			allUnits[i].myTransform.forward = unitForwardDirections[i];
			allUnits[i].myTransform.position = unitPositions[i];
			allUnits[i].currentVelocity = currentVelocities[i];
			allUnits[i].speed = allUnitsSpeeds[i];
		}
		unitForwardDirections.Dispose();
		unitPositions.Dispose();
		currentVelocities.Dispose();
		allUnitsSpeeds.Dispose();
		cohesionNeighbours.Dispose();
		avoidanceNeighbours.Dispose();
		aligementNeighbours.Dispose();
		neighbourSpeeds.Dispose();

	}

	private void GenerateUnits()
	{
		allUnits = new FlockUnit[flockSize];
		flockUnitPrefab = avaliableflockUnitPrefabs[UnityEngine.Random.Range(0, avaliableflockUnitPrefabs.Length)];
		for (int i = 0; i < flockSize; i++)
		{
			var randomVector = UnityEngine.Random.insideUnitSphere;
			randomVector = new Vector3(randomVector.x * spawnBounds.x, randomVector.y * spawnBounds.y, randomVector.z * spawnBounds.z);
			var spawnPosition = transform.position + randomVector;
			var rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
			allUnits[i] = Instantiate(flockUnitPrefab, spawnPosition, rotation);
			allUnits[i].myTransform.localScale = Vector3.one * UnityEngine.Random.Range(0.7f, 1.3f);
			allUnits[i].AssignFlock(this);
			allUnits[i].InitializeSpeed(UnityEngine.Random.Range(minSpeed, maxSpeed));
		}
	}
}


[BurstCompile]
public struct MoveJob : IJobParallelFor
{
	public NativeArray<Vector3> unitForwardDirections;
	public NativeArray<Vector3> currentVelocities;

	[NativeDisableParallelForRestriction]
	public NativeArray<Vector3> unitPositions;
	[NativeDisableParallelForRestriction]
	public NativeArray<Vector3> cohesionNeighbours;
	[NativeDisableParallelForRestriction]
	public NativeArray<Vector3> avoidanceNeighbours;
	[NativeDisableParallelForRestriction]
	public NativeArray<Vector3> aligementNeighbours;
	[NativeDisableParallelForRestriction]
	public NativeArray<float> allUnitsSpeeds;
	[NativeDisableParallelForRestriction]
	public NativeArray<float> neighbourSpeeds;

	public Vector3 flockPosition;
	public Vector3 playerPosition;
	public Vector3 randomDirection;
	public float cohesionDistance;
	public float aligementDistance;
	public float avoidanceDistance;
	public float boundsDistance;
	public float obstacleDistance;
	public float cohesionWeight;
	public float aligementWeight;
	public float avoidanceWeight;
	public float boundsWeight;
	public float obstacleWeight;
	public float angle;
	public float minSpeed;
	public float maxSpeed;
	public float smoothDamp;
	public float deltaTime;

	public void Execute(int index)
	{

		int cohesionIndex = 0;
		int avoidanceIndex = 0;
		int aligemetIndex = 0;
		for (int i = 0; i < unitPositions.Length; i++)
		{
			var currentUnitPos = unitPositions[i];
			if (unitPositions[index] != currentUnitPos)
			{
				float currentNeighbourDistanceSqr = Vector3.SqrMagnitude(unitPositions[index] - unitPositions[i]);
				if (currentNeighbourDistanceSqr <= cohesionDistance * cohesionDistance)
				{
					cohesionNeighbours[cohesionIndex] = currentUnitPos;
					neighbourSpeeds[cohesionIndex] = allUnitsSpeeds[i];
					cohesionIndex++;
				}
				if (currentNeighbourDistanceSqr <= avoidanceDistance * avoidanceDistance)
				{
					avoidanceNeighbours[avoidanceIndex] = currentUnitPos;
					avoidanceIndex++;
				}
				if (currentNeighbourDistanceSqr <= aligementDistance * aligementDistance)
				{
					aligementNeighbours[aligemetIndex] = currentUnitPos;
					aligemetIndex++;
				}
			}
		}
		float speed = 0;
		if (cohesionNeighbours.Length != 0)
		{

			for (int i = 0; i < cohesionNeighbours.Length; i++)
			{
				if (neighbourSpeeds[i] != 0f)
					speed += neighbourSpeeds[i];
			}

			speed /= cohesionNeighbours.Length;
		}
		speed = Mathf.Clamp(speed, minSpeed, maxSpeed);

		var cohesionVector = Vector3.zero;
		if (cohesionNeighbours.Length != 0)
		{
			int cohesionNeighboursInFOV = 0;
			for (int i = 0; i <= cohesionIndex; i++)
			{
				if (IsInFOV(unitForwardDirections[index], unitPositions[index], cohesionNeighbours[i], angle) && (cohesionNeighbours[i] != Vector3.zero))
				{
					cohesionNeighboursInFOV++;
					cohesionVector += cohesionNeighbours[i];
				}
			}

			cohesionVector /= cohesionNeighboursInFOV;
			cohesionVector -= unitPositions[index];
			cohesionVector = cohesionVector.normalized * cohesionWeight;
		}


		var avoidanceVector = Vector3.zero;
		if (avoidanceNeighbours.Length != 0)
		{
			int avoidanceNeighboursInFOV = 0;
			for (int i = 0; i <= avoidanceIndex; i++)
			{
				if (IsInFOV(unitForwardDirections[index], unitPositions[index], avoidanceNeighbours[i], angle) && (avoidanceNeighbours[i] != Vector3.zero))
				{
					avoidanceNeighboursInFOV++;
					avoidanceVector += (unitPositions[index] - avoidanceNeighbours[i]);
				}
			}

			avoidanceVector /= avoidanceNeighboursInFOV;
			avoidanceVector = avoidanceVector.normalized * avoidanceWeight;
		}

		var aligementVector = Vector3.zero;
		if (aligementNeighbours.Length != 0)
		{

			int aligementNeighboursInFOV = 0;
			for (int i = 0; i <= aligemetIndex; i++)
			{
				if (IsInFOV(unitForwardDirections[index], unitPositions[index], aligementNeighbours[i], angle) && (aligementNeighbours[i] != Vector3.zero))
				{
					aligementNeighboursInFOV++;
					aligementVector += aligementNeighbours[i];
				}
			}

			aligementVector /= aligementNeighboursInFOV;
			aligementVector = aligementVector.normalized * aligementWeight;
		}

		var offsetToCenter = flockPosition - unitPositions[index];
		bool isNearCenter = (offsetToCenter.magnitude >= boundsDistance * 0.9f);
		var boundsVector = isNearCenter ? offsetToCenter.normalized : Vector3.zero;
		boundsVector *= boundsWeight;

		var obstacleVector = Vector3.zero;
		var offsetToPlayer = playerPosition - unitPositions[index];
		if (Vector3.SqrMagnitude(offsetToPlayer) <= obstacleDistance * obstacleDistance)
		{
			offsetToPlayer = new Vector3(
				offsetToPlayer.x + (index - unitPositions.Length / 2) / 30,
				offsetToPlayer.y + (index - unitPositions.Length / 2) / 30,
				offsetToPlayer.z + (index - unitPositions.Length / 2) / 30);
			obstacleVector = -offsetToPlayer;
			speed *= 3f;
			

		}
		obstacleVector = obstacleVector.normalized * obstacleWeight;

		Vector3 currentVelocity = currentVelocities[index];
		var moveVector = cohesionVector + avoidanceVector + aligementVector + boundsVector + obstacleVector;
		//moveVector = Vector3.SmoothDamp(unitForwardDirections[index], moveVector, ref currentVelocity, smoothDamp, 100000, deltaTime);
		moveVector = Vector3.RotateTowards(unitForwardDirections[index], moveVector, 1f * deltaTime, 0f);
		moveVector = moveVector.normalized * speed;
		if (moveVector == Vector3.zero)
		{
			moveVector = unitForwardDirections[index];
		}
		unitPositions[index] = unitPositions[index] + moveVector * deltaTime;
		unitForwardDirections[index] = moveVector.normalized;
		allUnitsSpeeds[index] = speed;
		currentVelocities[index] = currentVelocity;

	}




	private bool IsInFOV(Vector3 forward, Vector3 unitPosition, Vector3 targetPosition, float angle)
	{
		return Vector3.Angle(forward, targetPosition - unitPosition) <= angle;
	}

}
