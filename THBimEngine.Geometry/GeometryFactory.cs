using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.IO.Memory;
using THBimEngine.Domain;
using Xbim.Geometry.Engine.Interop;
using System.IO;
using Xbim.Common.XbimExtensions;
using Xbim.Ifc4.Interfaces;

namespace THBimEngine.Geometry
{
    public class GeometryFactory
    {
        MemoryModel memoryModel;
        IXbimGeometryEngine geomEngine;
        IfcSchemaVersion ifcVersion;
        public GeometryFactory(IfcSchemaVersion ifcVersion, XbimStoreType storageType = XbimStoreType.InMemoryModel) 
        {
            this.ifcVersion = ifcVersion;
            var ef = GetFactory(ifcVersion);
            memoryModel = new MemoryModel(ef);
            geomEngine = new XbimGeometryEngine();
        }
        private IEntityFactory GetFactory(IfcSchemaVersion type)
        {
            switch (type)
            {
                case IfcSchemaVersion.Ifc4:
                    return new Xbim.Ifc4.EntityFactoryIfc4();
                case IfcSchemaVersion.Ifc4x1:
                    return new Xbim.Ifc4.EntityFactoryIfc4x1();
                case IfcSchemaVersion.Ifc2X3:
                    return new Xbim.Ifc2x3.EntityFactoryIfc2x3();
                case IfcSchemaVersion.Cobie2X4:
                case IfcSchemaVersion.Unsupported:
                default:
                    throw new NotSupportedException("Schema '" + type + "' is not supported");
            }
        }

        public XbimShapeGeometry GetShapeGeometry(GeometryStretch geometryStretch,XbimVector3D moveVector) 
        {
            using (var txn = memoryModel.BeginTransaction("Create Shape Geometry"))
            {
                IXbimSolid geoSolid = null;
                if (ifcVersion == IfcSchemaVersion.Ifc2X3)
                {
                    geoSolid = GetXBimSolid2x3(geometryStretch, moveVector);
                }
                else 
                {
                    geoSolid = GetXBimSolid4(geometryStretch, moveVector);
                }
                if (null == geoSolid || geoSolid.SurfaceArea<10)
                    return null;
                XbimShapeGeometry shapeGeometry = geomEngine.CreateShapeGeometry(geoSolid, 0.001, 0.0001,0.5,XbimGeometryType.PolyhedronBinary);
                using (var ms = new MemoryStream((shapeGeometry as IXbimShapeGeometryData).ShapeData))
                {
                    var testData = ms.ToArray();
                    var br = new BinaryReader(ms);
                    var tr = br.ReadShapeTriangulation();
                }
                   
                txn.Commit();
                return shapeGeometry;
            }
                
        }
        public IXbimSolid GetXBimSolid2x3(GeometryStretch geometryStretch, XbimVector3D moveVector) 
        {
            Xbim.Ifc2x3.ProfileResource.IfcProfileDef profile = null;
            XbimPoint3D planeOrigin = geometryStretch.Origin + moveVector;
            if (geometryStretch.OutLine != null && geometryStretch.OutLine.Count > 0)
            {
                profile = ThIFC2x3GeExtension.ToIfcArbitraryClosedProfileDef(memoryModel, geometryStretch.OutLine.First());
            }
            else
            {
                if (Math.Abs(geometryStretch.XAxisLength) > 1 && Math.Abs(geometryStretch.YAxisLength) > 1)
                    profile = ThIFC2x3GeExtension.ToIfcRectangleProfileDef(memoryModel, planeOrigin, geometryStretch.XAxisLength, geometryStretch.YAxisLength);
            }
            if (profile == null)
                return null;
            var solid = memoryModel.ToIfcExtrudedAreaSolid(profile, geometryStretch.ZAxis, geometryStretch.ZAxisLength);
            var geoSolid = geomEngine.CreateSolid(solid);
            return geoSolid;
        }
        public IXbimSolid GetXBimSolid4(GeometryStretch geometryStretch, XbimVector3D moveVector)
        {
            Xbim.Ifc4.ProfileResource.IfcProfileDef profile = null;
            XbimPoint3D planeOrigin = geometryStretch.Origin + moveVector;
            if (geometryStretch.OutLine != null && geometryStretch.OutLine.Count > 0)
            {
                profile = ThIFC4GeExtension.ToIfcArbitraryClosedProfileDef(memoryModel, geometryStretch.OutLine.First());
            }
            else
            {
                if (Math.Abs(geometryStretch.XAxisLength) > 1 && Math.Abs(geometryStretch.YAxisLength) > 1)
                    profile = ThIFC4GeExtension.ToIfcRectangleProfileDef(memoryModel, planeOrigin, geometryStretch.XAxisLength, geometryStretch.YAxisLength);
            }
            if (profile == null)
                return null;
            var solid = memoryModel.ToIfcExtrudedAreaSolid(profile, geometryStretch.ZAxis, geometryStretch.ZAxisLength);
            var geoSolid = geomEngine.CreateSolid(solid);
            return geoSolid;
        }
    }
}
