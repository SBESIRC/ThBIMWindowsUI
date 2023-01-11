using System;
using System.Collections.Generic;

using Xbim.Ifc;
using Xbim.Common;
using Xbim.IO.Memory;
using Xbim.Common.Step21;
using Xbim.Common.Geometry;
using Xbim.Ifc4.Interfaces;
using Xbim.Geometry.Engine.Interop;

using THBimEngine.Domain;
using Xbim.Tessellator;

namespace THBimEngine.Geometry
{
    public class GeometryFactory
    {
        MemoryModel memoryModel;
        IXbimGeometryEngine geomEngine;
        IfcSchemaVersion ifcVersion;
        double minSurfaceArea = 10;
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

        public XbimShapeGeometry GetShapeGeometry(List<IXbimSolid> geoSolids, List<IXbimSolid> openingSolids)
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
                shapeGeometry = geomEngine.CreateShapeGeometry(solidSet, 0.001, 10, 0.5, XbimGeometryType.PolyhedronBinary);
                txn.Commit();
                return shapeGeometry;
            }
        }
        //public List<IXbimSolid> GetXBimSolid(GeometryParam geometryParam, XbimVector3D moveVector)
        //{
        //    var resList = new List<IXbimSolid>();
        //    if (geometryParam is GeometryStretch geometryStretch)
        //    {
        //        IXbimSolid geoSolid = null;
        //        using (var txn = memoryModel.BeginTransaction("Create solid"))
        //        {
        //            if (ifcVersion == IfcSchemaVersion.Ifc2X3)
        //            {
        //                geoSolid = GetXBimSolid2x3(geometryStretch, moveVector);
        //            }
        //            else
        //            {
        //                geoSolid = GetXBimSolid4(geometryStretch, moveVector);
        //            }
        //            txn.Commit();
        //        }
        //        if (null != geoSolid)
        //            resList.Add(geoSolid);
        //    }
        //    else if (geometryParam is GeometryFacetedBrep facetedBrep)
        //    {
        //        using (var txn = memoryModel.BeginTransaction("Create solid"))
        //        { 
        //            resList = GetXBimSolid(facetedBrep, moveVector);
        //            txn.Commit();
        //        }
        //    }
        //    return resList;
        //}

        //public List<IXbimSolid> GetSlabSolid(GeometryParam geometryParam, List<GeometryStretch> slabDes, XbimVector3D moveVector)
        //{
        //    IXbimSolidSet solidSet = geomEngine.CreateSolidSet();
        //    var slabSolid = GetXBimSolid(geometryParam, moveVector);
        //    foreach (var item in slabSolid)
        //        solidSet.Add(item);
        //    var openings = new List<IXbimSolid>();
        //    if (geometryParam is GeometryStretch geometryStretch)
        //    {
        //        using (var txn = memoryModel.BeginTransaction("Create solid"))
        //        {
        //            var thisMove = moveVector + geometryStretch.ZAxis * geometryStretch.ZAxisOffSet;
        //            foreach (var item in slabDes)
        //            {
        //                var outLine = item.OutlineBuffer;
        //                IXbimSolid opening = null;
        //                IXbimSolid geoSolid = null;
        //                if (ifcVersion == IfcSchemaVersion.Ifc2X3)
        //                {
        //                    geoSolid = GetXBimSolid2x3(outLine, thisMove, geometryStretch.ZAxis, item.ZAxisOffSet + item.ZAxisLength);
        //                    opening = GetXBimSolid2x3(item.Outline.Shell, thisMove, geometryStretch.ZAxis, item.ZAxisOffSet);
        //                }
        //                else
        //                {
        //                    geoSolid = GetXBimSolid4(outLine, thisMove, geometryStretch.ZAxis, item.ZAxisOffSet + item.ZAxisLength);
        //                    opening = GetXBimSolid4(item.Outline.Shell, thisMove, geometryStretch.ZAxis, item.ZAxisOffSet);
        //                }
        //                if (null == geoSolid || geoSolid.SurfaceArea < minSurfaceArea)
        //                    continue;
        //                solidSet = solidSet.Union(geoSolid, 1);
        //                openings.Add(opening);
        //            }
        //            foreach (var item in geometryStretch.Outline.Holes)
        //            {
        //                IXbimSolid opening = null;
        //                if (item.Points == null || item.Points.Count < 1)
        //                    continue;
        //                if (ifcVersion == IfcSchemaVersion.Ifc2X3)
        //                {
        //                    opening = GetXBimSolid2x3(item, thisMove, geometryStretch.ZAxis, geometryStretch.ZAxisLength + 0);//geometryStretch.Outline.HolesMaxHeight
        //                }
        //                else
        //                {
        //                    opening = GetXBimSolid4(item, thisMove, geometryStretch.ZAxis, geometryStretch.ZAxisLength + 0);//geometryStretch.Outline.HolesMaxHeight
        //                }
        //                if (null == opening || opening.SurfaceArea < minSurfaceArea)
        //                    continue;
        //                openings.Add(opening);
        //            }
        //            foreach (var item in openings)
        //            {
        //                solidSet = solidSet.Cut(item, 1);
        //            }
        //            txn.Commit();
        //        }
        //    }
        //    List<IXbimSolid> solids = new List<IXbimSolid>();
        //    foreach (var item in solidSet)
        //        solids.Add(item);
        //    return solids;
        //}

        public List<IXbimSolid> GetXBimSolid(IPersistEntity persistEntity)
        {
            var resSolids = new List<IXbimSolid>();
            if (persistEntity is IIfcElement ifc4Elem)
            {
                var ifcGeo = ifc4Elem.Representation.Representations[0].Items[0];
                //var test = geomEngine.CreateSolidSet(ifcGeo);
            }
            else if (persistEntity is Xbim.Ifc2x3.Interfaces.IIfcElement ifc2Elem)
            {

            }
            return resSolids;
        }

        //public IXbimSolid GetXBimSolid(GeometryStretch geometryStretch, XbimVector3D moveVector)
        //{
        //    IXbimSolid geoSolid = null;
        //    using (var txn = memoryModel.BeginTransaction("Create solid"))
        //    {
        //        if (ifcVersion == IfcSchemaVersion.Ifc2X3)
        //        {
        //            geoSolid = GetXBimSolid2x3(geometryStretch, moveVector);
        //        }
        //        else
        //        {
        //            geoSolid = GetXBimSolid4(geometryStretch, moveVector);
        //        }
        //        txn.Commit();
        //    }
        //    return geoSolid;
        //}

        public List<IXbimSolid> GetXBimSolid(GeometryFacetedBrep facetedBrep, XbimVector3D moveVector)
        {
            var resSolids = new List<IXbimSolid>();
            if (null == facetedBrep || facetedBrep.Outer == null || facetedBrep.Outer.Count < 1)
                return resSolids;
            var brep = ThIFC2x3GeExtension.ToIfcFacetedBrep(memoryModel, facetedBrep.Outer, facetedBrep.Voids);
            var geoSolid = geomEngine.CreateSolidSet(brep);
            var trans = XbimMatrix3D.CreateTranslation(moveVector.X, moveVector.Y, moveVector.Z);
            foreach (var item in geoSolid)
            {
                resSolids.Add(item.Transform(trans) as IXbimSolid);
            }
            return resSolids;
        }
        public XbimShapeGeometry BrepFaceToXbimShapeGeometry(GeometryFacetedBrep facetedBrep) 
        {
            using (var txn = memoryModel.BeginTransaction("Create Shape Geometry"))
            {
                var brep = ThIFC2x3GeExtension.ToIfcFacetedBrep(memoryModel, facetedBrep.Outer, facetedBrep.Voids);
                XbimTessellator tessellator = new XbimTessellator(memoryModel, XbimGeometryType.PolyhedronBinary);
                return tessellator.Mesh(brep);
            }
        }
        #region
        //private IXbimSolid GetXBimSolid2x3(GeometryStretch geometryStretch, XbimVector3D moveVector)
        //{
        //    Xbim.Ifc2x3.ProfileResource.IfcProfileDef profile = null;
        //    XbimPoint3D planeOrigin = geometryStretch.Origin + moveVector + geometryStretch.ZAxis * geometryStretch.ZAxisOffSet;
        //    bool isOutLine = false;
        //    if (geometryStretch.Outline != null && geometryStretch.Outline.Shell != null && geometryStretch.Outline.Shell.Points.Count > 0)
        //    {
        //        isOutLine = true;
        //        profile = ThIFC2x3GeExtension.ToIfcArbitraryClosedProfileDef(memoryModel, geometryStretch.Outline.Shell);
        //    }
        //    else
        //    {
        //        if (Math.Abs(geometryStretch.XAxisLength) > 1 && Math.Abs(geometryStretch.YAxisLength) > 1)
        //            profile = ThIFC2x3GeExtension.ToIfcRectangleProfileDef(memoryModel, XbimPoint3D.Zero, geometryStretch.XAxisLength, geometryStretch.YAxisLength);
        //    }
        //    if (profile == null)
        //        return null;
        //    var solid = memoryModel.ToIfcExtrudedAreaSolid(profile, geometryStretch.ZAxis, geometryStretch.ZAxisLength);
        //    var geoSolid = geomEngine.CreateSolid(solid);
        //    if (!isOutLine)
        //    {
        //        var yAxis = geometryStretch.ZAxis.CrossProduct(geometryStretch.XAxis);
        //        var word = XbimMatrix3D.CreateWorld(planeOrigin.Point3D2Vector(), geometryStretch.ZAxis.Negated(), yAxis);
        //        geoSolid = geoSolid.Transform(word) as IXbimSolid;
        //    }
        //    else
        //    {
        //        var realMove = moveVector + geometryStretch.ZAxis * geometryStretch.ZAxisOffSet;
        //        var trans = XbimMatrix3D.CreateTranslation(realMove.X, realMove.Y, realMove.Z);
        //        geoSolid = geoSolid.Transform(trans) as IXbimSolid;
        //    }
        //    return geoSolid;
        //}

        private IXbimSolid GetXBimSolid4(GeometryStretch geometryStretch, XbimVector3D moveVector)
        {
            Xbim.Ifc4.ProfileResource.IfcProfileDef profile = null;
            XbimPoint3D planeOrigin = geometryStretch.Origin + moveVector + geometryStretch.ZAxis * geometryStretch.ZAxisOffSet;
            bool isOutLine = false;
            if (geometryStretch.Outline != null && geometryStretch.Outline.Shell.Points != null && geometryStretch.Outline.Shell.Points.Count > 1)
            {
                isOutLine = true;
                profile = ThIFC4GeExtension.ToIfcArbitraryClosedProfileDef(memoryModel, geometryStretch.Outline.Shell);
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
                var realMove = moveVector + geometryStretch.ZAxis * geometryStretch.ZAxisOffSet;
                var trans = XbimMatrix3D.CreateTranslation(realMove.X, realMove.Y, realMove.Z);
                geoSolid = geoSolid.Transform(trans) as IXbimSolid;
            }
            return geoSolid;
        }

        //private IXbimSolid GetXBimSolid2x3(ThTCHPolyline polyline, XbimVector3D moveVector, XbimVector3D zAxis, double zHeight)
        //{
        //    Xbim.Ifc2x3.ProfileResource.IfcProfileDef profile = ThIFC2x3GeExtension.ToIfcArbitraryClosedProfileDef(memoryModel, polyline);
        //    if (profile == null)
        //        return null;
        //    var solid = memoryModel.ToIfcExtrudedAreaSolid(profile, zAxis, zHeight);
        //    var geoSolid = geomEngine.CreateSolid(solid);
        //    var trans = XbimMatrix3D.CreateTranslation(moveVector.X, moveVector.Y, moveVector.Z);
        //    geoSolid = geoSolid.Transform(trans) as IXbimSolid;
        //    return geoSolid;
        //}

        private IXbimSolid GetXBimSolid4(ThTCHPolyline polyline, XbimVector3D moveVector, XbimVector3D zAxis, double zHeight)
        {
            Xbim.Ifc4.ProfileResource.IfcProfileDef profile = ThIFC4GeExtension.ToIfcArbitraryClosedProfileDef(memoryModel, polyline);
            if (profile == null)
                return null;
            var solid = memoryModel.ToIfcExtrudedAreaSolid(profile, zAxis, zHeight);
            var geoSolid = geomEngine.CreateSolid(solid);
            var trans = XbimMatrix3D.CreateTranslation(moveVector.X, moveVector.Y, moveVector.Z);
            geoSolid = geoSolid.Transform(trans) as IXbimSolid;
            return geoSolid;
        }
        #endregion
    }
}
