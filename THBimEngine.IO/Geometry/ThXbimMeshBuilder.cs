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
            // Reference:
            //  https://github.com/xBimTeam/XbimGltf/blob/master/Xbim.GLTF.IO/Builder.cs
            var shapeGeom = Reader.ShapeGeometryOfInstance(shapeInstance);
            if (shapeGeom.Format != XbimGeometryType.PolyhedronBinary)
            {
                throw new NotSupportedException();
            }

            var xbimMesher = new XbimMesher();
            xbimMesher.AddMesh((shapeGeom as IXbimShapeGeometryData).ShapeData);
            return xbimMesher;
        }
    }
}
