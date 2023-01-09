using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Geometry.Engine.Interop;
using ThBIMServer.Ifc2x3;

namespace THBimEngine.IO
{
    public class ThBimXbimSlabEngine
    {
        public IfcStore Model { get; private set; }
        public IXbimGeometryEngine Engine { get; private set; }
        public ThBimXbimSlabEngine()
        {
            Engine = new XbimGeometryEngine();
            Model = ThIFC2x3Factory.CreateMemoryModel();
        }
    }
}
