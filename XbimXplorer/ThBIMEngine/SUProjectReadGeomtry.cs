using System;
using System.Collections.Generic;
using System.Linq;
using THBimEngine.Domain;
using Xbim.Common.Geometry;

namespace XbimXplorer.ThBIMEngine
{
    class SUProjectReadGeomtry
    {
        public List<GeometryMeshModel> ReadGeomtry(ThSUProjectData project, XbimMatrix3D projectMatrix3D, out List<PointNormal> allPointNormals)
        {
            List<GeometryMeshModel> AllModels = new List<GeometryMeshModel>();
            allPointNormals = new List<PointNormal>();
            var buildings = project.Buildings;
            var definitions = project.Definitions;
            //var _ptIndex = 0;
            var _ptCount = 0;
            var _meshIndex = 0;
            foreach (var building in buildings)
            {
                var matrix = building.Component.Transformations;
                Xbim.Common.Geometry.XbimMatrix3D bimMaterial = new Xbim.Common.Geometry.XbimMatrix3D(
                    matrix.Data11, matrix.Data12, matrix.Data13, matrix.Data14,
                    matrix.Data21, matrix.Data22, matrix.Data23, matrix.Data24,
                    matrix.Data31, matrix.Data32, matrix.Data33, matrix.Data34,
                    matrix.Data41, matrix.Data42, matrix.Data43, matrix.Data44
                    );
                bimMaterial *= projectMatrix3D;
                GeometryMeshModel mesh = new GeometryMeshModel(_meshIndex++, building.Root.GlobalId);

                ThSUMaterialData ProtoMaterial = null;
                //ProtoMaterial = building.Component.Material;
                foreach (var face in definitions[building.Component.DefinitionIndex].MeshFaces)
                {
                    //var FaceMaterial = face.Material;
                    var normal = face.FaceNormal;
                    //if (FaceMaterial != null)
                    //{
                    //    ProtoMaterial = FaceMaterial;
                    //}
                    var faceTriangleCount = face.Mesh.Polygons.Count;
                    for (int i = 0; i < faceTriangleCount; i++)
                    {
                        var polygon = face.Mesh.Polygons[i].Indices.Select(o => Math.Abs(o) - 1).ToList();
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
                var TriangleMaterial = THBimMaterial.GetTHBimEntityMaterial("sucomponent");
                TriangleMaterial.MaterialName = ProtoMaterial?.MaterialName;
                mesh.TriangleMaterial = TriangleMaterial;
                //if (ProtoMaterial == null)
                //{
                //    mesh.TriangleMaterial = THBimMaterial.GetTHBimEntityMaterial("");
                //}
                //else
                //{
                //    mesh.TriangleMaterial = new THBimMaterial();
                //    mesh.TriangleMaterial.MaterialName = ProtoMaterial.MaterialName;
                //    mesh.TriangleMaterial.Alpha = ProtoMaterial.Alpha > 0 ? 1 : 0;
                //    if (ProtoMaterial.HasRGB)
                //    {
                //        mesh.TriangleMaterial.Color_R = ProtoMaterial.ColorR / 255f;
                //        mesh.TriangleMaterial.Color_G = ProtoMaterial.ColorG / 255f;
                //        mesh.TriangleMaterial.Color_B = ProtoMaterial.ColorB / 255f;
                //    }
                //}
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
