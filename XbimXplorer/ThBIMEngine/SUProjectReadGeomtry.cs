﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THBimEngine.Domain;

namespace XbimXplorer.ThBIMEngine
{
    class SUProjectReadGeomtry
    {
        public List<GeometryMeshModel> ReadGeomtry(ThSUProjectData project, out List<PointNormal> allPointNormals)
        {
            List<GeometryMeshModel> AllModels = new List<GeometryMeshModel>();
            allPointNormals = new List<PointNormal>();
            var buildings = project.Buildings;
            //var _ptIndex = 0;
            var _ptCount = 0;
            var _meshIndex = 0;
            foreach (var building in buildings)
            {
                var matrix = building.Component.Transformations;

                Xbim.Common.Geometry.XbimMatrix3D bimMaterial = new Xbim.Common.Geometry.XbimMatrix3D(
                    matrix.Data11,matrix.Data12,matrix.Data13,matrix.Data14,
                    matrix.Data21,matrix.Data22,matrix.Data23,matrix.Data24,
                    matrix.Data31,matrix.Data32,matrix.Data33,matrix.Data34,
                    matrix.Data41,matrix.Data42,matrix.Data43,matrix.Data44
                    );

                GeometryMeshModel mesh = new GeometryMeshModel(_meshIndex++, building.Root.GlobalId);
                mesh.TriangleMaterial = THBimMaterial.GetTHBimEntityMaterial("");
                foreach (var face in building.Component.Definition.Faces)
                {
                    var faceTriangleCount = face.Mesh.Polygons.Count;
                    for (int i = 0; i < faceTriangleCount; i++)
                    {
                        var polygon = face.Mesh.Polygons[i].Indices.Select(o => Math.Abs(o) - 1).ToList();
                        var normal = face.Mesh.Normals[i];
                        var faceTriangle = new FaceTriangle();
                        faceTriangle.ptIndex.Add(_ptCount);
                        faceTriangle.ptIndex.Add(_ptCount + 1);
                        faceTriangle.ptIndex.Add(_ptCount + 2);
                        var xBimNormal = bimMaterial.Transform(ToXbimVector3D(normal));
                        allPointNormals.Add(new PointNormal(_ptCount++, bimMaterial.Transform(ToXbimPoint3D(face.Mesh.Points[polygon[0]])), xBimNormal));
                        allPointNormals.Add(new PointNormal(_ptCount++, bimMaterial.Transform(ToXbimPoint3D(face.Mesh.Points[polygon[1]])), xBimNormal));
                        allPointNormals.Add(new PointNormal(_ptCount++, bimMaterial.Transform(ToXbimPoint3D(face.Mesh.Points[polygon[2]])), xBimNormal));
                        mesh.FaceTriangles.Add(faceTriangle);
                    }
                }
                AllModels.Add(mesh);
            }
            return AllModels;
        }

        private Xbim.Common.Geometry.XbimPoint3D ToXbimPoint3D(ThTCHPoint3d point)
        {
            return new Xbim.Common.Geometry.XbimPoint3D(point.X, point.Y, point.Z);
        }

        private Xbim.Common.Geometry.XbimVector3D ToXbimVector3D(ThTCHVector3d vector)
        {
            return new Xbim.Common.Geometry.XbimVector3D(vector.X, vector.Y, vector.Z);
        }
    }
}