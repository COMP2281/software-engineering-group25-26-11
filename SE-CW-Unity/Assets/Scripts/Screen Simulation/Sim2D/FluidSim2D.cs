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
		public Vector3 worldOffset;   // X/Y/Z origin of the 2D sim in world space
		public float worldScale = 1f; // Optional: scale sim units to world units
		[Tooltip("Automatically scale bounds by parent's transform scale")]
		public bool scaleByParent = true;
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
		public ComputeBuffer colorBuffer { get; private set; }

		ComputeBuffer sortTarget_Position;
		ComputeBuffer sortTarget_PredicitedPosition;
		ComputeBuffer sortTarget_Velocity;
		ComputeBuffer sortTarget_Color;

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
		public bool IsPaused => isPaused;


		void Start()
		{
			Debug.Log("Controls: Space = Play/Pause, R = Reset, C = Clear Paint, LMB = Attract, RMB = Repel");

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
			colorBuffer = ComputeHelper.CreateStructuredBuffer<float4>(bufferCapacity);

			sortTarget_Position = ComputeHelper.CreateStructuredBuffer<float2>(bufferCapacity);
			sortTarget_PredicitedPosition = ComputeHelper.CreateStructuredBuffer<float2>(bufferCapacity);
			sortTarget_Velocity = ComputeHelper.CreateStructuredBuffer<float2>(bufferCapacity);
			sortTarget_Color = ComputeHelper.CreateStructuredBuffer<float4>(bufferCapacity);

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
			ComputeHelper.SetBuffer(compute, colorBuffer, "ParticleColors", reorderKernel, copybackKernel);
			ComputeHelper.SetBuffer(compute, sortTarget_Color, "SortTarget_Colors", reorderKernel, copybackKernel);

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
			
			// Apply parent scale to bounds if enabled
			Vector2 scaledBoundsSize = boundsSize;
			if (scaleByParent && transform.parent != null)
			{
				Vector3 parentScale = transform.parent.lossyScale;
				scaledBoundsSize = new Vector2(boundsSize.x * parentScale.x, boundsSize.y * parentScale.y);
			}
			
			compute.SetVector("boundsSize", scaledBoundsSize);
			compute.SetVector("obstacleSize", obstacleSize);
			compute.SetVector("obstacleCentre", obstacleCentre);

			compute.SetVector("worldOffset", new Vector4(worldOffset.x, worldOffset.y, worldOffset.z, 0f));
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

			bool isPushInteraction = Input.GetMouseButton(0);
			bool isPullInteraction = Input.GetMouseButton(1);
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

			// Set colors (default to white if not provided)
			if (spawnData.colors != null && spawnData.colors.Length > 0)
			{
				colorBuffer.SetData(spawnData.colors);
			}
			else
			{
				float4[] defaultColors = new float4[spawnData.positions.Length];
				for (int i = 0; i < defaultColors.Length; i++)
				{
					defaultColors[i] = new float4(1, 1, 1, 1);
				}
				colorBuffer.SetData(defaultColors);
			}
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

			if (Input.GetKeyDown(KeyCode.C))
			{
				ClearAllParticles();
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
			var oldColors = new float4[numParticles];
			positionBuffer.GetData(oldPositions);
			velocityBuffer.GetData(oldVelocities);
			colorBuffer.GetData(oldColors);

			// Create new arrays with combined size
			int newParticleCount = numParticles + additionalCount;
			var allPositions = new float2[newParticleCount];
			var allVelocities = new float2[newParticleCount];
			var allColors = new float4[newParticleCount];

			// Copy old and new data
			System.Array.Copy(oldPositions, allPositions, numParticles);
			System.Array.Copy(offsetPositions, 0, allPositions, numParticles, additionalCount);
			System.Array.Copy(oldVelocities, allVelocities, numParticles);
			System.Array.Copy(spawnData.velocities, 0, allVelocities, numParticles, spawnData.velocities.Length);
			System.Array.Copy(oldColors, allColors, numParticles);
			// Copy new colors (default to white if not provided)
			if (spawnData.colors != null && spawnData.colors.Length > 0)
			{
				System.Array.Copy(spawnData.colors, 0, allColors, numParticles, spawnData.colors.Length);
			}
			else
			{
				for (int i = numParticles; i < newParticleCount; i++)
				{
					allColors[i] = new float4(1, 1, 1, 1);
				}
			}

			// Release old buffers
			ComputeHelper.Release(positionBuffer, predictedPositionBuffer, velocityBuffer, densityBuffer, colorBuffer, sortTarget_Position, sortTarget_Velocity, sortTarget_PredicitedPosition, sortTarget_Color);
			spatialHash.Release();

			// Update particle count and re-initialize buffers and spatial hash
			numParticles = newParticleCount;
			spatialHash = new SpatialHash(numParticles);

			// Create new buffers
			positionBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
			predictedPositionBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
			velocityBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
			densityBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
			colorBuffer = ComputeHelper.CreateStructuredBuffer<float4>(numParticles);
			sortTarget_Position = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
			sortTarget_PredicitedPosition = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
			sortTarget_Velocity = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
			sortTarget_Color = ComputeHelper.CreateStructuredBuffer<float4>(numParticles);

			// Set data on new buffers
			positionBuffer.SetData(allPositions);
			predictedPositionBuffer.SetData(allPositions);
			velocityBuffer.SetData(allVelocities);
			colorBuffer.SetData(allColors);

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
			ComputeHelper.SetBuffer(compute, colorBuffer, "ParticleColors", reorderKernel, copybackKernel);
			ComputeHelper.SetBuffer(compute, sortTarget_Color, "SortTarget_Colors", reorderKernel, copybackKernel);

			compute.SetInt("numParticles", numParticles);
		}

		/// <summary>
		/// Clears all particles from the simulation (resets the paint screen)
		/// </summary>
		public void ClearAllParticles()
		{
			if (numParticles == 0) return;

			// Store empty data
			numParticles = 0;
			compute.SetInt("numParticles", numParticles);

			Debug.Log("[FluidSim2D] All particles cleared.");
		}

		/// <summary>
		/// Toggles the simulation between paused and playing state.
		/// Call this from UI button OnClick event.
		/// </summary>
		public void TogglePause()
		{
			isPaused = !isPaused;
			Debug.Log($"[FluidSim2D] Simulation {(isPaused ? "Paused" : "Playing")}");
		}

		/// <summary>
		/// Sets the pause state explicitly
		/// </summary>
		public void SetPaused(bool paused)
		{
			isPaused = paused;
		}

		public Vector2 WorldToSimLocal(Vector3 worldPos)
		{
			Transform anchor = particleDisplay != null ? particleDisplay.worldAnchor : null;
			Vector3 local = anchor != null ? anchor.InverseTransformPoint(worldPos) : worldPos;
			float scale = Mathf.Approximately(worldScale, 0f) ? 1f : worldScale;
			return ((Vector2)local - (Vector2)worldOffset) / scale;
		}


		void OnDestroy()
		{
			ComputeHelper.Release(positionBuffer, predictedPositionBuffer, velocityBuffer, densityBuffer, colorBuffer, sortTarget_Position, sortTarget_Velocity, sortTarget_PredicitedPosition, sortTarget_Color);
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
			gizmoCentre += worldOffset;

			// Calculate scaled bounds size for display
			Vector2 displayBoundsSize = boundsSize;
			if (scaleByParent && transform.parent != null)
			{
				Vector3 parentScale = transform.parent.lossyScale;
				displayBoundsSize = new Vector2(boundsSize.x * parentScale.x, boundsSize.y * parentScale.y);
			}

			Gizmos.color = new Color(0, 1, 0, 0.4f);
			Gizmos.DrawWireCube(gizmoCentre, displayBoundsSize * worldScale);
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