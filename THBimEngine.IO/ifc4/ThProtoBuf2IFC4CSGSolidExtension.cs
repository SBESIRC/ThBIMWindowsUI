using Xbim.Ifc;
using Xbim.Common.Geometry;
using Xbim.Ifc4.GeometricModelResource;
using THBimEngine.IO.Xbim;
using Xbim.Ifc4.Interfaces;

namespace ThBIMServer.Ifc4
{
    public static class ThProtoBuf2IFC4CSGSolidExtension
    {
        public static IfcBooleanResult ToIfcBooleanResult(this IfcStore model,
            IfcSolidModel first, IfcSolidModel second, IfcBooleanOperator op)
        {
            return model.Instances.New<IfcBooleanResult>(b =>
            {
                b.Operator = op;
                b.FirstOperand = first;
                b.SecondOperand = second;
            });
        }

        public static IfcCsgSolid ToIfcCsgSolid(this IfcStore model,
            IfcSolidModel first, IfcSolidModel second, IfcBooleanOperator op)
        {
            return model.Instances.New<IfcCsgSolid>(c =>
            {
                c.TreeRootExpression = model.ToIfcBooleanResult(first, second, op);
            });
        }

        public static IXbimSolid CreateXbimSolid(IfcCsgSolid csgSolid)
        {
            return ThXbimGeometryService.Instance.Engine.CreateSolid(csgSolid);
        }
    }
}
