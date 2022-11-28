﻿using System;
using System.Linq;
using System.Collections.Generic;

using Xbim.Ifc;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.TopologyResource;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Common.XbimExtensions;
using THBimEngine.Domain;

namespace ThBIMServer.Ifc2x3
{
    public static class ThProtoBuf2IFC2x3BrepExtension
    {
        public static IfcFacetedBrep CreateIfcFacetedBrep(this IfcStore model, List<IXbimSolid> solids)
        {
            var NewBrep = model.Instances.New<IfcFacetedBrep>();
            var ifcClosedShell = model.Instances.New<IfcClosedShell>();
            foreach (var solid in solids)
            {
                foreach (var face in solid.Faces)
                {
                    var ifcface = model.Instances.New<IfcFace>();
                    var ifcFaceOuterBound = model.Instances.New<IfcFaceOuterBound>();
                    IfcPolyLoop ifcloop = model.Instances.New<IfcPolyLoop>();
                    foreach (var pt in face.OuterBound.Points)
                    {
                        var Newpt = model.Instances.New<IfcCartesianPoint>();
                        Newpt.SetXYZ(pt.X, pt.Y, pt.Z);
                        ifcloop.Polygon.Add(Newpt);
                    }
                    ifcFaceOuterBound.Bound = ifcloop;
                    ifcface.Bounds.Add(ifcFaceOuterBound);
                    var innerBounds = face.InnerBounds;
                    if (innerBounds != null && innerBounds.Count > 0)
                    {
                        foreach (var innerBound in innerBounds)
                        {
                            IfcPolyLoop ifcInnerloop = model.Instances.New<IfcPolyLoop>();
                            foreach (var pt in innerBound.Points)
                            {
                                var Newpt = model.Instances.New<IfcCartesianPoint>();
                                Newpt.SetXYZ(pt.X, pt.Y, pt.Z);
                                ifcInnerloop.Polygon.Add(Newpt);
                            }
                            var ifcFaceBound = model.Instances.New<IfcFaceBound>();
                            ifcFaceBound.Bound = ifcInnerloop;
                            ifcface.Bounds.Add(ifcFaceBound);
                        }
                    }

                    ifcClosedShell.CfsFaces.Add(ifcface);
                }
            }
            NewBrep.Outer = ifcClosedShell;

            return NewBrep;
        }

        public static IfcFaceBasedSurfaceModel ToIfcFaceBasedSurface(this IfcStore model, ThSUCompDefinitionData def)
        {
            var connectedFaceSet = model.Instances.New<IfcConnectedFaceSet>();
            var faceBasedSurface = model.Instances.New<IfcFaceBasedSurfaceModel>();
            foreach (var face in def.MeshFaces)
            {
                var mesh = face.Mesh;
                for (int i = 0; i < mesh.Polygons.Count; i++)
                {
                    var vertices = Vertices(mesh, mesh.Polygons[i]);
                    connectedFaceSet.CfsFaces.Add(ToIfcFace(model, vertices));
                }
            }
            faceBasedSurface.FbsmFaces.Add(connectedFaceSet);
            return faceBasedSurface;
        }

        public static IfcFacetedBrep ToIfcFacetedBrep(this IfcStore model, ThSUCompDefinitionData def, bool isRightHandedCoordinate = true, double xZoom = 1.0, double yZoom = 1.0, double zZoom = 1.0)
        {
            var NewBrep = model.Instances.New<IfcFacetedBrep>();
            var ifcClosedShell = model.Instances.New<IfcClosedShell>();
            foreach (var face in def.BrepFaces)
            {
                var ifcface = model.Instances.New<IfcFace>();
                ifcface.Bounds.Add(model.ToIfcFaceOuterBound(face.OuterLoop.Points.Select(o => new ThTCHPoint3d() { X = o.X * xZoom, Y = (isRightHandedCoordinate ? 1 : -1) * o.Y * yZoom, Z = o.Z * zZoom }).ToList()));
                var innerBounds = face.InnerLoops;
                if (innerBounds != null && innerBounds.Count > 0)
                {
                    foreach (var innerBound in innerBounds)
                    {
                        ifcface.Bounds.Add(model.ToIfcFaceBound(innerBound.Points.Select(o => new ThTCHPoint3d() { X = o.X * xZoom, Y = (isRightHandedCoordinate ? 1 : -1) * o.Y * yZoom, Z = o.Z * zZoom }).ToList()));
                    }
                }
                ifcClosedShell.CfsFaces.Add(ifcface);
            }
            NewBrep.Outer = ifcClosedShell;
            return NewBrep;
        }

        public static IfcRepresentationItem ToIfcExtrudedAreaSolid(this IfcStore model, IfcFacetedBrep ifcFacetedBrep, out XbimMatrix3D matrix)
        {
            matrix = XbimMatrix3D.Identity;
            Xbim.Ifc4.Interfaces.IXbimGeometryEngine geomEngine = new Xbim.Geometry.Engine.Interop.XbimGeometryEngine();
            var solid = geomEngine.CreateSolid(ifcFacetedBrep);
            if (solid.Faces.Count == 6 && solid.Vertices.Count == 8)
            {
                //不符合长方体(6个面，8个点)规则的我们认为是异形梁，不予转成拉伸体
                var BeamCrossSections = solid.Faces.OrderBy(o => o.Area).Take(2);
                var CrossSection1 = BeamCrossSections.First();
                var CrossSection2 = BeamCrossSections.Last();
                if (Math.Abs(CrossSection1.Area - CrossSection2.Area) < 10 && CrossSection1.Normal.IsParallel(CrossSection2.Normal, 1) && CrossSection1.OuterBound.Vertices.Count == 4)
                {
                    var edge = solid.Edges.FirstOrDefault(e => !CrossSection1.OuterBound.Edges.Contains(e) && !CrossSection2.OuterBound.Edges.Contains(e));
                    var pt1 = edge.EdgeStart.VertexGeometry;
                    var pt2 = edge.EdgeEnd.VertexGeometry;
                    XbimVector3D direction = XbimVector3D.Zero;
                    if (CrossSection1.OuterBound.Points.Any(o => o.Equals(pt1)) && CrossSection2.OuterBound.Points.Any(o => o.Equals(pt2)))
                    {
                        direction = pt2 - pt1;
                    }
                    else if (CrossSection2.OuterBound.Points.Any(o => o.Equals(pt1)) && CrossSection1.OuterBound.Points.Any(o => o.Equals(pt2)))
                    {
                        direction = pt1 - pt2;
                    }
                    else
                    {
                        return ifcFacetedBrep;
                    }
                    var pts = CrossSection1.OuterBound.Points.ToList();
                    var centerPt = pts[0].GetCenter(pts[2]);
                    var upDirection = pts[2] - pts[1];
                    matrix = XbimMatrix3D.CreateWorld(centerPt.Point3D2Vector(), direction.Normalized().Negated(), upDirection.Normalized());
                    matrix.M44 = 1;
                    matrix.Invert();
                    var BottomFace = CrossSection1.Transform(matrix) as IXbimFace;
                    var d = matrix.Transform(direction);
                    var profile = model.ToIfcArbitraryClosedProfileDef(BottomFace.OuterBound);
                    var ifcAreaSolid = model.ToIfcExtrudedAreaSolid(profile, new XbimVector3D(0, 0, 1), direction.Length);
                    matrix = XbimMatrix3D.CreateWorld(centerPt.Point3D2Vector(), direction.Normalized().Negated(), upDirection.Normalized());
                    matrix.M44 = 1;
                    return ifcAreaSolid;
                }
            }
            return ifcFacetedBrep;
        }

        private static IfcFaceOuterBound ToIfcFaceOuterBound(this IfcStore model, List<ThTCHPoint3d> vertices)
        {
            return model.Instances.New<IfcFaceOuterBound>(b =>
            {
                b.Bound = model.ToIfcPolyLoop(vertices);
            });
        }

        private static IfcFaceBound ToIfcFaceBound(this IfcStore model, List<ThTCHPoint3d> vertices)
        {
            return model.Instances.New<IfcFaceBound>(b =>
            {
                b.Bound = model.ToIfcPolyLoop(vertices);
            });
        }

        private static IfcPolyLoop ToIfcPolyLoop(this IfcStore model, List<ThTCHPoint3d> vertices)
        {
            var polyLoop = model.Instances.New<IfcPolyLoop>();
            foreach (ThTCHPoint3d v in vertices)
            {
                polyLoop.Polygon.Add(model.ToIfcCartesianPoint(v));
            }
            return polyLoop;
        }

        private static List<ThTCHPoint3d> Vertices(ThSUPolygonMesh mesh, ThSUPolygon polygon)
        {
            List<ThTCHPoint3d> vertices = new List<ThTCHPoint3d>();
            for (int i = 0; i < polygon.Indices.Count; i++)
            {
                vertices.Add(mesh.Points[Math.Abs(polygon.Indices[i]) - 1]);
            }
            return vertices;
        }

        private static IfcFace ToIfcFace(this IfcStore model, List<ThTCHPoint3d> vertices)
        {
            var ifcFace = model.Instances.New<IfcFace>();
            ifcFace.Bounds.Add(ToIfcFaceBound(model, vertices));
            return ifcFace;
        }
    }
}
