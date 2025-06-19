using OpenGLMinecraftClone.World;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;

namespace OpenGLMinecraftClone
{
    public struct RaycastResult
    {
        public bool hit;
        public Vector3i blockPosition;
        public Vector3 hitPoint;
        public Vector3 normal;
        public float distance;
        public BlockType blockType;
        public Faces hitFace;
    }

    public struct PlayerBounds
    {
        public Vector3 center;
        public Vector3 size; // 0.8 width, 1.8 height, 0.8 depth
        public Vector3 min => center - size * 0.5f;
        public Vector3 max => center + size * 0.5f;

        public PlayerBounds(Vector3 position)
        {
            center = position;
            size = new Vector3(0.8f, 1.8f, 0.8f);
        }

        public bool IntersectsBlock(Vector3i blockPos)
        {
            Vector3 blockMin = new Vector3(blockPos.X - 0.5f, blockPos.Y - 0.5f, blockPos.Z - 0.5f);
            Vector3 blockMax = new Vector3(blockPos.X + 0.5f, blockPos.Y + 0.5f, blockPos.Z + 0.5f);

            return min.X < blockMax.X && max.X > blockMin.X &&
                   min.Y < blockMax.Y && max.Y > blockMin.Y &&
                   min.Z < blockMax.Z && max.Z > blockMin.Z;
        }
    }

    public class BlockDetection
    {
        private ChunkManager chunkManager;
        private const float MAX_REACH_DISTANCE = 5.0f;
        private const float RAYCAST_STEP = 0.1f;

        public BlockDetection(ChunkManager chunkManager)
        {
            this.chunkManager = chunkManager;
        }

        public RaycastResult RaycastBlock(Vector3 origin, Vector3 direction, float maxDistance = MAX_REACH_DISTANCE)
        {
            RaycastResult result = new RaycastResult();
            result.hit = false;

            Vector3 normalizedDir = Vector3.Normalize(direction);
            Vector3 currentPos = origin;
            Vector3i lastBlockPos = new Vector3i(int.MinValue);

            for (float distance = 0; distance < maxDistance; distance += RAYCAST_STEP)
            {
                currentPos = origin + normalizedDir * distance;
                Vector3i blockPos = new Vector3i(
                    (int)Math.Floor(currentPos.X),
                    (int)Math.Floor(currentPos.Y),
                    (int)Math.Floor(currentPos.Z)
                );

                // Skip if we're still in the same block
                if (blockPos == lastBlockPos)
                    continue;

                lastBlockPos = blockPos;
                BlockType blockType = GetBlockAt(blockPos);

                if (blockType != BlockType.AIR)
                {
                    result.hit = true;
                    result.blockPosition = blockPos;
                    result.hitPoint = currentPos;
                    result.distance = distance;
                    result.blockType = blockType;
                    result.normal = CalculateBlockNormal(currentPos, blockPos);
                    result.hitFace = GetHitFace(result.normal);
                    break;
                }
            }

            return result;
        }

        private BlockType GetBlockAt(Vector3i blockPos)
        {
            Vector3i chunkCoord = Chunk.WorldToChunkCoord(new Vector3(blockPos.X, blockPos.Y, blockPos.Z));

            // Try to get the chunk from the chunk manager
            var loadedChunks = GetLoadedChunks();
            if (loadedChunks.TryGetValue(chunkCoord, out Chunk chunk))
            {
                int localX = blockPos.X - chunkCoord.X * Chunk.SIZE;
                int localY = blockPos.Y - chunkCoord.Y * Chunk.SIZE;
                int localZ = blockPos.Z - chunkCoord.Z * Chunk.SIZE;

                // Ensure coordinates are within chunk bounds
                if (localX >= 0 && localX < Chunk.SIZE &&
                    localY >= 0 && localY < Chunk.SIZE &&
                    localZ >= 0 && localZ < Chunk.SIZE)
                {
                    return chunk.chunkBlocks[localX, localY, localZ];
                }
            }

            return BlockType.AIR;
        }

        private Dictionary<Vector3i, Chunk> GetLoadedChunks()
        {
            // Access private field through reflection or make it internal/public
            var field = typeof(ChunkManager).GetField("loadedChunks",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (Dictionary<Vector3i, Chunk>)field.GetValue(chunkManager);
        }

        private Vector3 CalculateBlockNormal(Vector3 hitPoint, Vector3i blockPos)
        {
            Vector3 blockCenter = new Vector3(blockPos.X, blockPos.Y, blockPos.Z);
            Vector3 diff = hitPoint - blockCenter;

            // Find the face with the largest absolute component
            Vector3 abs = new Vector3(Math.Abs(diff.X), Math.Abs(diff.Y), Math.Abs(diff.Z));

            if (abs.X >= abs.Y && abs.X >= abs.Z)
                return new Vector3(Math.Sign(diff.X), 0, 0);
            else if (abs.Y >= abs.Z)
                return new Vector3(0, Math.Sign(diff.Y), 0);
            else
                return new Vector3(0, 0, Math.Sign(diff.Z));
        }

        private Faces GetHitFace(Vector3 normal)
        {
            if (normal.X > 0) return Faces.RIGHT;
            if (normal.X < 0) return Faces.LEFT;
            if (normal.Y > 0) return Faces.TOP;
            if (normal.Y < 0) return Faces.BOTTOM;
            if (normal.Z > 0) return Faces.FRONT;
            return Faces.BACK;
        }

        public bool MineBlock(Vector3i blockPos)
        {
            Vector3i chunkCoord = Chunk.WorldToChunkCoord(new Vector3(blockPos.X, blockPos.Y, blockPos.Z));
            var loadedChunks = GetLoadedChunks();

            if (loadedChunks.TryGetValue(chunkCoord, out Chunk chunk))
            {
                int localX = blockPos.X - chunkCoord.X * Chunk.SIZE;
                int localY = blockPos.Y - chunkCoord.Y * Chunk.SIZE;
                int localZ = blockPos.Z - chunkCoord.Z * Chunk.SIZE;

                if (localX >= 0 && localX < Chunk.SIZE &&
                    localY >= 0 && localY < Chunk.SIZE &&
                    localZ >= 0 && localZ < Chunk.SIZE)
                {
                    if (chunk.chunkBlocks[localX, localY, localZ] != BlockType.AIR)
                    {
                        chunk.chunkBlocks[localX, localY, localZ] = BlockType.AIR;
                        chunk.needsRebuild = true;
                        chunk.BuildMesh();
                        return true;
                    }
                }
            }
            return false;
        }

        public bool PlaceBlock(Vector3i blockPos, BlockType blockType)
        {
            Vector3i chunkCoord = Chunk.WorldToChunkCoord(new Vector3(blockPos.X, blockPos.Y, blockPos.Z));
            var loadedChunks = GetLoadedChunks();

            if (loadedChunks.TryGetValue(chunkCoord, out Chunk chunk))
            {
                int localX = blockPos.X - chunkCoord.X * Chunk.SIZE;
                int localY = blockPos.Y - chunkCoord.Y * Chunk.SIZE;
                int localZ = blockPos.Z - chunkCoord.Z * Chunk.SIZE;

                if (localX >= 0 && localX < Chunk.SIZE &&
                    localY >= 0 && localY < Chunk.SIZE &&
                    localZ >= 0 && localZ < Chunk.SIZE)
                {
                    if (chunk.chunkBlocks[localX, localY, localZ] == BlockType.AIR)
                    {
                        chunk.chunkBlocks[localX, localY, localZ] = blockType;
                        chunk.isEmpty = false;
                        chunk.needsRebuild = true;
                        chunk.BuildMesh();
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public class CollisionDetection
    {
        private ChunkManager chunkManager;
        private BlockDetection blockDetection;

        public CollisionDetection(ChunkManager chunkManager)
        {
            this.chunkManager = chunkManager;
            this.blockDetection = new BlockDetection(chunkManager);
        }

        public Vector3 HandlePlayerCollision(Vector3 currentPosition, Vector3 newPosition)
        {
            PlayerBounds currentBounds = new PlayerBounds(currentPosition);
            PlayerBounds newBounds = new PlayerBounds(newPosition);

            // Check collision for each axis separately for better sliding behavior
            Vector3 resolvedPosition = currentPosition;

            // Test X movement
            Vector3 testPosX = new Vector3(newPosition.X, currentPosition.Y, currentPosition.Z);
            if (!IsPlayerColliding(testPosX))
            {
                resolvedPosition.X = newPosition.X;
            }

            // Test Y movement
            Vector3 testPosY = new Vector3(resolvedPosition.X, newPosition.Y, currentPosition.Z);
            if (!IsPlayerColliding(testPosY))
            {
                resolvedPosition.Y = newPosition.Y;
            }

            // Test Z movement
            Vector3 testPosZ = new Vector3(resolvedPosition.X, resolvedPosition.Y, newPosition.Z);
            if (!IsPlayerColliding(testPosZ))
            {
                resolvedPosition.Z = newPosition.Z;
            }

            return resolvedPosition;
        }

        private bool IsPlayerColliding(Vector3 playerPosition)
        {
            PlayerBounds bounds = new PlayerBounds(playerPosition);

            // Get all block positions that could intersect with player bounds
            Vector3i minBlock = new Vector3i(
                (int)Math.Floor(bounds.min.X),
                (int)Math.Floor(bounds.min.Y),
                (int)Math.Floor(bounds.min.Z)
            );

            Vector3i maxBlock = new Vector3i(
                (int)Math.Ceiling(bounds.max.X),
                (int)Math.Ceiling(bounds.max.Y),
                (int)Math.Ceiling(bounds.max.Z)
            );

            for (int x = minBlock.X; x <= maxBlock.X; x++)
            {
                for (int y = minBlock.Y; y <= maxBlock.Y; y++)
                {
                    for (int z = minBlock.Z; z <= maxBlock.Z; z++)
                    {
                        Vector3i blockPos = new Vector3i(x, y, z);
                        BlockType blockType = GetBlockAt(blockPos);

                        if (blockType != BlockType.AIR && bounds.IntersectsBlock(blockPos))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private BlockType GetBlockAt(Vector3i blockPos)
        {
            return blockDetection.GetType()
                .GetMethod("GetBlockAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(blockDetection, new object[] { blockPos }) as BlockType? ?? BlockType.AIR;
        }

        public Vector3 FindSafeSpawnPosition(Vector3 preferredPosition)
        {
            // Start from preferred position and move up until we find a safe spot
            Vector3 testPosition = preferredPosition;

            for (int attempts = 0; attempts < 50; attempts++)
            {
                if (!IsPlayerColliding(testPosition))
                {
                    return testPosition;
                }
                testPosition.Y += 1.0f;
            }

            // If no safe position found, return high above preferred position
            return preferredPosition + new Vector3(0, 50, 0);
        }

        public bool IsOnGround(Vector3 playerPosition)
        {
            Vector3 testPosition = playerPosition - new Vector3(0, 0.01f, 0);
            return IsPlayerColliding(testPosition);
        }
    }

    // Player class that uses the detection systems
    public class Player
    {
        private Camera camera;
        private BlockDetection blockDetection;
        private CollisionDetection collisionDetection;
        private Vector3 velocity = Vector3.Zero;
        private const float GRAVITY = -20.0f;
        private const float JUMP_FORCE = 8.0f;
        private bool isOnGround = false;
        private bool creativeMode = true;

        public Vector3 Position => camera.position;

        public Player(float width, float height, Vector3 position, ChunkManager chunkManager)
        {
            camera = new Camera(width, height, position);
            blockDetection = new BlockDetection(chunkManager);
            collisionDetection = new CollisionDetection(chunkManager);

            // Find safe spawn position
            camera.position = collisionDetection.FindSafeSpawnPosition(position);
        }

        public RaycastResult GetTargetBlock()
        {
            return blockDetection.RaycastBlock(camera.position, camera.Front);
        }

        public bool MineTargetBlock()
        {
            var target = GetTargetBlock();
            if (target.hit)
            {
                return blockDetection.MineBlock(target.blockPosition);
            }
            return false;
        }

        public bool PlaceBlock(BlockType blockType)
        {
            var target = GetTargetBlock();
            if (target.hit)
            {
                // Place block adjacent to hit face
                // Vector3 normal = target.normal; // assuming this is a Vector3

                Vector3i placePosition = target.blockPosition + new Vector3i(
                    (int)MathF.Round(target.normal.X),
                    (int)MathF.Round(target.normal.Y),
                    (int)MathF.Round(target.normal.Z)
                );

                // Check if player would be inside the block
                PlayerBounds playerBounds = new PlayerBounds(camera.position);
                PlayerBounds blockBounds = new PlayerBounds(new Vector3(placePosition.X, placePosition.Y, placePosition.Z));

                if (!playerBounds.IntersectsBlock(placePosition))
                {
                    return blockDetection.PlaceBlock(placePosition, blockType);
                }
            }
            return false;
        }

        public void Update(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            // Handle mouse look first
            camera.InputController(input, mouse, e);

            // Store old position
            Vector3 oldPosition = camera.position;

            if (!creativeMode)
            {
                // Apply gravity in survival mode
                velocity.Y += GRAVITY * (float)e.Time;

                // Check if on ground
                isOnGround = collisionDetection.IsOnGround(camera.position);

                // Handle jumping
                if (input.IsKeyDown(Keys.Space) && isOnGround)
                {
                    velocity.Y = JUMP_FORCE;
                }
            }

            // Calculate movement from input (override camera's direct movement)
            Vector3 inputMovement = Vector3.Zero;
            float speed = 10.0f;
            Vector3 frontFlat = new Vector3(camera.Front.X, 0, camera.Front.Z).Normalized();
            Vector3 rightFlat = Vector3.Cross(frontFlat, Vector3.UnitY).Normalized();

            if (input.IsKeyDown(Keys.W))
                inputMovement += frontFlat * speed * (float)e.Time;
            if (input.IsKeyDown(Keys.S))
                inputMovement -= frontFlat * speed * (float)e.Time;
            if (input.IsKeyDown(Keys.A))
                inputMovement -= rightFlat * speed * (float)e.Time;
            if (input.IsKeyDown(Keys.D))
                inputMovement += rightFlat * speed * (float)e.Time;

            // Creative mode flying
            if (creativeMode)
            {
                if (input.IsKeyDown(Keys.Space))
                    inputMovement.Y += speed * (float)e.Time;
                if (input.IsKeyDown(Keys.LeftShift))
                    inputMovement.Y -= speed * (float)e.Time;
            }

            // Combine input movement with velocity
            Vector3 totalMovement = inputMovement;
            if (!creativeMode)
            {
                totalMovement += new Vector3(0, velocity.Y * (float)e.Time, 0);
            }

            // Apply collision detection
            Vector3 newPosition = collisionDetection.HandlePlayerCollision(camera.position, camera.position + totalMovement);

            // Reset Y velocity if we hit ground or ceiling (survival mode)
            if (!creativeMode && Math.Abs(newPosition.Y - (camera.position.Y + velocity.Y * (float)e.Time)) > 0.001f)
            {
                velocity.Y = 0;
            }

            camera.position = newPosition;
        }

        public void ToggleCreativeMode()
        {
            creativeMode = !creativeMode;
            if (creativeMode)
            {
                velocity = Vector3.Zero;
            }
        }

        public void HandleMouseClick(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left)
            {
                MineTargetBlock();
            }
            else if (e.Button == MouseButton.Right)
            {
                PlaceBlock(BlockType.DIRT); // Default block type
            }
        }

        // Debug info
        public string GetDebugInfo()
        {
            var target = GetTargetBlock();
            return $"Position: {camera.position:F2}\n" +
                   $"On Ground: {isOnGround}\n" +
                   $"Creative: {creativeMode}\n" +
                   $"Velocity: {velocity:F2}\n" +
                   $"Target Block: {(target.hit ? $"{target.blockType} at {target.blockPosition}" : "None")}";
        }
    }

    // Usage example in your main game class
}