using System.Collections.Generic;
using Blockbound.Blocks;
using Blockbound.Core;
using Blockbound.World;
using UnityEngine;

namespace Blockbound.Generation
{
    public class CloudManager : MonoBehaviour
    {
        [SerializeField] private VoxelWorld world;
        [SerializeField] private int cloudCount = 12;
        [SerializeField] private float cloudSpeed = 0.5f;
        [SerializeField] private int minHeight = 180;
        [SerializeField] private int maxHeight = 220;

        private readonly List<CloudInstance> clouds = new List<CloudInstance>();

        private void Start()
        {
            if (world == null)
                world = FindFirstObjectByType<VoxelWorld>();

            GenerateInitialClouds();
        }

        private void Update()
        {
            if (world == null)
                return;

            float delta = Time.deltaTime;

            for (int i = 0; i < clouds.Count; i++)
                clouds[i].Update(world, cloudSpeed * delta);
        }

        private void GenerateInitialClouds()
        {
            for (int i = 0; i < cloudCount; i++)
            {
                int x = Random.Range(-300, 300);
                int z = Random.Range(-300, 300);
                int y = Random.Range(minHeight, maxHeight);

                CloudInstance cloud = new CloudInstance();
                cloud.Initialize(new Vector3(x, y, z));
                clouds.Add(cloud);
            }
        }
    }

    internal class CloudInstance
    {
        private Vector3 position;
        private bool[,,] shape;
        private Vector3 lastPosition;

        private const ushort CloudBlockId = 15;

        public void Initialize(Vector3 startPosition)
        {
            position = startPosition;
            lastPosition = position;
            GenerateShape();
        }

        public void Update(VoxelWorld world, float movement)
        {
            RemoveFromWorld(world);
            position += new Vector3(movement, 0, 0);
            PlaceInWorld(world);
            lastPosition = position;
        }

        private void GenerateShape()
        {
            int sizeX = Random.Range(8, 16);
            int sizeZ = Random.Range(6, 14);
            int sizeY = Random.Range(2, 4);

            shape = new bool[sizeX, sizeY, sizeZ];

            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        float edgeFalloff =
                            Mathf.Abs((x - sizeX * 0.5f) / sizeX) +
                            Mathf.Abs((z - sizeZ * 0.5f) / sizeZ);

                        shape[x, y, z] = edgeFalloff < 0.75f + Random.Range(-0.1f, 0.1f);
                    }
                }
            }
        }

        private void PlaceInWorld(VoxelWorld world)
        {
            for (int x = 0; x < shape.GetLength(0); x++)
            {
                for (int y = 0; y < shape.GetLength(1); y++)
                {
                    for (int z = 0; z < shape.GetLength(2); z++)
                    {
                        if (!shape[x, y, z])
                            continue;

                        int wx = Mathf.FloorToInt(position.x) + x;
                        int wy = Mathf.FloorToInt(position.y) + y;
                        int wz = Mathf.FloorToInt(position.z) + z;

                        world.SetBlock(wx, wy, wz, new BlockData(CloudBlockId));
                    }
                }
            }
        }

        private void RemoveFromWorld(VoxelWorld world)
        {
            for (int x = 0; x < shape.GetLength(0); x++)
            {
                for (int y = 0; y < shape.GetLength(1); y++)
                {
                    for (int z = 0; z < shape.GetLength(2); z++)
                    {
                        if (!shape[x, y, z])
                            continue;

                        int wx = Mathf.FloorToInt(lastPosition.x) + x;
                        int wy = Mathf.FloorToInt(lastPosition.y) + y;
                        int wz = Mathf.FloorToInt(lastPosition.z) + z;

                        BlockData current = world.GetBlock(wx, wy, wz);
                        if (current.Id == CloudBlockId)
                            world.SetBlock(wx, wy, wz, new BlockData(0));
                    }
                }
            }
        }
    }
}