using THBimEngine.Domain;
using Xbim.Common.Geometry;

namespace THBimEngine.Geometry
{
    // References:
    //  https://github.com/Eneroth3/sketchup-community-lib/blob/master/modules/geom/transformation.rb
    public static class XbimMatrixHelper
    {
        public static double XScale(this XbimMatrix3D matrix)
        {
            if (matrix.IsAffine)
            {
                var xaxis = matrix.XAxis();
                return xaxis.Length;
            }
            return 0.0;
        }

        public static XbimVector3D XAxis(this XbimMatrix3D matrix)
        {
            return new XbimVector3D(matrix.Right.X / matrix.M44, matrix.Right.Y / matrix.M44, matrix.Right.Z / matrix.M44);
        }

        public static double YScale(this XbimMatrix3D matrix)
        {
            if (matrix.IsAffine)
            {
                var yaxis = matrix.YAxis();
                return yaxis.Length;
            }
            return 0.0;
        }

        public static XbimVector3D YAxis(this XbimMatrix3D matrix)
        {
            return new XbimVector3D(matrix.Up.X / matrix.M44, matrix.Up.Y / matrix.M44, matrix.Up.Z / matrix.M44);
        }

        public static double ZScale(this XbimMatrix3D matrix)
        {
            if (matrix.IsAffine)
            {
                var zaxis = matrix.ZAxis();
                return zaxis.Length;
            }
            return 0.0;
        }

        public static XbimVector3D ZAxis(this XbimMatrix3D matrix)
        {
            return new XbimVector3D(matrix.Backward.X / matrix.M44, matrix.Backward.Y / matrix.M44, matrix.Backward.Z / matrix.M44);
        }

        public static double Determinant(this XbimMatrix3D matrix)
        {
            return matrix.XAxis().DotProduct(matrix.YAxis().CrossProduct(matrix.ZAxis()));
        }

        public static XbimMatrix3D CreateFromAxes(XbimPoint3D origin, XbimVector3D xaxis, XbimVector3D yaxis, XbimVector3D zaxis)
        {
            return new XbimMatrix3D(
                xaxis.X, xaxis.Y, xaxis.Z, 0,
                yaxis.X, yaxis.Y, yaxis.Z, 0,
                zaxis.X, zaxis.Y, zaxis.Z, 0,
                origin.X, origin.Y, origin.Z, 1);
        }

        public static XbimMatrix3D RemoveScaling(this XbimMatrix3D matrix, bool allowFlip = false)
        {
            var xaxis = matrix.XAxis().Normalized();
            if (matrix.IsFlipped() && !allowFlip)
            {
                xaxis = xaxis.Negated();
            }
            var origin = new XbimPoint3D(matrix.OffsetX, matrix.OffsetY, matrix.OffsetZ);
            return CreateFromAxes(origin, xaxis, matrix.YAxis().Normalized(), matrix.ZAxis().Normalized());
        }

        public static bool IsSheared(this XbimMatrix3D matrix, double angularTolerance)
        {
            return !matrix.XAxis().IsParallel(matrix.YAxis().CrossProduct(matrix.ZAxis()), angularTolerance);
        }

        public static bool IsFlipped(this XbimMatrix3D matrix)
        {
            var dotx = matrix.AxesDotProductX();
            var doty = matrix.AxesDotProductY();
            var dotz = matrix.AxesDotProductZ();
            return FlippedDot(dotx, doty, dotz);
        }

        public static bool FlippedX(this XbimMatrix3D matrix)
        {
            var dotx = matrix.AxesDotProductX();
            var doty = matrix.AxesDotProductY();
            var dotz = matrix.AxesDotProductZ();
            return dotx < 0 && FlippedDot(dotx, doty, dotz);
        }

        public static bool FlippedY(this XbimMatrix3D matrix)
        {
            var dotx = matrix.AxesDotProductX();
            var doty = matrix.AxesDotProductY();
            var dotz = matrix.AxesDotProductZ();
            return doty < 0 && FlippedDot(dotx, doty, dotz);
        }

        public static bool FlippedZ(this XbimMatrix3D matrix)
        {
            var dotx = matrix.AxesDotProductX();
            var doty = matrix.AxesDotProductY();
            var dotz = matrix.AxesDotProductZ();
            return dotz < 0 && FlippedDot(dotx, doty, dotz);
        }

        public static double AxesDotProductX(this XbimMatrix3D matrix)
        {
            return matrix.Right.DotProduct(THBimDomainCommon.XAxis);
        }

        public static double AxesDotProductY(this XbimMatrix3D matrix)
        {
            return matrix.Up.DotProduct(THBimDomainCommon.YAxis);
        }

        public static double AxesDotProductZ(this XbimMatrix3D matrix)
        {
            return matrix.Backward.DotProduct(THBimDomainCommon.ZAxis);
        }

        private static bool FlippedDot(double x, double y, double z)
        {
            return x * y * z < 0;
        }
    }
}
