using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using SimplexNoise;
using OpenGLMinecraftClone.Graphics;
using Noise = SimplexNoise.Noise;

namespace OpenGLMinecraftClone.World
{
    // Optimized chunk class with much lower memory usage
    internal class Chunk
    {
        public const int SIZE = 16; // 16x16x16 cubical chunks
        public Vector3i chunkCoord; // Chunk coordinates
        public Vector3 worldPosition; // Actual world position

        public bool isEmpty = true;
        public bool needsRebuild = false;

        // OpenGL objects - now includes lighting VBO
        private VAO chunkVAO;
        private VBO chunkVertexVBO;
        private VBO chunkUVVBO;
        private VBO chunkLightVBO; // New VBO for light levels
        private IBO chunkIBO;

        private int vertexCount = 0;
        private int indexCount = 0;

        // Store only block types - much more memory efficient than storing Block objects
        public BlockType[,,] chunkBlocks = new BlockType[SIZE, SIZE, SIZE];

        // Static shared texture - only load once for all chunks
        private static Texture sharedTexture;
        private static bool textureLoaded = false;

        public Chunk(Vector3i chunkCoord)
        {
            this.chunkCoord = chunkCoord;
            this.worldPosition = new Vector3(chunkCoord.X * SIZE, chunkCoord.Y * SIZE, chunkCoord.Z * SIZE);

            GenerateBlocks();

            // Load shared texture only once
            if (!textureLoaded)
            {
                sharedTexture = new Texture("atlas.PNG");
                textureLoaded = true;
            }

            // Only build mesh if chunk has blocks
            if (!isEmpty)
            {
                BuildMesh();
            }
        }

        public void GenerateBlocks()
        {
            SimplexNoise.Noise.Seed = 123456;

            for (int x = 0; x < SIZE; x++)
            {
                for (int z = 0; z < SIZE; z++)
                {
                    int worldX = chunkCoord.X * SIZE + x;
                    int worldZ = chunkCoord.Z * SIZE + z;

                    float noiseValue = SimplexNoise.Noise.CalcPixel2D(worldX, worldZ, 0.01f);
                    int terrainHeight = (int)(noiseValue / 4.0f) + 32;

                    for (int y = 0; y < SIZE; y++)
                    {
                        int worldY = chunkCoord.Y * SIZE + y;
                        BlockType type = BlockType.AIR;

                        if (worldY < terrainHeight - 1)
                        {
                            type = BlockType.DIRT;
                            isEmpty = false;
                        }
                        if (worldY < terrainHeight - 3)
                        {
                            type = BlockType.STONE;
                            isEmpty = false;
                        }
                        else if (worldY == terrainHeight - 1)
                        {
                            type = BlockType.GRASS;
                            isEmpty = false;
                        }

                        chunkBlocks[x, y, z] = type;
                    }
                }
            }
        }

        public void BuildMesh()
        {
            if (isEmpty) return;

            // Use temporary lists that we'll dispose of after building the mesh
            var tempVerts = new List<Vector3>();
            var tempUVs = new List<Vector2>();
            var tempLightLevels = new List<float>(); // New list for light levels
            var tempIndices = new List<uint>();
            uint currentIndex = 0;

            for (int x = 0; x < SIZE; x++)
            {
                for (int y = 0; y < SIZE; y++)
                {
                    for (int z = 0; z < SIZE; z++)
                    {
                        if (chunkBlocks[x, y, z] == BlockType.AIR) continue;

                        Vector3 blockWorldPos = worldPosition + new Vector3(x, y, z);

                        // Check each face and add only if visible
                        currentIndex = CheckAndAddFace(tempVerts, tempUVs, tempLightLevels, tempIndices, currentIndex, x, y, z, blockWorldPos, Faces.LEFT, -1, 0, 0);
                        currentIndex = CheckAndAddFace(tempVerts, tempUVs, tempLightLevels, tempIndices, currentIndex, x, y, z, blockWorldPos, Faces.RIGHT, 1, 0, 0);
                        currentIndex = CheckAndAddFace(tempVerts, tempUVs, tempLightLevels, tempIndices, currentIndex, x, y, z, blockWorldPos, Faces.BOTTOM, 0, -1, 0);
                        currentIndex = CheckAndAddFace(tempVerts, tempUVs, tempLightLevels, tempIndices, currentIndex, x, y, z, blockWorldPos, Faces.TOP, 0, 1, 0);
                        currentIndex = CheckAndAddFace(tempVerts, tempUVs, tempLightLevels, tempIndices, currentIndex, x, y, z, blockWorldPos, Faces.BACK, 0, 0, -1);
                        currentIndex = CheckAndAddFace(tempVerts, tempUVs, tempLightLevels, tempIndices, currentIndex, x, y, z, blockWorldPos, Faces.FRONT, 0, 0, 1);
                    }
                }
            }

            // Only create OpenGL objects if we have geometry
            if (tempVerts.Count > 0)
            {
                CreateOpenGLObjects(tempVerts, tempUVs, tempLightLevels, tempIndices);
                vertexCount = tempVerts.Count;
                indexCount = tempIndices.Count;
            }

            // Clear temporary data immediately to free memory
            tempVerts.Clear();
            tempUVs.Clear();
            tempLightLevels.Clear();
            tempIndices.Clear();
            tempVerts = null;
            tempUVs = null;
            tempLightLevels = null;
            tempIndices = null;

            // Force garbage collection to free memory immediately
            GC.Collect();
        }

        private uint CheckAndAddFace(List<Vector3> verts, List<Vector2> uvs, List<float> lightLevels, List<uint> indices, uint currentIndex,
            int x, int y, int z, Vector3 blockWorldPos, Faces face, int dx, int dy, int dz)
        {
            int nx = x + dx;
            int ny = y + dy;
            int nz = z + dz;

            bool shouldRender = false;

            // Check if neighbor is outside this chunk or is air
            if (nx < 0 || nx >= SIZE || ny < 0 || ny >= SIZE || nz < 0 || nz >= SIZE)
            {
                shouldRender = true; // Render edge faces (could be optimized with neighbor checking later)
            }
            else
            {
                shouldRender = chunkBlocks[nx, ny, nz] == BlockType.AIR;
            }

            if (shouldRender)
            {
                AddFaceGeometry(verts, uvs, lightLevels, indices, currentIndex, blockWorldPos, face, chunkBlocks[x, y, z]);
                return currentIndex + 4; // 4 vertices per face
            }

            return currentIndex;
        }

        private void AddFaceGeometry(List<Vector3> verts, List<Vector2> uvs, List<float> lightLevels, List<uint> indices, uint startIndex,
            Vector3 blockWorldPos, Faces face, BlockType blockType)
        {
            // Get face vertices from static data
            var faceVerts = FaceDataRaw.rawVertexData[face];
            var uvCoords = GetUVsForBlockFace(blockType, face);

            // Get light level for this face
            float lightLevel = LightingConstants.CurrentLightLevels[face];

            // Add transformed vertices
            foreach (var vert in faceVerts)
            {
                verts.Add(vert + blockWorldPos);
            }

            // Add UV coordinates
            uvs.AddRange(uvCoords);

            // Add light levels (same light level for all 4 vertices of this face)
            for (int i = 0; i < 4; i++)
            {
                lightLevels.Add(lightLevel);
            }

            // Add indices for this face (two triangles)
            indices.Add(startIndex + 0);
            indices.Add(startIndex + 1);
            indices.Add(startIndex + 2);
            indices.Add(startIndex + 2);
            indices.Add(startIndex + 3);
            indices.Add(startIndex + 0);
        }

        private List<Vector2> GetUVsForBlockFace(BlockType blockType, Faces face)
        {
            if (!TextureData.blockTypeUVCoord.ContainsKey(blockType))
                return new List<Vector2> { Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero };

            var coords = TextureData.blockTypeUVCoord[blockType][face];
            return new List<Vector2>
            {
                new Vector2((coords.X + 1f) / 16f, (coords.Y + 1f) / 16f),
                new Vector2(coords.X / 16f, (coords.Y + 1f) / 16f),
                new Vector2(coords.X / 16f, coords.Y / 16f),
                new Vector2((coords.X + 1f) / 16f, coords.Y / 16f),
            };
        }

        private void CreateOpenGLObjects(List<Vector3> verts, List<Vector2> uvs, List<float> lightLevels, List<uint> indices)
        {
            chunkVAO = new VAO();
            chunkVAO.Bind();

            // Vertex positions (location 0)
            chunkVertexVBO = new VBO(verts);
            chunkVertexVBO.Bind();
            chunkVAO.LinkToVAO(0, 3, chunkVertexVBO);

            // Texture coordinates (location 1)
            chunkUVVBO = new VBO(uvs);
            chunkUVVBO.Bind();
            chunkVAO.LinkToVAO(1, 2, chunkUVVBO);

            // Light levels (location 2)
            chunkLightVBO = new VBO(lightLevels);
            chunkLightVBO.Bind();
            chunkVAO.LinkToVAO(2, 1, chunkLightVBO); // 1 component per vertex (float)

            chunkIBO = new IBO(indices);
        }

        public void Render(ShaderProgram program)
        {
            if (isEmpty || indexCount == 0) return;

            program.Bind();
            chunkVAO.Bind();
            chunkIBO.Bind();
            sharedTexture.Bind();
            GL.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedInt, 0);
        }

        public void Delete()
        {
            chunkVAO?.Delete();
            chunkVertexVBO?.Delete();
            chunkUVVBO?.Delete();
            chunkLightVBO?.Delete(); // Don't forget to delete the lighting VBO
            chunkIBO?.Delete();

            // Don't delete shared texture here - it's shared among all chunks
        }

        // Helper method to get chunk coordinate from world position
        public static Vector3i WorldToChunkCoord(Vector3 worldPos)
        {
            return new Vector3i(
                (int)Math.Floor(worldPos.X / SIZE),
                (int)Math.Floor(worldPos.Y / SIZE),
                (int)Math.Floor(worldPos.Z / SIZE)
            );
        }

        // Check if this chunk should be loaded based on distance from player
        public bool ShouldLoad(Vector3 playerPos, int renderDistance)
        {
            Vector3i playerChunk = WorldToChunkCoord(playerPos);
            int dx = Math.Abs(chunkCoord.X - playerChunk.X);
            int dz = Math.Abs(chunkCoord.Z - playerChunk.Z);

            // Check horizontal distance and vertical bounds
            bool withinHorizontalDistance = dx <= renderDistance && dz <= renderDistance;
            bool withinVerticalBounds = chunkCoord.Y >= 0 && chunkCoord.Y <= 11;

            return withinHorizontalDistance && withinVerticalBounds;
        }

        // Clean up shared resources when application closes
        public static void CleanupSharedResources()
        {
            sharedTexture?.Delete();
            textureLoaded = false;
        }
    }
    public class ChunkManager
    {
        private Dictionary<Vector3i, Chunk> loadedChunks = new Dictionary<Vector3i, Chunk>();
        // After loadedChunks[coord] = chunk;
        
        Vector3i[] neighborOffsets = new Vector3i[]
{
    new Vector3i(1, 0, 0), new Vector3i(-1, 0, 0),
    new Vector3i(0, 1, 0), new Vector3i(0, -1, 0),
    new Vector3i(0, 0, 1), new Vector3i(0, 0, -1)
};

        private int renderDistance = 4;
        private const int MAX_VERTICAL_CHUNKS = 12;
        private const int MIN_Y_CHUNK = 0;
        private const int MAX_Y_CHUNK = MAX_VERTICAL_CHUNKS - 1;

        // Limit the number of chunks that can be loaded at once
        private const int MAX_LOADED_CHUNKS = 200; // Adjust based on your needs

        public void UpdateChunks(Vector3 playerPos)
        {
            Vector3i playerChunk = Chunk.WorldToChunkCoord(playerPos);
            GenerateTreesForLoadedChunks();
            // Calculate which chunks should be loaded
            var chunksToLoad = new List<Vector3i>();
            foreach (var coord in chunksToLoad)
            {
                if (loadedChunks.Count >= MAX_LOADED_CHUNKS)
                    break;

                var chunk = new Chunk(coord);
                if (!chunk.isEmpty)
                {

                    loadedChunks[coord] = chunk;

                    // Rebuild neighbors when a new chunk is loaded
                    foreach (var offset in neighborOffsets)
                    {
                        Vector3i neighborCoord = coord + offset;
                        if (loadedChunks.TryGetValue(neighborCoord, out var neighborChunk))
                        {
                            neighborChunk.needsRebuild = true;
                            neighborChunk.BuildMesh();
                        }

                    }

                }
                else
                {
                    chunk.Delete(); // Clean up empty chunks immediately
                }
            }
            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    for (int y = MIN_Y_CHUNK; y <= MAX_Y_CHUNK; y++)
                    {
                        Vector3i chunkCoord = new Vector3i(playerChunk.X + x, y, playerChunk.Z + z);

                        if (!loadedChunks.ContainsKey(chunkCoord))
                        {
                            chunksToLoad.Add(chunkCoord);
                        }
                    }
                }
                foreach (var coord in chunksToLoad)
                {
                    if (loadedChunks.ContainsKey(coord))
                    {
                        GenerateTreesForChunk(coord);
                    }
                }
            }

            // Load new chunks (but don't exceed maximum)
            int chunksLoaded = 0;
            foreach (var coord in chunksToLoad)
            {
                if (loadedChunks.Count >= MAX_LOADED_CHUNKS)
                    break;

                var chunk = new Chunk(coord);
                if (!chunk.isEmpty)
                {
                    loadedChunks[coord] = chunk;
                    chunksLoaded++;
                }
                else
                {
                    chunk.Delete(); // Clean up empty chunks immediately
                }
            }

            // Unload distant chunks
            var chunksToRemove = new List<Vector3i>();
            foreach (var kvp in loadedChunks)
            {
                Vector3i chunkCoord = kvp.Key;

                bool tooFarHorizontally = !ShouldLoadHorizontally(chunkCoord, playerChunk, renderDistance + 1);
                bool outsideVerticalBounds = chunkCoord.Y < MIN_Y_CHUNK || chunkCoord.Y > MAX_Y_CHUNK;

                if (tooFarHorizontally || outsideVerticalBounds)
                {
                    kvp.Value.Delete();
                    chunksToRemove.Add(kvp.Key);
                }
            }

            foreach (var coord in chunksToRemove)
            {
                loadedChunks.Remove(coord);
            }

            // Force garbage collection periodically when chunks are unloaded
            if (chunksToRemove.Count > 0)
            {
                GC.Collect();
            }
        }
        private void GenerateTreesForLoadedChunks()
        {
            foreach (var kvp in loadedChunks)
            {
                // Only generate trees for surface chunks (y=0)
                if (kvp.Key.Y == 0 && !chunksWithTreesGenerated.ContainsKey(kvp.Key))
                {
                    GenerateTreesForChunk(kvp.Key);
                    chunksWithTreesGenerated[kvp.Key] = true;
                    kvp.Value.needsRebuild = true;
                }
            }
        }
        private Dictionary<Vector3i, bool> chunksWithTreesGenerated = new Dictionary<Vector3i, bool>();
        private void GenerateTreesForChunk(Vector3i chunkCoord)
        {
            var chunk = loadedChunks[chunkCoord];
            Random random = new Random(chunkCoord.X * 1000 + chunkCoord.Z);

            // Find grass blocks where trees might grow
            for (int x = 0; x < Chunk.SIZE; x++)
            {
                for (int z = 0; z < Chunk.SIZE; z++)
                {
                    int worldX = chunkCoord.X * Chunk.SIZE + x;
                    int worldZ = chunkCoord.Z * Chunk.SIZE + z;

                    // Check if this is a potential tree location
                    if (worldX % 4 == 0 && worldZ % 4 == 0 && random.Next(0, 100) < 10)
                    {
                        // Find the top non-air block in this column
                        for (int y = Chunk.SIZE - 1; y >= 0; y--)
                        {
                            if (chunk.chunkBlocks[x, y, z] == BlockType.GRASS)
                            {
                                int worldY = chunkCoord.Y * Chunk.SIZE + y;
                                GenerateTree(worldX, worldY + 1, worldZ, random);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void GenerateTree(int worldX, int baseY, int worldZ, Random random)
        {
            int treeHeight = random.Next(4, 6);
            int leavesRadius = 2;

            // Generate trunk
            for (int y = 0; y < treeHeight; y++)
            {
                SetBlockInAnyChunk(worldX, baseY + y, worldZ, BlockType.OakLog);
            }

            // Generate leaves
            int leavesStartY = baseY + treeHeight - 2;
            for (int ly = -leavesRadius; ly <= leavesRadius; ly++)
            {
                for (int lx = -leavesRadius; lx <= leavesRadius; lx++)
                {
                    for (int lz = -leavesRadius; lz <= leavesRadius; lz++)
                    {
                        // Skip full corners for better shape
                        if (Math.Abs(lx) == leavesRadius &&
                            Math.Abs(ly) == leavesRadius &&
                            Math.Abs(lz) == leavesRadius) continue;

                        int leafX = worldX + lx;
                        int leafY = leavesStartY + ly;
                        int leafZ = worldZ + lz;

                        // Only replace air or leaves
                        var current = GetBlockInAnyChunk(leafX, leafY, leafZ);
                        if (current == BlockType.AIR || current == BlockType.OAK_LEAVES)
                        {
                            SetBlockInAnyChunk(leafX, leafY, leafZ, BlockType.OAK_LEAVES);
                        }
                    }
                }
            }
        }

        private BlockType GetBlockInAnyChunk(int worldX, int worldY, int worldZ)
        {
            Vector3i chunkCoord = Chunk.WorldToChunkCoord(new Vector3(worldX, worldY, worldZ));
            if (loadedChunks.TryGetValue(chunkCoord, out Chunk chunk))
            {
                int x = worldX - chunkCoord.X * Chunk.SIZE;
                int y = worldY - chunkCoord.Y * Chunk.SIZE;
                int z = worldZ - chunkCoord.Z * Chunk.SIZE;

                if (x >= 0 && x < Chunk.SIZE &&
                    y >= 0 && y < Chunk.SIZE &&
                    z >= 0 && z < Chunk.SIZE)
                {
                    return chunk.chunkBlocks[x, y, z];
                }
            }
            return BlockType.AIR;
        }

        private void SetBlockInAnyChunk(int worldX, int worldY, int worldZ, BlockType type)
        {
            Vector3i chunkCoord = Chunk.WorldToChunkCoord(new Vector3(worldX, worldY, worldZ));
            if (loadedChunks.TryGetValue(chunkCoord, out Chunk chunk))
            {
                int x = worldX - chunkCoord.X * Chunk.SIZE;
                int y = worldY - chunkCoord.Y * Chunk.SIZE;
                int z = worldZ - chunkCoord.Z * Chunk.SIZE;

                if (x >= 0 && x < Chunk.SIZE &&
                    y >= 0 && y < Chunk.SIZE &&
                    z >= 0 && z < Chunk.SIZE)
                {
                    chunk.chunkBlocks[x, y, z] = type;
                    chunk.isEmpty = false;
                    chunk.needsRebuild = true;
                }
            }
        }
        private bool ShouldLoadHorizontally(Vector3i chunkCoord, Vector3i playerChunk, int maxDistance)
        {
            int dx = Math.Abs(chunkCoord.X - playerChunk.X);
            int dz = Math.Abs(chunkCoord.Z - playerChunk.Z);
            return dx <= maxDistance && dz <= maxDistance;
        }

        public void RenderChunks(ShaderProgram program)
        {
            foreach (var chunk in loadedChunks.Values)
            {
                chunk.Render(program);
            }
        }

        public void DeleteAll()
        {
            foreach (var chunk in loadedChunks.Values)
            {
                chunk.Delete();
            }
            loadedChunks.Clear();

            // Clean up shared resources
            Chunk.CleanupSharedResources();

            // Force garbage collection
            GC.Collect();
        }

        public int RenderDistance
        {
            get { return renderDistance; }
            set { renderDistance = Math.Max(1, value); }
        }

        public int LoadedChunkCount => loadedChunks.Count;

        // Get memory usage info for debugging
        public string GetMemoryInfo()
        {
            long memory = GC.GetTotalMemory(false);
            return $"Chunks: {LoadedChunkCount}, Memory: {memory / (1024 * 1024)}MB";
        }
    }

public struct Frustum
    {
        public Vector4[] planes; // 6 planes: left, right, bottom, top, near, far

        public Frustum(Matrix4 viewProjectionMatrix)
        {
            planes = new Vector4[6];

            // Extract frustum planes from view-projection matrix
            // Left plane
            planes[0] = new Vector4(
                viewProjectionMatrix.M41 + viewProjectionMatrix.M11,
                viewProjectionMatrix.M42 + viewProjectionMatrix.M12,
                viewProjectionMatrix.M43 + viewProjectionMatrix.M13,
                viewProjectionMatrix.M44 + viewProjectionMatrix.M14
            );

            // Right plane
            planes[1] = new Vector4(
                viewProjectionMatrix.M41 - viewProjectionMatrix.M11,
                viewProjectionMatrix.M42 - viewProjectionMatrix.M12,
                viewProjectionMatrix.M43 - viewProjectionMatrix.M13,
                viewProjectionMatrix.M44 - viewProjectionMatrix.M14
            );

            // Bottom plane
            planes[2] = new Vector4(
                viewProjectionMatrix.M41 + viewProjectionMatrix.M21,
                viewProjectionMatrix.M42 + viewProjectionMatrix.M22,
                viewProjectionMatrix.M43 + viewProjectionMatrix.M23,
                viewProjectionMatrix.M44 + viewProjectionMatrix.M24
            );

            // Top plane
            planes[3] = new Vector4(
                viewProjectionMatrix.M41 - viewProjectionMatrix.M21,
                viewProjectionMatrix.M42 - viewProjectionMatrix.M22,
                viewProjectionMatrix.M43 - viewProjectionMatrix.M23,
                viewProjectionMatrix.M44 - viewProjectionMatrix.M24
            );

            // Near plane
            planes[4] = new Vector4(
                viewProjectionMatrix.M41 + viewProjectionMatrix.M31,
                viewProjectionMatrix.M42 + viewProjectionMatrix.M32,
                viewProjectionMatrix.M43 + viewProjectionMatrix.M33,
                viewProjectionMatrix.M44 + viewProjectionMatrix.M34
            );

            // Far plane
            planes[5] = new Vector4(
                viewProjectionMatrix.M41 - viewProjectionMatrix.M31,
                viewProjectionMatrix.M42 - viewProjectionMatrix.M32,
                viewProjectionMatrix.M43 - viewProjectionMatrix.M33,
                viewProjectionMatrix.M44 - viewProjectionMatrix.M34
            );

            // Normalize all planes
            for (int i = 0; i < 6; i++)
            {
                float length = MathF.Sqrt(planes[i].X * planes[i].X + planes[i].Y * planes[i].Y + planes[i].Z * planes[i].Z);
                if (length > 0)
                {
                    planes[i] /= length;
                }
            }
        }

        public bool IsBoxInFrustum(Vector3 min, Vector3 max)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector3 positive = new Vector3(
                    planes[i].X >= 0 ? max.X : min.X,
                    planes[i].Y >= 0 ? max.Y : min.Y,
                    planes[i].Z >= 0 ? max.Z : min.Z
                );

                if (Vector3.Dot(positive, planes[i].Xyz) + planes[i].W < 0)
                {
                    return false; // Box is completely outside this plane
                }
            }
            return true; // Box is at least partially inside frustum
        }
    }

    // Axis-Aligned Bounding Box for chunks
    public struct AABB
    {
        public Vector3 min;
        public Vector3 max;

        public AABB(Vector3 center, float size)
        {
            float halfSize = size * 0.5f;
            min = center - new Vector3(halfSize);
            max = center + new Vector3(halfSize);
        }

        public Vector3 Center => (min + max) * 0.5f;
        public Vector3 Size => max - min;

        public bool Intersects(AABB other)
        {
            return min.X <= other.max.X && max.X >= other.min.X &&
                   min.Y <= other.max.Y && max.Y >= other.min.Y &&
                   min.Z <= other.max.Z && max.Z >= other.min.Z;
        }

        public bool Contains(Vector3 point)
        {
            return point.X >= min.X && point.X <= max.X &&
                   point.Y >= min.Y && point.Y <= max.Y &&
                   point.Z >= min.Z && point.Z <= max.Z;
        }
    }
}