﻿using System;
using System.Collections.Generic;
using System.Linq;
using THBimEngine.Domain;
using THBimEngine.Domain.GeometryModel;
using THBimEngine.Geometry.NTS;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Common.Step21;
using Xbim.Geometry.Engine.Interop;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Memory;
using Xbim.ModelGeometry.Scene.Extensions;

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
                            if(null == item)
                                continue;
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
        public List<IXbimSolid> GetXBimSolid(GeometryParam geometryParam, XbimVector3D moveVector)
        {
            var resList = new List<IXbimSolid>();
            using (var txn = memoryModel.BeginTransaction("Create solid"))
            {
                if (geometryParam is GeometryStretch geometryStretch)
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
                    if (null != geoSolid)
                        resList.Add(geoSolid);
                }
                else if (geometryParam is GeometryBrep geometryBrep)
                {
                    resList = GetXBimSolid(geometryBrep, moveVector);
                }
                txn.Commit();
            }

            return resList;
        }
        public List<IXbimSolid> GetSlabSolid(GeometryParam geometryParam, List<GeometryStretch> slabDes, XbimVector3D moveVector)
        {
            List<IXbimSolid> solids = new List<IXbimSolid>();
            var slabSolid = GetXBimSolid(geometryParam, moveVector);
            using (var txn = memoryModel.BeginTransaction("Create solid"))
            {
                IXbimSolidSet solidSet = geomEngine.CreateSolidSet();
                foreach (var item in slabSolid)
                    solidSet.Add(item);
                var openings = new List<IXbimSolid>();
                if (geometryParam is GeometryStretch geometryStretch)
                {

                    foreach (var item in slabDes)
                    {
                        var outLine = item.OutLine.Buffer(item.YAxisLength);
                        IXbimSolid opening = null;
                        IXbimSolid geoSolid = null;
                        var thisMove = moveVector + geometryStretch.ZAxis * item.ZAxisOffSet;
                        if (ifcVersion == IfcSchemaVersion.Ifc2X3)
                        {
                            geoSolid = GetXBimSolid2x3(outLine, moveVector, geometryStretch.ZAxis, item.ZAxisOffSet + item.ZAxisLength);
                            opening = GetXBimSolid2x3(item.OutLine, moveVector, geometryStretch.ZAxis, item.ZAxisOffSet);
                        }
                        else
                        {
                            geoSolid = GetXBimSolid4(outLine, moveVector, geometryStretch.ZAxis, item.ZAxisOffSet + item.ZAxisLength);
                            opening = GetXBimSolid4(item.OutLine, moveVector, geometryStretch.ZAxis, item.ZAxisOffSet);
                        }
                        if (null == geoSolid || geoSolid.SurfaceArea < minSurfaceArea)
                            continue;
                        solidSet = solidSet.Union(geoSolid, 1);
                        openings.Add(opening);
                    }
                    foreach (var item in geometryStretch.OutLine.InnerPolylines)
                    {
                        IXbimSolid opening = null;
                        if (ifcVersion == IfcSchemaVersion.Ifc2X3)
                        {
                            opening = GetXBimSolid2x3(item, moveVector, geometryStretch.ZAxis, geometryStretch.ZAxisLength);
                        }
                        else
                        {
                            opening = GetXBimSolid4(item, moveVector, geometryStretch.ZAxis, geometryStretch.ZAxisLength);
                        }
                        if (null == opening || opening.SurfaceArea < minSurfaceArea)
                            continue;
                        openings.Add(opening);
                    }
                    foreach (var item in openings)
                    {
                        solidSet = solidSet.Cut(item, 1);
                    }

                }

                foreach (var item in solidSet)
                    solids.Add(item);
                txn.Commit();
            }
            return solids;
        }
        public List<IXbimSolid> GetXBimSolid(IIfcProduct persistEntity)
        {
            var resList = new List<IXbimSolid>();
            XbimShapeGeometry shapeGeometry = null;
            var thisEntityIfcType = persistEntity.Model.SchemaVersion;
            List<int> productShapeIds = new List<int>();
            if (persistEntity.Representation != null)
            {
                if (persistEntity.Representation.Representations == null)
                    return resList;
                var rep = persistEntity.Representation.Representations.FirstOrDefault();
                //write out the representation if it has one
                if (rep != null)
                {
                    foreach (var shape in rep.Items.Where(i => !(i is IIfcGeometricSet)))
                    {
                        var mappedItem = shape as IIfcMappedItem;
                        if (mappedItem != null)
                        {

                        }
                        else 
                        {
                            //if not already processed, then add it
                            productShapeIds.Add(shape.EntityLabel);
                            // according to https://github.com/BuildingSMART/IFC4-CV/issues/14 no need to punch holes in the shape if it's a tessellated body
                            //if (rep.RepresentationType.ToString().ToLowerInvariant() == "tessellation" && rep.RepresentationIdentifier.ToString().ToLowerInvariant() == "body")
                            //{
                            //    var groupsToRemove = OpeningsAndProjections.Where(x => x.Key.EntityLabel == product.EntityLabel).ToArray();
                            //    foreach (var rem in groupsToRemove)
                            //    {
                            //        OpeningsAndProjections.Remove(rem);
                            //    }
                            //    VoidedProductIds.Remove(product.EntityLabel);
                            //    continue;
                            //}
                        }

                    }
                }
            }
            else 
            {
            
            }
            if (productShapeIds.Count < 1) 
            {
            
            }
            XbimMatrix3D matrix3D = persistEntity.ObjectPlacement.PlacementToMatrix3D();
            foreach (var item in productShapeIds)
            {
                var geoItem = persistEntity.Model.Instances[item] as IIfcGeometricRepresentationItem;
                if (null == geoItem)
                    continue;
                var createGeo = geomEngine.Create(geoItem);

                if (createGeo is IXbimSolid solid)
                {
                    var transSolid = solid.Transform(matrix3D) as IXbimSolid;
                    resList.Add(transSolid);
                }
                else if (createGeo is IXbimGeometryObjectSet geoSet) 
                {
                    foreach (var gSolid in geoSet.Solids) 
                    {
                        var transSolid = gSolid.Transform(matrix3D) as IXbimSolid;
                        resList.Add(transSolid);
                    }
                }
                else
                {
                    throw new NotSupportedException(string.Format("暂未支持的类型{0},GeoType {1}", createGeo.GetType().Name, createGeo.GeometryType));
                }
            }
            //if (thisEntityIfcType == IfcSchemaVersion.Ifc2X3)
            //{
            //    var ifc2Elem = persistEntity as Xbim.Ifc2x3.Interfaces.IIfcElement;
            //    foreach (var item in ifc2Elem.Representation.Representations)
            //    {

            //        var geoItem = ((Xbim.Ifc2x3.RepresentationResource.IfcRepresentation)item.Model.Instances[item.EntityLabel]).Items[0] as IIfcGeometricRepresentationItem;
            //        if (null == geoItem)
            //            continue;
            //        var createGeo = geomEngine.Create(geoItem);
            //        var shapeGeo = geomEngine.CreateShapeGeometry(createGeo, 1, 1, 0.5, XbimGeometryType.PolyhedronBinary);
            //    }
            //    if (ifc2Elem is Xbim.Ifc2x3.Interfaces.IIfcWall wall) 
            //    {
            //        //处理洞口
            //    }
            //}
            //else 
            //{
            //    var ifc4Elem = persistEntity as Xbim.Ifc4.Interfaces.IIfcElement;

            //}
            return resList;
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
        public List<IXbimSolid> GetXBimSolid(GeometryBrep geometryBrep, XbimVector3D moveVector)
        {
            var resSolids = new List<IXbimSolid>();
            var brep = ThIFC2x3GeExtension.ToIfcFacetedBrep(memoryModel, geometryBrep.Outer, geometryBrep.Voids);
            var geoSolid = geomEngine.CreateSolidSet(brep);
            var trans = XbimMatrix3D.CreateTranslation(moveVector.X, moveVector.Y, moveVector.Z);
            foreach (var item in geoSolid)
            {
                resSolids.Add(item.Transform(trans) as IXbimSolid);
            }
            return resSolids;
        }

        #region
        private IXbimSolid GetXBimSolid2x3(GeometryStretch geometryStretch, XbimVector3D moveVector)
        {
            Xbim.Ifc2x3.ProfileResource.IfcProfileDef profile = null;
            XbimPoint3D planeOrigin = geometryStretch.Origin + moveVector + geometryStretch.ZAxis * geometryStretch.ZAxisOffSet;
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
                var realMove = moveVector + geometryStretch.ZAxis * geometryStretch.ZAxisOffSet;
                var trans = XbimMatrix3D.CreateTranslation(realMove.X, realMove.Y, realMove.Z);
                geoSolid = geoSolid.Transform(trans) as IXbimSolid;
            }
            return geoSolid;
        }
        private IXbimSolid GetXBimSolid4(GeometryStretch geometryStretch, XbimVector3D moveVector)
        {
            Xbim.Ifc4.ProfileResource.IfcProfileDef profile = null;
            XbimPoint3D planeOrigin = geometryStretch.Origin + moveVector + geometryStretch.ZAxis * geometryStretch.ZAxisOffSet;
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
                var realMove = moveVector + geometryStretch.ZAxis * geometryStretch.ZAxisOffSet;
                var trans = XbimMatrix3D.CreateTranslation(realMove.X, realMove.Y, realMove.Z);
                geoSolid = geoSolid.Transform(trans) as IXbimSolid;
            }
            return geoSolid;
        }

        private IXbimSolid GetXBimSolid2x3(PolylineSurrogate polyline, XbimVector3D moveVector, XbimVector3D zAxis, double zHeight)
        {
            Xbim.Ifc2x3.ProfileResource.IfcProfileDef profile = ThIFC2x3GeExtension.ToIfcArbitraryClosedProfileDef(memoryModel, polyline);
            if (profile == null)
                return null;
            var solid = memoryModel.ToIfcExtrudedAreaSolid(profile, zAxis, zHeight);
            var geoSolid = geomEngine.CreateSolid(solid);
            var trans = XbimMatrix3D.CreateTranslation(moveVector.X, moveVector.Y, moveVector.Z);
            geoSolid = geoSolid.Transform(trans) as IXbimSolid;
            return geoSolid;
        }
        private IXbimSolid GetXBimSolid4(PolylineSurrogate polyline, XbimVector3D moveVector, XbimVector3D zAxis, double zHeight)
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

        public List<IXbimSolid> GetXBimSolid2x3(GeometryBrep geometryBrep, XbimVector3D moveVector)
        {
            var resSolids = new List<IXbimSolid>();
            var brep = ThIFC2x3GeExtension.ToIfcFacetedBrep(memoryModel, geometryBrep.Outer, geometryBrep.Voids);
            var geoSolid = geomEngine.CreateSolidSet(brep);
            var trans = XbimMatrix3D.CreateTranslation(moveVector.X, moveVector.Y, moveVector.Z);
            foreach (var item in geoSolid)
            {
                resSolids.Add(item.Transform(trans) as IXbimSolid);
            }
            return resSolids;
        }
        public List<IXbimSolid> GetXBimSolid4(GeometryBrep geometryBrep, XbimVector3D moveVector)
        {
            var resSolids = new List<IXbimSolid>();
            var brep = ThIFC4GeExtension.ToIfcFacetedBrep(memoryModel, geometryBrep.Outer, geometryBrep.Voids);
            var geoSolid = geomEngine.CreateSolidSet(brep);
            var trans = XbimMatrix3D.CreateTranslation(moveVector.X, moveVector.Y, moveVector.Z);
            foreach (var item in geoSolid)
            {
                resSolids.Add(item.Transform(trans) as IXbimSolid);
            }
            return resSolids;
        }
        #endregion
    }
}
