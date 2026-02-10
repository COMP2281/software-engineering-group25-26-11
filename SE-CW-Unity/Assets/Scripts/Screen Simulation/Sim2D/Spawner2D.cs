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
		Vector2 size = region.size * clumpScale; // Apply clump scale to make tighter spawn

		int i = 0;
		Vector2Int numPerAxis = CalculateSpawnCountPerAxisBox2D(region.size, spawnDensity);
		float2[] points = new float2[numPerAxis.x * numPerAxis.y];

		for (int y = 0; y < numPerAxis.y; y++)
		{
			for (int x = 0; x < numPerAxis.x; x++)
			{
				float tx = numPerAxis.x > 1 ? x / (numPerAxis.x - 1f) : 0.5f;
				float ty = numPerAxis.y > 1 ? y / (numPerAxis.y - 1f) : 0.5f;
				float px = (tx - 0.5f) * size.x + centre.x;
				float py = (ty - 0.5f) * size.y + centre.y;
				points[i] = new float2(px, py);
				i++;
			}
		}

		return points;
	}


	static Vector2Int CalculateSpawnCountPerAxisBox2D(Vector2 size, float spawnDensity)
	{
		float area = size.x * size.y;
		int targetTotal = Mathf.CeilToInt(area * spawnDensity);

		float lenSum = size.x + size.y;
		Vector2 t = size / lenSum;
		float m = Mathf.Sqrt(targetTotal / (t.x * t.y));
		int nx = Mathf.CeilToInt(t.x * m);
		int ny = Mathf.CeilToInt(t.y * m);

		return new Vector2Int(nx, ny);
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
			Vector2Int spawnCountPerAxis = CalculateSpawnCountPerAxisBox2D(region.size, spawnDensity);
			spawnParticleCount += spawnCountPerAxis.x * spawnCountPerAxis.y;
		}
	}

	void OnDrawGizmos()
	{
		if (showSpawnBoundsGizmos)
		{
			foreach (SpawnRegion region in spawnRegions)
			{
				Gizmos.color = region.debugCol;
				Gizmos.DrawWireCube((Vector2)transform.position + region.position, region.size);

			}
		}
	}
}