using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Spawner2D : MonoBehaviour
{
	public float spawnDensity;

	public Vector2 initialVelocity;
	public float jitterStr;
	
	[Header("Spawn Clump Settings")]
	[Tooltip("Multiplier for spawn region size (smaller = tighter clump)")]
	[Range(0.1f, 2f)]
	public float clumpScale = 1f;
	
	[Tooltip("Velocity damping on spawn (0 = no velocity, 1 = full velocity)")]
	[Range(0f, 1f)]
	public float spawnVelocityScale = 0.2f;
	
	public SpawnRegion[] spawnRegions;
	public bool showSpawnBoundsGizmos;

	[Header("Debug Info")]
	public int spawnParticleCount;

	public ParticleSpawnData GetSpawnData()
	{
		return GetSpawnData(new float4(1, 1, 1, 1)); // Default white color
	}

	public ParticleSpawnData GetSpawnData(float4 color)
	{
		var rng = new Unity.Mathematics.Random(42);

		List<float2> allPoints = new();
		List<float2> allVelocities = new();
		List<int> allIndices = new();
		List<float4> allColors = new();

		for (int regionIndex = 0; regionIndex < spawnRegions.Length; regionIndex++)
		{
			SpawnRegion region = spawnRegions[regionIndex];
			float2[] points = SpawnInRegion(region);

			for (int i = 0; i < points.Length; i++)
			{
				float angle = (float)rng.NextDouble() * 3.14f * 2;
				float2 dir = new float2(Mathf.Cos(angle), Mathf.Sin(angle));
				float2 jitter = dir * jitterStr * ((float)rng.NextDouble() - 0.5f) * clumpScale;
				allPoints.Add(points[i] + jitter);
				// Apply velocity scale to reduce initial momentum
				allVelocities.Add(initialVelocity * spawnVelocityScale);
				allIndices.Add(regionIndex);
				allColors.Add(color);
			}
		}

		ParticleSpawnData data = new()
		{
			positions = allPoints.ToArray(),
			velocities = allVelocities.ToArray(),
			spawnIndices = allIndices.ToArray(),
			colors = allColors.ToArray(),
		};

		return data;
	}

	float2[] SpawnInRegion(SpawnRegion region)
	{
		// Centre is region offset (local space)
		Vector2 centre = region.position;
		float circleRadius = GetCircleRadius(region.size) * clumpScale;

		int pointCount = CalculateSpawnCount(region.size, spawnDensity);
		List<float2> points = new(pointCount);

		float goldenAngle = 2.39996323f;

		for (int i = 0; i < pointCount; i++)
		{
			float t = (i + 0.5f) / pointCount;
			float radialDistance = Mathf.Sqrt(t) * circleRadius;
			float angle = i * goldenAngle;
			float px = centre.x + Mathf.Cos(angle) * radialDistance;
			float py = centre.y + Mathf.Sin(angle) * radialDistance;
			points.Add(new float2(px, py));
		}

		return points.ToArray();
	}

	static float GetCircleRadius(Vector2 size)
	{
		return Mathf.Max(0f, Mathf.Min(size.x, size.y) * 0.5f);
	}


	static int CalculateSpawnCount(Vector2 size, float spawnDensity)
	{
		float radius = GetCircleRadius(size);
		float area = Mathf.PI * radius * radius;
		return Mathf.Max(1, Mathf.CeilToInt(area * spawnDensity));
	}

	public struct ParticleSpawnData
	{
		public float2[] positions;
		public float2[] velocities;
		public int[] spawnIndices;
		public float4[] colors;

		public ParticleSpawnData(int num)
		{
			positions = new float2[num];
			velocities = new float2[num];
			spawnIndices = new int[num];
			colors = new float4[num];
		}
	}

	[System.Serializable]
	public struct SpawnRegion
	{
		public Vector2 position;
		public Vector2 size;
		public Color debugCol;
	}

	void OnValidate()
	{
		spawnParticleCount = 0;
		foreach (SpawnRegion region in spawnRegions)
		{
			spawnParticleCount += CalculateSpawnCount(region.size, spawnDensity);
		}
	}

	void OnDrawGizmos()
	{
		if (showSpawnBoundsGizmos)
		{
			foreach (SpawnRegion region in spawnRegions)
			{
				Gizmos.color = region.debugCol;
				float radius = GetCircleRadius(region.size) * clumpScale;
				Gizmos.DrawWireSphere((Vector2)transform.position + region.position, radius);

			}
		}
	}
}