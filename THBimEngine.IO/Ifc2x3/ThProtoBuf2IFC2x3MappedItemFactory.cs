using Xbim.Ifc;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.RepresentationResource;
using ThBIMServer.Geometries;

namespace ThBIMServer.Ifc2x3
{
    public static class ThProtoBuf2IFC2x3MappedItemFactory
    {
        public static IfcMappedItem CreateIfcMappedItem(this IfcStore model, 
            IfcShapeRepresentation shape, XbimMatrix3D transform)
        {
            return model.Instances.New<IfcMappedItem>(m =>
            {
                m.MappingSource = model.CreateRepresentationMap(shape);
                m.MappingTarget = model.CreateCartesianTransformationOperator(transform);
            });
        }
        
        public static IfcMappedItem CreateIfcMappedItem(this IfcStore model,
            IfcRepresentationItem shape, XbimMatrix3D transform)
        {
            return model.Instances.New<IfcMappedItem>(m =>
            {
                m.MappingSource = model.CreateRepresentationMap(shape);
                m.MappingTarget = model.CreateCartesianTransformationOperator(transform);
            });
        }

        private static IfcRepresentationMap CreateRepresentationMap(this IfcStore model, IfcShapeRepresentation shape)
        {
            return model.Instances.New<IfcRepresentationMap>(m =>
            {
                m.MappedRepresentation = shape;
            });
        }

        private static IfcRepresentationMap CreateRepresentationMap(this IfcStore model, IfcRepresentationItem shape)
        {
            var mappedRepresentation = model.Instances.New<IfcRepresentation>();
            mappedRepresentation.Items.Add(shape);
            return model.Instances.New<IfcRepresentationMap>(m =>
            {
                m.MappedRepresentation = mappedRepresentation;
            });
        }

        private static IfcCartesianTransformationOperator3D CreateCartesianTransformationOperator(this IfcStore model, XbimMatrix3D matrix)
        {
            var cs = new ThXbimCoordinateSystem3D(matrix);
            return model.Instances.New<IfcCartesianTransformationOperator3D>(o =>
            {
                // 暂时不考虑缩放（Scale）
                o.Axis1 = model.ToIfcDirection(cs.CS.XAxis);
                o.Axis2 = model.ToIfcDirection(cs.CS.YAxis);
                o.Axis3 = model.ToIfcDirection(cs.CS.ZAxis);
                o.LocalOrigin = model.ToIfcCartesianPoint(cs.CS.Origin);
            });
        }
    }
}
