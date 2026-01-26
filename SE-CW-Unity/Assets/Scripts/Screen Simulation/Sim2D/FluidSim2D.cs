using Seb.Fluid2D.Rendering;
using Seb.Helpers;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Serialization;

namespace Seb.Fluid2D.Simulation
{
	public class FluidSim2D : MonoBehaviour
	{
		public event System.Action SimulationStepCompleted;
		[Header("World Mapping")]
		public Vector2 worldOffset;   // X/Y origin of the 2D sim in world space
		public float worldScale = 1f; // Optional: scale sim units to world units
		[Tooltip("Spawn initial particles at startup")]
		public bool spawnOnStart = true;

		[Header("Simulation Settings")]
		public float timeScale = 1;
		public float maxTimestepFPS = 60; // if time-step dips lower than this fps, simulation will run slower (set to 0 to disable)
		public int iterationsPerFrame;
		public float gravity;
		public Vector2 gravityDir = new Vector2(0, -1);
		[Range(0, 1)] public float collisionDamping = 0.95f;
		public float smoothingRadius = 2;
		public float targetDensity;
		public float pressureMultiplier;
		public float nearPressureMultiplier;
		public float viscosityStrength;
		public Vector2 boundsSize;
		public Vector2 obstacleSize;
		public Vector2 obstacleCentre;

		[Header("Interaction Settings")]
		[Tooltip("The 3D object (e.g., WaterCube) whose collider is used to detect mouse interaction points")]
		public Collider interactionPlane;
		public float interactionRadius;

		public float interactionStrength;

		[Header("References")]
		public ComputeShader compute;

		public Spawner2D spawner2D;
		public ParticleDisplay2D particleDisplay;

		// Buffers
		public ComputeBuffer positionBuffer { get; private set; }
		public ComputeBuffer velocityBuffer { get; private set; }
		public ComputeBuffer densityBuffer { get; private set; }

		ComputeBuffer sortTarget_Position;
		ComputeBuffer sortTarget_PredicitedPosition;
		ComputeBuffer sortTarget_Velocity;

		ComputeBuffer predictedPositionBuffer;
		SpatialHash spatialHash;

		// Kernel IDs
		const int externalForcesKernel = 0;
		const int spatialHashKernel = 1;
		const int reorderKernel = 2;
		const int copybackKernel = 3;
		const int densityKernel = 4;
		const int pressureKernel = 5;
		const int viscosityKernel = 6;
		const int updatePositionKernel = 7;

		// State
		bool isPaused;
		Spawner2D.ParticleSpawnData spawnData;
		bool pauseNextFrame;

		public int numParticles { get; private set; }


		void Start()
		{
			Debug.Log("Controls: Space = Play/Pause, R = Reset, LMB = Attract, RMB = Repel");

			Init();
		}

		void Init()
		{
			float deltaTime = 1 / 60f;
			Time.fixedDeltaTime = deltaTime;

			spawnData = spawner2D.GetSpawnData();
			numParticles = spawnOnStart ? spawnData.positions.Length : 0;
			int bufferCapacity = Mathf.Max(numParticles, 1);
			spatialHash = new SpatialHash(bufferCapacity);

			// Create buffers
			positionBuffer = ComputeHelper.CreateStructuredBuffer<float2>(bufferCapacity);
			predictedPositionBuffer = ComputeHelper.CreateStructuredBuffer<float2>(bufferCapacity);
			velocityBuffer = ComputeHelper.CreateStructuredBuffer<float2>(bufferCapacity);
			densityBuffer = ComputeHelper.CreateStructuredBuffer<float2>(bufferCapacity);

			sortTarget_Position = ComputeHelper.CreateStructuredBuffer<float2>(bufferCapacity);
			sortTarget_PredicitedPosition = ComputeHelper.CreateStructuredBuffer<float2>(bufferCapacity);
			sortTarget_Velocity = ComputeHelper.CreateStructuredBuffer<float2>(bufferCapacity);

			// Set buffer data
			if (spawnOnStart)
			{
				SetInitialBufferData(spawnData);
			}
			else
			{
				float2[] empty = new float2[bufferCapacity];
				positionBuffer.SetData(empty);
				predictedPositionBuffer.SetData(empty);
				velocityBuffer.SetData(empty);
			}

			// Init compute
			ComputeHelper.SetBuffer(compute, positionBuffer, "Positions", externalForcesKernel, updatePositionKernel, reorderKernel, copybackKernel);
			ComputeHelper.SetBuffer(compute, predictedPositionBuffer, "PredictedPositions", externalForcesKernel, spatialHashKernel, densityKernel, pressureKernel, viscosityKernel, reorderKernel, copybackKernel);
			ComputeHelper.SetBuffer(compute, velocityBuffer, "Velocities", externalForcesKernel, pressureKernel, viscosityKernel, updatePositionKernel, reorderKernel, copybackKernel);
			ComputeHelper.SetBuffer(compute, densityBuffer, "Densities", densityKernel, pressureKernel, viscosityKernel);

			ComputeHelper.SetBuffer(compute, spatialHash.SpatialIndices, "SortedIndices", spatialHashKernel, reorderKernel);
			ComputeHelper.SetBuffer(compute, spatialHash.SpatialOffsets, "SpatialOffsets", spatialHashKernel, densityKernel, pressureKernel, viscosityKernel);
			ComputeHelper.SetBuffer(compute, spatialHash.SpatialKeys, "SpatialKeys", spatialHashKernel, densityKernel, pressureKernel, viscosityKernel);

			ComputeHelper.SetBuffer(compute, sortTarget_Position, "SortTarget_Positions", reorderKernel, copybackKernel);
			ComputeHelper.SetBuffer(compute, sortTarget_PredicitedPosition, "SortTarget_PredictedPositions", reorderKernel, copybackKernel);
			ComputeHelper.SetBuffer(compute, sortTarget_Velocity, "SortTarget_Velocities", reorderKernel, copybackKernel);

			compute.SetInt("numParticles", numParticles);
		}


		void Update()
		{
			if (!isPaused)
			{
				float maxDeltaTime = maxTimestepFPS > 0 ? 1 / maxTimestepFPS : float.PositiveInfinity; // If framerate dips too low, run the simulation slower than real-time
				float dt = Mathf.Min(Time.deltaTime * timeScale, maxDeltaTime);
				RunSimulationFrame(dt);
			}

			if (pauseNextFrame)
			{
				isPaused = true;
				pauseNextFrame = false;
			}

			HandleInput();
		}

		void RunSimulationFrame(float frameTime)
		{
			if (numParticles <= 0)
			{
				return;
			}

			float timeStep = frameTime / iterationsPerFrame;

			UpdateSettings(timeStep);

			for (int i = 0; i < iterationsPerFrame; i++)
			{
				RunSimulationStep();
				SimulationStepCompleted?.Invoke();
			}
		}

		void RunSimulationStep()
		{
			if (numParticles <= 0)
			{
				return;
			}

			ComputeHelper.Dispatch(compute, numParticles, kernelIndex: externalForcesKernel);

			RunSpatial();

			ComputeHelper.Dispatch(compute, numParticles, kernelIndex: densityKernel);
			ComputeHelper.Dispatch(compute, numParticles, kernelIndex: pressureKernel);
			ComputeHelper.Dispatch(compute, numParticles, kernelIndex: viscosityKernel);
			ComputeHelper.Dispatch(compute, numParticles, kernelIndex: updatePositionKernel);
		}

		void RunSpatial()
		{
			if (numParticles <= 0)
			{
				return;
			}

			ComputeHelper.Dispatch(compute, numParticles, kernelIndex: spatialHashKernel);
			spatialHash.Run();

			ComputeHelper.Dispatch(compute, numParticles, kernelIndex: reorderKernel);
			ComputeHelper.Dispatch(compute, numParticles, kernelIndex: copybackKernel);
		}

		void UpdateSettings(float deltaTime)
		{
			compute.SetFloat("deltaTime", deltaTime);
			Vector2 g = gravityDir.normalized * gravity;
			compute.SetVector("gravityVec", new Vector4(g.x, g.y, 0f, 0f));

			compute.SetFloat("collisionDamping", collisionDamping);
			compute.SetFloat("smoothingRadius", smoothingRadius);
			compute.SetFloat("collisionDamping", collisionDamping);
			compute.SetFloat("smoothingRadius", smoothingRadius);
			compute.SetFloat("targetDensity", targetDensity);
			compute.SetFloat("pressureMultiplier", pressureMultiplier);
			compute.SetFloat("nearPressureMultiplier", nearPressureMultiplier);
			compute.SetFloat("viscosityStrength", viscosityStrength);
			compute.SetVector("boundsSize", boundsSize);
			compute.SetVector("obstacleSize", obstacleSize);
			compute.SetVector("obstacleCentre", obstacleCentre);

			compute.SetVector("worldOffset", new Vector4(worldOffset.x, worldOffset.y, 0f, 0f));
        	compute.SetFloat("worldScale", worldScale);

			compute.SetFloat("Poly6ScalingFactor", 4 / (Mathf.PI * Mathf.Pow(smoothingRadius, 8)));
			compute.SetFloat("SpikyPow3ScalingFactor", 10 / (Mathf.PI * Mathf.Pow(smoothingRadius, 5)));
			compute.SetFloat("SpikyPow2ScalingFactor", 6 / (Mathf.PI * Mathf.Pow(smoothingRadius, 4)));
			compute.SetFloat("SpikyPow3DerivativeScalingFactor", 30 / (Mathf.Pow(smoothingRadius, 5) * Mathf.PI));
			compute.SetFloat("SpikyPow2DerivativeScalingFactor", 12 / (Mathf.Pow(smoothingRadius, 4) * Mathf.PI));

			// Mouse interaction settings:
			Vector2 interactionPoint = Vector2.zero;
			if (interactionPlane != null)
			{
				Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				if (interactionPlane.Raycast(ray, out RaycastHit hit, float.MaxValue))
				{
					// Convert world-space hit point to simulation local coordinates
					interactionPoint = WorldToSimLocal(hit.point);
				}
			}

			bool isPullInteraction = Input.GetMouseButton(0);
			bool isPushInteraction = Input.GetMouseButton(1);
			float currInteractStrength = 0;
			if (isPushInteraction || isPullInteraction)
			{
				currInteractStrength = isPushInteraction ? -interactionStrength : interactionStrength;
			}

			compute.SetVector("interactionInputPoint", interactionPoint);
			compute.SetFloat("interactionInputStrength", currInteractStrength);
			compute.SetFloat("interactionInputRadius", interactionRadius);
		}

		void SetInitialBufferData(Spawner2D.ParticleSpawnData spawnData)
		{
			float2[] allPoints = new float2[spawnData.positions.Length]; //
			System.Array.Copy(spawnData.positions, allPoints, spawnData.positions.Length);

			positionBuffer.SetData(allPoints);
			predictedPositionBuffer.SetData(allPoints);
			velocityBuffer.SetData(spawnData.velocities);
		}

		void HandleInput()
		{
			if (Input.GetKeyDown(KeyCode.Space))
			{
				isPaused = !isPaused;
			}

			if (Input.GetKeyDown(KeyCode.RightArrow))
			{
				isPaused = false;
				pauseNextFrame = true;
			}

			if (Input.GetKeyDown(KeyCode.R))
			{
				isPaused = true;
				// Reset positions, the run single frame to get density etc (for debug purposes) and then reset positions again
				SetInitialBufferData(spawnData);
				RunSimulationStep();
				SetInitialBufferData(spawnData);
			}

			if (Input.GetKeyDown(KeyCode.P))
			{
				SpawnParticles(spawner2D.GetSpawnData(), Vector2.zero);
			}
		}

		public void SpawnParticles(Spawner2D.ParticleSpawnData spawnData, Vector2 spawnOffsetLocal)
		{
			float2 offset = new float2(spawnOffsetLocal.x, spawnOffsetLocal.y);
			int additionalCount = spawnData.positions.Length;
			float2[] offsetPositions = new float2[additionalCount];
			for (int i = 0; i < additionalCount; i++)
			{
				offsetPositions[i] = spawnData.positions[i] + offset;
			}

			// Store current particle data
			var oldPositions = new float2[numParticles];
			var oldVelocities = new float2[numParticles];
			positionBuffer.GetData(oldPositions);
			velocityBuffer.GetData(oldVelocities);

			// Create new arrays with combined size
			int newParticleCount = numParticles + additionalCount;
			var allPositions = new float2[newParticleCount];
			var allVelocities = new float2[newParticleCount];

			// Copy old and new data
			System.Array.Copy(oldPositions, allPositions, numParticles);
			System.Array.Copy(offsetPositions, 0, allPositions, numParticles, additionalCount);
			System.Array.Copy(oldVelocities, allVelocities, numParticles);
			System.Array.Copy(spawnData.velocities, 0, allVelocities, numParticles, spawnData.velocities.Length);

			// Release old buffers
			ComputeHelper.Release(positionBuffer, predictedPositionBuffer, velocityBuffer, densityBuffer, sortTarget_Position, sortTarget_Velocity, sortTarget_PredicitedPosition);
			spatialHash.Release();

			// Update particle count and re-initialize buffers and spatial hash
			numParticles = newParticleCount;
			spatialHash = new SpatialHash(numParticles);

			// Create new buffers
			positionBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
			predictedPositionBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
			velocityBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
			densityBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
			sortTarget_Position = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
			sortTarget_PredicitedPosition = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
			sortTarget_Velocity = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);

			// Set data on new buffers
			positionBuffer.SetData(allPositions);
			predictedPositionBuffer.SetData(allPositions);
			velocityBuffer.SetData(allVelocities);

			// Re-bind all buffers to the compute shader
			ComputeHelper.SetBuffer(compute, positionBuffer, "Positions", externalForcesKernel, updatePositionKernel, reorderKernel, copybackKernel);
			ComputeHelper.SetBuffer(compute, predictedPositionBuffer, "PredictedPositions", externalForcesKernel, spatialHashKernel, densityKernel, pressureKernel, viscosityKernel, reorderKernel, copybackKernel);
			ComputeHelper.SetBuffer(compute, velocityBuffer, "Velocities", externalForcesKernel, pressureKernel, viscosityKernel, updatePositionKernel, reorderKernel, copybackKernel);
			ComputeHelper.SetBuffer(compute, densityBuffer, "Densities", densityKernel, pressureKernel, viscosityKernel);
			ComputeHelper.SetBuffer(compute, spatialHash.SpatialIndices, "SortedIndices", spatialHashKernel, reorderKernel);
			ComputeHelper.SetBuffer(compute, spatialHash.SpatialOffsets, "SpatialOffsets", spatialHashKernel, densityKernel, pressureKernel, viscosityKernel);
			ComputeHelper.SetBuffer(compute, spatialHash.SpatialKeys, "SpatialKeys", spatialHashKernel, densityKernel, pressureKernel, viscosityKernel);
			ComputeHelper.SetBuffer(compute, sortTarget_Position, "SortTarget_Positions", reorderKernel, copybackKernel);
			ComputeHelper.SetBuffer(compute, sortTarget_PredicitedPosition, "SortTarget_PredictedPositions", reorderKernel, copybackKernel);
			ComputeHelper.SetBuffer(compute, sortTarget_Velocity, "SortTarget_Velocities", reorderKernel, copybackKernel);

			compute.SetInt("numParticles", numParticles);
		}

		public Vector2 WorldToSimLocal(Vector3 worldPos)
		{
			Transform anchor = particleDisplay != null ? particleDisplay.worldAnchor : null;
			Vector3 local = anchor != null ? anchor.InverseTransformPoint(worldPos) : worldPos;
			float scale = Mathf.Approximately(worldScale, 0f) ? 1f : worldScale;
			return ((Vector2)local - worldOffset) / scale;
		}


		void OnDestroy()
		{
			ComputeHelper.Release(positionBuffer, predictedPositionBuffer, velocityBuffer, densityBuffer, sortTarget_Position, sortTarget_Velocity, sortTarget_PredicitedPosition);
			spatialHash.Release();
		}


		void OnDrawGizmos()
		{
			// Determine the center for the gizmo
			Vector3 gizmoCentre = transform.position;
			if (particleDisplay != null && particleDisplay.worldAnchor != null)
			{
				gizmoCentre = particleDisplay.worldAnchor.position;
			}
			gizmoCentre += (Vector3)worldOffset;


			Gizmos.color = new Color(0, 1, 0, 0.4f);
			Gizmos.DrawWireCube(gizmoCentre, boundsSize * worldScale);
			Gizmos.DrawWireCube(gizmoCentre + (Vector3)obstacleCentre, obstacleSize * worldScale);

			if (Application.isPlaying)
			{
				Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				bool isPullInteraction = Input.GetMouseButton(0);
				bool isPushInteraction = Input.GetMouseButton(1);
				bool isInteracting = isPullInteraction || isPushInteraction;
				if (isInteracting)
				{
					Gizmos.color = isPullInteraction ? Color.green : Color.red;
					Gizmos.DrawWireSphere(mousePos, interactionRadius);
				}
			}
		}
	}
}