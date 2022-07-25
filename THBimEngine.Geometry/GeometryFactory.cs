using System;
using System.Collections.Generic;
using System.Linq;
using THBimEngine.Domain;
using THBimEngine.Domain.GeometryModel;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Common.Step21;
using Xbim.Geometry.Engine.Interop;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Memory;

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
        
        public XbimShapeGeometry GetShapeGeometry(GeometryStretch geometryStretch,XbimVector3D moveVector,out IXbimSolid geoSolid) 
        {
            geoSolid = null;
            using (var txn = memoryModel.BeginTransaction("Create Shape Geometry"))
            {
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
                txn.Commit();
                return shapeGeometry;
            }
        }
        public XbimShapeGeometry GetShapeGeometry(GeometryStretch geometryStretch, XbimVector3D moveVector,List<IXbimSolid> openingSolids)
        {
            XbimShapeGeometry shapeGeometry = null;
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
                if (null == geoSolid || geoSolid.SurfaceArea < 10)
                    return shapeGeometry;
                IXbimSolidSet solid = null;
                if (null != openingSolids && openingSolids.Count > 0) 
                {
                    foreach (var item in openingSolids) 
                    {
                        if (solid == null)
                            solid = geoSolid.Cut(item,1);
                        else
                            solid = solid.Cut(item, 1);
                    }
                }
                if (solid != null)
                    shapeGeometry = geomEngine.CreateShapeGeometry(solid, 0.001, 0.0001, 0.5, XbimGeometryType.PolyhedronBinary);
                else
                    shapeGeometry = geomEngine.CreateShapeGeometry(geoSolid, 0.001, 0.0001, 0.5, XbimGeometryType.PolyhedronBinary);
                txn.Commit();
                return shapeGeometry;
            }
        }

        public XbimShapeGeometry GetShapeGeometry(List<IXbimSolid> geoSolids,List<IXbimSolid> openingSolids)
        {
            XbimShapeGeometry shapeGeometry = null;
            using (var txn = memoryModel.BeginTransaction("Create Shape Geometry"))
            {
                IXbimSolidSet solidSet = geomEngine.CreateSolidSet();
                foreach (var geoSolid in geoSolids) 
                {
                    IXbimSolidSet solid = null;
                    if (null != openingSolids && openingSolids.Count > 0)
                    {
                        foreach (var item in openingSolids)
                        {
                            if (solid == null)
                                solid = geoSolid.Cut(item, 1);
                            else
                                solid = solid.Cut(item, 1);
                        }
                    }
                    if (solid == null)
                        solidSet.Add(geoSolid);
                    else
                        foreach (var subSolid in solid)
                            solidSet.Add(subSolid);
                }
                shapeGeometry = geomEngine.CreateShapeGeometry(solidSet, 0.001, 0.0001, 0.5, XbimGeometryType.PolyhedronBinary);
                txn.Commit();
                return shapeGeometry;
            }
        }
        public XbimShapeGeometry GetShapeGeometry(IXbimSolid geoSolid)
        {
            using (var txn = memoryModel.BeginTransaction("Create Shape Geometry"))
            {
                if (null == geoSolid || geoSolid.SurfaceArea < 10)
                    return null;
                XbimShapeGeometry shapeGeometry = geomEngine.CreateShapeGeometry(geoSolid, 0.001, 0.0001, 0.5, XbimGeometryType.PolyhedronBinary);
                txn.Commit();
                return shapeGeometry;
            }
        }

        public IXbimSolid GetXBimSolid(GeometryStretch geometryStretch, XbimVector3D moveVector) 
        {
            IXbimSolid geoSolid = null;
            using (var txn = memoryModel.BeginTransaction("Create solid"))
            {
                if (ifcVersion == IfcSchemaVersion.Ifc2X3)
                {
                    geoSolid = GetXBimSolid2x3(geometryStretch, moveVector);
                }
                else
                {
                    geoSolid = GetXBimSolid4(geometryStretch, moveVector);
                }
                txn.Commit();
            }
            return geoSolid;
        }
        private IXbimSolid GetXBimSolid2x3(GeometryStretch geometryStretch, XbimVector3D moveVector) 
        {
            Xbim.Ifc2x3.ProfileResource.IfcProfileDef profile = null;
            XbimPoint3D planeOrigin = geometryStretch.Origin + moveVector+ geometryStretch.ZAxis* geometryStretch.ZAxisOffSet;
            bool isOutLine = false;
            if (geometryStretch.OutLine.Points != null)
            {
                isOutLine = true;
                profile = ThIFC2x3GeExtension.ToIfcArbitraryClosedProfileDef(memoryModel, geometryStretch.OutLine);
            }
            else
            {
                if (Math.Abs(geometryStretch.XAxisLength) > 1 && Math.Abs(geometryStretch.YAxisLength) > 1)
                    profile = ThIFC2x3GeExtension.ToIfcRectangleProfileDef(memoryModel, XbimPoint3D.Zero, geometryStretch.XAxisLength, geometryStretch.YAxisLength);
            }
            if (profile == null)
                return null;
            var solid = memoryModel.ToIfcExtrudedAreaSolid(profile, geometryStretch.ZAxis, geometryStretch.ZAxisLength);
            var geoSolid = geomEngine.CreateSolid(solid);
            if (!isOutLine)
            {
                var yAxis = geometryStretch.ZAxis.CrossProduct(geometryStretch.XAxis);
                var word = XbimMatrix3D.CreateWorld(planeOrigin.Point3D2Vector(), geometryStretch.ZAxis.Negated(), yAxis);
                geoSolid = geoSolid.Transform(word) as IXbimSolid;
            }
            else 
            {
                var trans = XbimMatrix3D.CreateTranslation(moveVector.X, moveVector.Y, moveVector.Z);
                geoSolid = geoSolid.Transform(trans) as IXbimSolid;
            }
            return geoSolid;
        }
        private IXbimSolid GetXBimSolid4(GeometryStretch geometryStretch, XbimVector3D moveVector)
        {
            Xbim.Ifc4.ProfileResource.IfcProfileDef profile = null;
            XbimPoint3D planeOrigin = geometryStretch.Origin + moveVector + geometryStretch.ZAxis*geometryStretch.ZAxisOffSet;
            bool isOutLine = false;
            if (geometryStretch.OutLine.Points != null)
            {
                isOutLine = true;
                profile = ThIFC4GeExtension.ToIfcArbitraryClosedProfileDef(memoryModel, geometryStretch.OutLine);
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
            if (!isOutLine)
            {
                var yAxis = geometryStretch.ZAxis.CrossProduct(geometryStretch.XAxis);
                var word = XbimMatrix3D.CreateWorld(planeOrigin.Point3D2Vector(), geometryStretch.ZAxis.Negated(), yAxis);
                geoSolid = geoSolid.Transform(word) as IXbimSolid;
            }
            else
            {
                var trans = XbimMatrix3D.CreateTranslation(moveVector.X, moveVector.Y, moveVector.Z);
                geoSolid = geoSolid.Transform(trans) as IXbimSolid;
            }
            return geoSolid;
        }
    }
}
