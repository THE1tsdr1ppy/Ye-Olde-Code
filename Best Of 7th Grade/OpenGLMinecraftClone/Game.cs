using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Graphics;
using OpenTK.Windowing.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenGLMinecraftClone;
using OpenGLMinecraftClone.World;
using OpenGLMinecraftClone.Graphics;
using System.Runtime.InteropServices;
using OpenGLMinecraftClone.Source.Player;

namespace OpenGLMinecraftClone
{
    // Game class that inherets from the Game Window Class
    internal class Game : GameWindow
    {
        // Replace the old chunk system with the new chunk manager
        private ChunkManager chunkManager;
        private bool chunksLoaded = false;
        private IGraphicsContext sharedContext;
        ShaderProgram program;
        private MainThreadInvoker _invoker;
        // camera
        CameraExtended camera;


        // transformation variables
        float yRot = 0f;

        // width and height of screen
        int width, height;

        private Vector3 fogColor = new Vector3(0.5f, 0.5f, 0.7f); // Light blue for sky
        private float fogDensity = .025f;
        private float fogStart = 50f;
        private float fogEnd = 100f;

        public Game(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            this.width = width;
            this.height = height;
            CenterWindow(new Vector2i(width, height));
            _invoker = new MainThreadInvoker(this);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            this.width = e.Width;
            this.height = e.Height;
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            // Initialize the new chunk manager instead of old chunk list
            chunkManager = new ChunkManager();
            program = new ShaderProgram("Default.vert", "Default.frag");

            GL.Enable(EnableCap.DepthTest);
            GL.FrontFace(FrontFaceDirection.Cw);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            camera = new CameraExtended(width, height, new Vector3(0, 20, 0), chunkManager);
            CursorState = CursorState.Grabbed;
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            // Clean up the chunk manager
            chunkManager.DeleteAll();
            program.Delete();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.ClearColor(fogColor.X, fogColor.Y, fogColor.Z, 1f); // Clear with fog color
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.DepthFunc(DepthFunction.Lequal); // Draw even if dep
            Matrix4 model = Matrix4.Identity;
            Matrix4 view = camera.GetViewMatrix();
            Matrix4 projection = camera.GetProjectionMatrix();

            // Render skybox first (with depth testing disabled)
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.DepthTest);

            // Then render chunks with the main shader
            program.Bind();

            int modelLocation = GL.GetUniformLocation(program.ID, "model");
            int viewLocation = GL.GetUniformLocation(program.ID, "view");
            int projectionLocation = GL.GetUniformLocation(program.ID, "projection");
            GL.UniformMatrix4(modelLocation, true, ref model);
            GL.UniformMatrix4(viewLocation, true, ref view);
            GL.UniformMatrix4(projectionLocation, true, ref projection);
            // Set fog uniforms
            int fogColorLoc = GL.GetUniformLocation(program.ID, "fogColor");
            int fogDensityLoc = GL.GetUniformLocation(program.ID, "fogDensity");
            int fogStartLoc = GL.GetUniformLocation(program.ID, "fogStart");
            int fogEndLoc = GL.GetUniformLocation(program.ID, "fogEnd");

            GL.Uniform3(fogColorLoc, fogColor);
            GL.Uniform1(fogDensityLoc, fogDensity);
            GL.Uniform1(fogStartLoc, fogStart);
            GL.Uniform1(fogEndLoc, fogEnd);
            // Render all loaded chunks with proper parameters
            chunkManager.RenderChunks(program);

            program.Unbind();

            program.Unbind();
            Context.SwapBuffers();
            base.OnRenderFrame(args);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            _invoker.Update(); // Process pending actions
            camera.Update(KeyboardState, MouseState, args);
            chunkManager.UpdateChunks(camera.Position); // `camera.Position` is from base class
            var input = KeyboardState;
            var mouse = MouseState;

            // --- Mining ---
            if (mouse.IsButtonDown(MouseButton.Left))
            {
                camera.MineTargetBlock();
            }

            // --- Placing (uses DIRT as example) ---
            if (mouse.IsButtonDown(MouseButton.Right))
            {
                camera.PlaceBlock(BlockType.DIRT);
            }
            // Update chunks based on camera position
            // camera.position is Vector3, which is what UpdateChunks expects
            chunkManager.UpdateChunks(camera.position);

            
        }
    }
}