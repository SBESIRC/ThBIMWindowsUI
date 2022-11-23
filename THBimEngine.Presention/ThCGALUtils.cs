using System;
using System.Runtime.InteropServices;

namespace THBimEngine.Presention
{
    public class ThCGALUtils
    {
        [DllImport("ThCGALUtils.dll", SetLastError = true)]
        public static extern void ThCGALMeshOBBFromSTLMesh(double[] vertices, int fCount, double[] result);
    }
}
