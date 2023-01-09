using Xbim.Common.Geometry;
using MathNet.Spatial.Euclidean;
using MathNet.Numerics.LinearAlgebra.Double;

namespace ThBIMServer.Geometries
{
    public class ThXbimCoordinateSystem3D
    {
        public CoordinateSystem CS { get; private set; }

        public static ThXbimCoordinateSystem3D Identity => new ThXbimCoordinateSystem3D();

        public ThXbimCoordinateSystem3D()
        {
            CS = CreateCoordinateSystem(XbimMatrix3D.Identity);
        }

        public ThXbimCoordinateSystem3D(ThTCHMatrix3d m)
        {
            CS = CreateCoordinateSystem(m.ToXbimMatrix3D());
        }

        public ThXbimCoordinateSystem3D(XbimMatrix3D m)
        {
            CS = CreateCoordinateSystem(m);
        }

        private CoordinateSystem CreateCoordinateSystem(XbimMatrix3D m)
        {
            return new CoordinateSystem(new DenseMatrix(4, 4, m.ToDoubleArray()));
        }
    }
}
