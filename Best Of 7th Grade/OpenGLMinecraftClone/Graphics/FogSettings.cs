using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGLMinecraftClone.Graphics
{
    public class FogSettings
    {
        public System.Numerics.Vector3 Color = new System.Numerics.Vector3(0.5f, 0.5f, 0.7f);
        public float Density = 0.01f;
        public float Start = 50f;
        public float End = 150f;
        public bool Enabled = true;
        public FogMode Mode = FogMode.Exponential;

        public enum FogMode
        {
            Linear,
            Exponential,
            ExponentialSquared
        }
    }
}
