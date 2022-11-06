using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Geometry;

namespace THBimEngine.Domain
{
    public static class ThProtoBufExtension
    {
        public static readonly XbimVector3D XAxis = new XbimVector3D(1, 0, 0);
        public static readonly XbimVector3D YAxis = new XbimVector3D(0, 1, 0);
        public static readonly XbimVector3D ZAxis = new XbimVector3D(0, 0, 1);
        public static readonly XbimMatrix3D WordMatrix = new XbimMatrix3D(XbimVector3D.Zero);
        public static readonly XbimMatrix3D XZMatrix = new XbimMatrix3D(1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
        public static XbimVector3D Point3D2Vector(this ThTCHPoint3d pt)
        {
            return new XbimVector3D(pt.X, pt.Y, pt.Z);
        }
        public static XbimMatrix3D ToXBimMatrix3D(this ThTCHMatrix3d matrix)
        {
            return new XbimMatrix3D(matrix.Data11, matrix.Data12, matrix.Data13, matrix.Data14,
                matrix.Data21, matrix.Data22, matrix.Data23, matrix.Data24,
                matrix.Data31, matrix.Data32, matrix.Data33, matrix.Data34,
                matrix.Data41, matrix.Data42, matrix.Data43, matrix.Data44);
        }
        public static XbimVector3D Vector3D2XBimVector(this ThTCHVector3d vector)
        {
            if (vector == null)
                return new XbimVector3D(1, 0, 0);
            return new XbimVector3D(vector.X, vector.Y, vector.Z);
        }
        public static XbimVector3D Vector3D2XBimVector(this ThTCHBuiltElementData element)
        {
            return new XbimVector3D(0, 0, 1);
        }
        public static XbimPoint3D Point3D2XBimPoint(this ThTCHPoint3d point)
        {
            return new XbimPoint3D(point.X, point.Y, point.Z);
        }

        public static ThTCHPoint3d XBimPoint2Point3D(this XbimPoint3D point)
        {
            return new ThTCHPoint3d()
            {
                X = point.X,
                Y =  point.Y,
                Z = point.Z
            };
        }

        public static int ToInt(this uint value)
        {
            return int.Parse(value.ToString());
        }

        public static GeometryParam THSUGeometryParam(this ThSUCompDefinitionData definitionData, ThTCHMatrix3d suMatrix)
        {
            var geoParam = new GeometryFacetedBrep(definitionData, suMatrix);
            return geoParam;
        }

        public static GeometryParam THTCHGeometryParam(this ThTCHBuiltElementData tchElement)
        {
            var xVector = tchElement.XVector.Vector3D2XBimVector();
            if (tchElement.Outline != null && tchElement.Outline.Shell != null && tchElement.Outline.Shell.Points.Count >= 2)
            {
                var outLineGeoParam = new GeometryStretch(
                                        tchElement.Outline,
                                        xVector,
                                        tchElement.Vector3D2XBimVector(),
                                        tchElement.Height);
                outLineGeoParam.YAxisLength = tchElement.Width;
                return outLineGeoParam;
            }
            else
            {
                var geoParam = new GeometryStretch(
                                   tchElement.Origin.Point3D2XBimPoint(),
                                   xVector,
                                   tchElement.Length,
                                   tchElement.Width,
                                   tchElement.Vector3D2XBimVector(),
                                   tchElement.Height);
                return geoParam;
            }
        }

        public static GeometryParam SlabGeometryParam(this ThTCHSlabData tchElement, out List<GeometryStretch> slabDescendingData)
        {
            slabDescendingData = new List<GeometryStretch>();
            //楼板因为向下拉伸了，这里ZOffSet给相反的值
            var outLineGeoParam = new GeometryStretch(tchElement.BuildElement.Outline, XAxis, ZAxis.Negated(), tchElement.BuildElement.Height);
            if (null != tchElement.Descendings)
            {
                foreach (var item in tchElement.Descendings)
                {
                    if (item.IsDescending)
                    {
                        //降板
                        var desGeoStretch = new GeometryStretch(item.Outline, XAxis, ZAxis.Negated(), item.DescendingThickness, item.DescendingHeight);
                        desGeoStretch.YAxisLength = item.DescendingWrapThickness;
                        desGeoStretch.OutlineBuffer = item.OutlineBuffer.Shell;
                        slabDescendingData.Add(desGeoStretch);
                    }
                    else
                    {
                        //洞口
                        outLineGeoParam.Outline.Holes.Add(item.Outline.Shell);
                    }
                }
            }
            return outLineGeoParam;
        }

        public static bool VerifyPipeData(this byte[] data)
        {
            return data[0] == 84 && data[1] == 72 //校验
                && (data[2] == 1 || data[2] == 2 || data[2] == 3) //push/zoom/外链
                && (data[3] == 1 || data[3] == 2); //CAD/SU
        }
    }
}
