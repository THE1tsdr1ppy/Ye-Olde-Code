using OpenTK.Mathematics;
using OpenTK.Windowing.Common.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using System.Drawing.Imaging;
using System;



namespace OpenGLMinecraftClone.Graphics

{

    internal class UIManager
    {
        public static int LoadTexture(string path)
        {
            if (!System.IO.File.Exists(path))
                throw new Exception("Texture file not found: " + path);

            using (var image = System.Drawing.Image.FromFile(path) as System.Drawing.Bitmap)
            {
                int texID = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, texID);

                var data = image.LockBits(
                    new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                    image.Width, image.Height, 0,   
                    OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

                image.UnlockBits(data);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                GL.BindTexture(TextureTarget.Texture2D, 0);
                return texID;
            }
        }


    }
}
