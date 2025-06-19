using OpenGLMinecraftClone.World;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
namespace OpenGLMinecraftClone.Source.Player
{
    internal class CameraExtended : Camera
    {
        private BlockDetection blockDetection;
        private Vector3 velocity = Vector3.Zero;
        private const float GRAVITY = -20.0f;
        private const float JUMP_FORCE = 8.0f;
        public Vector3 Position => position;
        public CameraExtended(float width, float height, Vector3 position, ChunkManager chunkManager)
            : base(width, height, position)
        {
            blockDetection = new BlockDetection(chunkManager);
        }

        public bool MineTargetBlock()
        {
            var target = blockDetection.RaycastBlock(position, Front);
            if (target.hit)
            {
                return blockDetection.MineBlock(target.blockPosition);
            }
            return false;
        }

        public bool PlaceBlock(BlockType blockType)
        {
            var target = blockDetection.RaycastBlock(position, Front);
            if (target.hit)
            {
                Vector3i placePosition = target.blockPosition + new Vector3i(
                    (int)MathF.Round(target.normal.X),
                    (int)MathF.Round(target.normal.Y),
                    (int)MathF.Round(target.normal.Z)
                );
                return blockDetection.PlaceBlock(placePosition, blockType);
            }
            return false;
        }

        public void Update(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            float speed = 10.0f;

            Vector3 inputMovement = Vector3.Zero;
            if (input.IsKeyDown(Keys.W))
                inputMovement += new Vector3(Front.X, 0, Front.Z).Normalized() * speed * (float)e.Time;
            if (input.IsKeyDown(Keys.S))
                inputMovement -= new Vector3(Front.X, 0, Front.Z).Normalized() * speed * (float)e.Time;
            if (input.IsKeyDown(Keys.A))
                inputMovement -= Vector3.Cross(new Vector3(Front.X, 0, Front.Z).Normalized(), Vector3.UnitY) * speed * (float)e.Time;
            if (input.IsKeyDown(Keys.D))
                inputMovement += Vector3.Cross(new Vector3(Front.X, 0, Front.Z).Normalized(), Vector3.UnitY) * speed * (float)e.Time;

            if (input.IsKeyDown(Keys.Space))
                inputMovement.Y += speed * (float)e.Time;

            if (input.IsKeyDown(Keys.LeftShift))
                inputMovement.Y -= speed * (float)e.Time;

            position += inputMovement;

            // Basic mouse look
            if (firstMove)
            {
                lastPos = new Vector2(mouse.X, mouse.Y);
                firstMove = false;
            }
            else
            {
                float deltaX = mouse.X - lastPos.X;
                float deltaY = mouse.Y - lastPos.Y;
                lastPos = new Vector2(mouse.X, mouse.Y);

                yaw += deltaX * SENSITIVITY/50;
                pitch -= deltaY * SENSITIVITY/50;
            }

            UpdateVectors();
        }
    }
}
