
using OpenGLMinecraftClone.World;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGLMinecraftClone.World
{
    internal static class TextureData
    {
        public static Dictionary<BlockType, Dictionary<Faces, Vector2>> blockTypeUVCoord = new Dictionary<BlockType, Dictionary<Faces, Vector2>>()
        {
            {BlockType.DIRT, new Dictionary<Faces, Vector2>()
                {
                    {Faces.FRONT, new Vector2(2f, 15f) },
                    {Faces.LEFT, new Vector2(2f, 15f) },
                    {Faces.RIGHT, new Vector2(2f, 15f) },
                    {Faces.BACK, new Vector2(2f, 15f) },
                    {Faces.TOP, new Vector2(2f, 15f) },
                    {Faces.BOTTOM, new Vector2(2f, 15f) },
                }
            },
            {BlockType.GRASS, new Dictionary<Faces, Vector2>()
                {
                    {Faces.FRONT, new Vector2(3f, 15f) },
                    {Faces.LEFT, new Vector2(3f, 15f) },
                    {Faces.RIGHT, new Vector2(3f, 15f) },
                    {Faces.BACK, new Vector2(3f, 15f) },
                    {Faces.TOP, new Vector2(7f, 13f) },
                    {Faces.BOTTOM, new Vector2(2f, 15f) },
                }
            },
            {BlockType.STONE, new Dictionary<Faces, Vector2>()
                {
                    {Faces.FRONT, new Vector2(1f, 15f) },
                    {Faces.LEFT, new Vector2(1f, 15f) },
                    {Faces.RIGHT, new Vector2(1f, 15f) },
                    {Faces.BACK, new Vector2(1f, 15f) },
                    {Faces.TOP, new Vector2(1f, 15f) },
                    {Faces.BOTTOM, new Vector2(1f, 15f) },
                }
            },
            {BlockType.OakLog, new Dictionary<Faces, Vector2>()
                {
                    {Faces.FRONT, new Vector2(4f, 14f) },
                    {Faces.LEFT, new Vector2(4f, 14f) },
                    {Faces.RIGHT, new Vector2(4f, 14f) },
                    {Faces.BACK, new Vector2(4f, 14f) },
                    {Faces.TOP, new Vector2(5, 14f) },
                    {Faces.BOTTOM, new Vector2(5f, 14f) },
                }
            },
            {BlockType.OAK_LEAVES, new Dictionary<Faces, Vector2>()
                {
                    {Faces.FRONT, new Vector2(4f, 12f) },
                    {Faces.LEFT, new Vector2(4f, 12f) },
                    {Faces.RIGHT, new Vector2(4f, 12f) },
                    {Faces.BACK, new Vector2(4f, 12f) },
                    {Faces.TOP, new Vector2(4f, 12f) },
                    {Faces.BOTTOM, new Vector2(4f, 12f) },
                }
            },
            {BlockType.SAND, new Dictionary<Faces, Vector2>()
                {
                    {Faces.FRONT, new Vector2(2f, 14f) },
                    {Faces.LEFT, new Vector2(2f, 14f) },
                    {Faces.RIGHT, new Vector2(2f, 14f) },
                    {Faces.BACK, new Vector2(2f, 14f) },
                    {Faces.TOP, new Vector2(2f, 14f) },
                    {Faces.BOTTOM, new Vector2(2f, 14f) },
                }
            },
            {BlockType.BEDROCK, new Dictionary<Faces, Vector2>()
                {
                    {Faces.FRONT, new Vector2(1f, 14f) },
                    {Faces.LEFT, new Vector2(1f, 14f) },
                    {Faces.RIGHT, new Vector2(1f, 14f) },
                    {Faces.BACK, new Vector2(1f, 14f) },
                    {Faces.TOP, new Vector2(1f, 14f) },
                    {Faces.BOTTOM, new Vector2(1f, 14f) },
                }
            }
        };
    }
}