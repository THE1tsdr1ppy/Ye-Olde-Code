using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGLMinecraftClone
{
    internal class Camera
    {
        // CONSTANTS
        private float SPEED = 10f;
        private float SCREENWIDTH;
        private float SCREENHEIGHT;
        public float SENSITIVITY = 6;
        private float _fov = 60.0f;
        // position vars
        public Vector3 position;
        public Vector3 Front => front;
        Vector3 up = Vector3.UnitY;
        Vector3 front = -Vector3.UnitZ;
        Vector3 right = Vector3.UnitX;

        // --- view rotations ---
        public float pitch;
        public float yaw = -90.0f;

        public bool firstMove = true;
        public Vector2 lastPos;
        private Matrix4 _projectionMatrix;
        private bool _projectionDirty = true;

        public Camera(float width, float height, Vector3 position)
        {
            SCREENWIDTH = width;
            SCREENHEIGHT = height;
            this.position = position;
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(position, position + front, up);
        }
        public float CalculateViewSpaceDepth(Vector3 worldPosition)
        {
            Vector4 viewSpacePos = Matrix4.Transpose(GetViewMatrix()) * new Vector4(worldPosition, 1.0f);
            return viewSpacePos.Length;
        }
        public Matrix4 GetProjectionMatrix()
        {
            // Improved depth precision by adjusting near/far ratio
            return Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45.0f),
                SCREENWIDTH / SCREENHEIGHT,
                0.01f,  // Closer near plane
                2000.0f // Further far plane
            );
        }

        public void UpdateVectors()
        {
            if (pitch > 89.0f)
            {
                pitch = 89.0f;
            }
            if (pitch < -89.0f)
            {
                pitch = -89.0f;
            }

            front.X = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Cos(MathHelper.DegreesToRadians(yaw));
            front.Y = MathF.Sin(MathHelper.DegreesToRadians(pitch));
            front.Z = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Sin(MathHelper.DegreesToRadians(yaw));

            front = Vector3.Normalize(front);

            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            up = Vector3.Normalize(Vector3.Cross(right, front));
        }

        public void InputController(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            if (input.IsKeyDown(Keys.W))
            {
                position += front * SPEED * (float)e.Time;
            }
            if (input.IsKeyDown(Keys.A))
            {
                position -= right * SPEED * (float)e.Time;
            }
            if (input.IsKeyDown(Keys.S))
            {
                position -= front * SPEED * (float)e.Time;
            }
            if (input.IsKeyDown(Keys.D))
            {
                position += right * SPEED * (float)e.Time;
            }

            if (input.IsKeyDown(Keys.Space))
            {
                position.Y += SPEED * (float)e.Time;
            }
            if (input.IsKeyDown(Keys.LeftShift))
            {
                position.Y -= SPEED * (float)e.Time;
            }

            if (firstMove)
            {
                lastPos = new Vector2(mouse.X, mouse.Y);
                firstMove = false;
            }
            else
            {
                var deltaX = mouse.X - lastPos.X;
                var deltaY = mouse.Y - lastPos.Y;
                lastPos = new Vector2(mouse.X, mouse.Y);

                yaw += deltaX * SENSITIVITY * (float)e.Time;
                pitch -= deltaY * SENSITIVITY * (float)e.Time;
            }
            UpdateVectors();
        }
        public void UpdateProjectionMatrix()
        {
            _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(_fov),
                SCREENWIDTH / SCREENHEIGHT,
                0.01f,
                2000.0f);
            _projectionDirty = false;
        }
        public void Update(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            InputController(input, mouse, e);
        }

        // Method to update screen dimensions when window is resized
        public void UpdateScreenSize(float width, float height)
        {
            SCREENWIDTH = width;
            SCREENHEIGHT = height;
        }
    }
}