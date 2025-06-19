using System;
using System.Runtime.CompilerServices;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OpenGLMinecraftClone.Graphics
{
    public class ImGuiController : IDisposable
    {
        private bool _frameBegun;
        private int _vertexArray;
        private int _vertexBuffer;
        private int _vertexBufferSize;
        private int _indexBuffer;
        private int _indexBufferSize;
        private Texture _fontTexture;
        private ShaderProgram _shader;

        public ImGuiController(int width, int height)
        {
            var context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);

            var io = ImGui.GetIO();
            io.Fonts.AddFontDefault();
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

            CreateDeviceResources();
            SetKeyMappings();

            SetPerFrameImGuiData(1f / 60f, width, height);
        }

        private void CreateDeviceResources()
        {
            _vertexBufferSize = 10000;
            _indexBufferSize = 2000;

            _vertexArray = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArray);

            _vertexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            _indexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            _shader = new ShaderProgram("IMGUI.frag", "IMGUI.vert");

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 20, 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 20, 8);
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, 20, 16);

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        private static void SetKeyMappings()
        {
            var io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard; // Enable Keyboard Controls
           
        }

        public void WindowResized(int width, int height)
        {
            ImGui.GetIO().DisplaySize = new System.Numerics.Vector2(width, height);
        }

        public void Update(GameWindow window, float deltaSeconds)
        {
            if (_frameBegun)
                ImGui.Render();

            SetPerFrameImGuiData(deltaSeconds, window.ClientSize.X, window.ClientSize.Y);
            UpdateImGuiInput(window);

            _frameBegun = true;
            ImGui.NewFrame();
        }

        private void SetPerFrameImGuiData(float deltaSeconds, int width, int height)
        {
            var io = ImGui.GetIO();
            io.DisplaySize = new System.Numerics.Vector2(width, height);
            io.DisplayFramebufferScale = System.Numerics.Vector2.One;
            io.DeltaTime = deltaSeconds;
        }

        private void UpdateImGuiInput(GameWindow window)
        {
            var io = ImGui.GetIO();

            var mouseState = window.MouseState;
            var keyboardState = window.KeyboardState;

            io.MousePos = new System.Numerics.Vector2(mouseState.X, mouseState.Y);
            io.MouseDown[0] = mouseState[MouseButton.Left];
            io.MouseDown[1] = mouseState[MouseButton.Right];
            io.MouseDown[2] = mouseState[MouseButton.Middle];

            io.KeyCtrl = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
            io.KeyAlt = keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt);
            io.KeyShift = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
            io.KeySuper = keyboardState.IsKeyDown(Keys.LeftSuper) || keyboardState.IsKeyDown(Keys.RightSuper);
        }

        public void Render()
        {
            if (!_frameBegun)
                return;

            _frameBegun = false;
            ImGui.Render();
            RenderImDrawData(ImGui.GetDrawData());
        }

        private void RenderImDrawData(ImDrawDataPtr drawData)
        {
            if (drawData.CmdListsCount == 0)
                return;

            GL.Enable(EnableCap.Blend);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.ScissorTest);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            var io = ImGui.GetIO();
            drawData.ScaleClipRects(io.DisplayFramebufferScale);

            GL.BindVertexArray(_vertexArray);
            _shader.Bind();

            Matrix4 orthoProjection = Matrix4.CreateOrthographicOffCenter(
                0.0f,
                io.DisplaySize.X,
                io.DisplaySize.Y,
                0.0f,
                -1.0f,
                1.0f);
            for (int i = 0; i < drawData.CmdListsCount; i++)
            {
                var cmdList = drawData.CmdLists[i];

                int vertexSize = cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
                if (vertexSize > _vertexBufferSize)
                {
                    _vertexBufferSize = (int)(vertexSize * 1.5f);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
                    GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                }

                int indexSize = cmdList.IdxBuffer.Size * sizeof(ushort);
                if (indexSize > _indexBufferSize)
                {
                    _indexBufferSize = (int)(indexSize * 1.5f);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
                    GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                }

                GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
                GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertexSize, cmdList.VtxBuffer.Data);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
                GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, indexSize, cmdList.IdxBuffer.Data);

                int vtxOffset = 0;
                int idxOffset = 0;

                for (int j = 0; j < cmdList.CmdBuffer.Size; j++)
                {
                    var cmd = cmdList.CmdBuffer[j];
                    if (cmd.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        GL.ActiveTexture(TextureUnit.Texture0);
                        GL.BindTexture(TextureTarget.Texture2D, (int)cmd.TextureId);
                        GL.Scissor(
                            (int)cmd.ClipRect.X,
                            (int)(io.DisplaySize.Y - cmd.ClipRect.W),
                            (int)(cmd.ClipRect.Z - cmd.ClipRect.X),
                            (int)(cmd.ClipRect.W - cmd.ClipRect.Y));

                        GL.DrawElementsBaseVertex(
                            PrimitiveType.Triangles,
                            (int)cmd.ElemCount,
                            DrawElementsType.UnsignedShort,
                            (IntPtr)(idxOffset * sizeof(ushort)),
                            vtxOffset);
                    }

                    idxOffset += (int)cmd.ElemCount;
                }
            }

            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.UseProgram(0);
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.ScissorTest);
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(_vertexArray);
            GL.DeleteBuffer(_vertexBuffer);
            GL.DeleteBuffer(_indexBuffer);
            _shader.Delete();
        }
    }
}