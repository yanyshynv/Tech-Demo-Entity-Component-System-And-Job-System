using UnityEngine;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;

namespace TechDemo
{
	public struct FallSpeed : IComponentData
	{
		public float Value;
	}

	public struct AngularSpeed : IComponentData
    {
        public float Value;
    }

	public struct StartPosition : IComponentData
	{
		public float3 Value;
	}

	public class FallSystem : JobComponentSystem
	{
		[BurstCompile]
		private struct FallJob : IJobProcessComponentData<FallSpeed, Position, StartPosition>
		{
			public float deltaTime;
			public float3 parentPos;

			public void Execute(ref FallSpeed speed, ref Position position, ref StartPosition startPosition)
			{
				position.Value.y -= speed.Value * deltaTime;
				if (position.Value.y < 0)
				{
					position.Value = startPosition.Value + parentPos;
				}
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var job = new FallJob
			{
				deltaTime = Time.deltaTime,
				parentPos = SnowFallGenerator.position,
			};
			return job.Schedule(this, 64, inputDeps);
		}
	}

	public class RotationSystem : JobComponentSystem
    {
        [BurstCompile]
		private struct RotateJob : IJobProcessComponentData<AngularSpeed, Rotation>
        {
            public float deltaTime;

			public void Execute(ref AngularSpeed speed, ref Rotation rotation)
            {
				rotation.Value *= Quaternion.Euler(0, 0, speed.Value * deltaTime);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
			var job = new RotateJob
            {
                deltaTime = Time.deltaTime
            };
            return job.Schedule(this, 64, inputDeps);
        }
    }

	public class SnowFallGenerator : MonoBehaviour
	{      
		private EntityManager entityManager;

		public static Vector3 position;
		public float cloudRadius = 100f;
		public float cloudSpeed = 100f;
		public float fallSpeed = 50f;
		public float fallSpeedRandom = 2f;
		public float fallAngularSpeed = 2f;
		public int snowflakesNum = 10000;

		public GameObject snowflakePrefab;

		void Start()
		{
			entityManager = World.Active.GetOrCreateManager<EntityManager>();
			SpawnSnowflakes(snowflakesNum);

		}

		void Update()
		{
			MoveCloud();
		}

		public void SpawnSnowflakes(int amount)
		{
			NativeArray<Entity> entities = new NativeArray<Entity>(amount, Allocator.Temp);
			entityManager.Instantiate(snowflakePrefab, entities);

			for (int i = 0; i < amount; i++)
			{
				Vector2 pos = Random.insideUnitCircle * cloudRadius;

				entityManager.AddComponentData(entities[i], new FallSpeed
				{
					Value = fallSpeed + Random.Range(-fallSpeedRandom, fallSpeedRandom)
				});

				entityManager.AddComponentData(entities[i], new AngularSpeed
				{
					Value = fallAngularSpeed
                });

				entityManager.AddComponentData(entities[i], new StartPosition
				{
					Value = new float3(pos.x, 0, pos.y)
				});

				entityManager.SetComponentData(entities[i], new Position
				{
					Value = new float3(pos.x, Random.Range(0f, transform.position.y), pos.y)
				});

				entityManager.SetComponentData(entities[i], new Rotation
                {
					Value = Quaternion.Euler(90, Random.Range(0f, 360), 0)
                });
			}

			entities.Dispose();
		}

		void MoveCloud()
		{
			transform.position += new Vector3(Input.GetAxis("Horizontal") * cloudSpeed, 0, Input.GetAxis("Vertical") * cloudSpeed) * Time.deltaTime;
			position = transform.position;
		}
	}
}
