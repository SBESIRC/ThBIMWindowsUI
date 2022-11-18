using System;
using Xbim.Geom;
using Xbim.Common.Geometry;

namespace XbimXplorer.Geometry
{
    public class ThXbimMeshBuilder
    {
        private IGeometryStoreReader Reader { get; set; }

        public ThXbimMeshBuilder(IGeometryStoreReader reader)
        {
            Reader = reader;
        }

        public XbimMesher ToMesh(XbimShapeInstance shapeInstance)
        {
            IXbimShapeGeometryData shapeGeom = Reader.ShapeGeometry(shapeInstance.ShapeGeometryLabel);
            if (shapeGeom.Format != (byte)XbimGeometryType.PolyhedronBinary)
            {
                throw new NotSupportedException();
            }

            var xbimMesher = new XbimMesher();
            xbimMesher.AddMesh(shapeGeom.ShapeData);
            return xbimMesher;
        }
    }
}
