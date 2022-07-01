using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace THBimEngine.Presention
{
	public struct TWindow
    {
    }
    public static class ExampleScene
    {
        private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        [DllImport("ifc-render-engine.dll")]//, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.None, ExactSpelling = false)]
        public static unsafe extern void ifcre_init(TWindow* wndPtr);

        [DllImport("ifc-render-engine.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.None, ExactSpelling = false)]
        public static extern void ifcre_run();

        [DllImport("ifc-render-engine.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.None, ExactSpelling = false)]
        public static extern void ifcre_set_config(string key, string value);

        public static unsafe void Init(IntPtr wndPtr, int width, int height,string ifcPath)
        {
            ifcre_set_config("width", width.ToString());
            ifcre_set_config("height", height.ToString());
            ifcre_set_config("model_type", "ifc");
            ifcre_set_config("use_transparency", "true");
            ifcre_set_config("file", ifcPath); //".\\ff.ifc");
            ifcre_set_config("render_api", "opengl");
            //ifcre_set_config("render_api", "vulkan");
            TWindow* ptrToWnd = (TWindow*)wndPtr.ToPointer();
            ifcre_init(ptrToWnd);
        }
        public static void Render()
        {
            //render by c++ code
            ifcre_run();
        }
        
    }
}
