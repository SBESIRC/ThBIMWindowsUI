using Xbim.Ifc;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.RepresentationResource;
using ThBIMServer.Ifc2x3;
using ThBIMServer.Geometries;

namespace THBimEngine.IO.Ifc2x3
{
    public static class ThProtoBuf2IFC2x3SUExtension
    {
        public static IfcMappedItem ToIfcMappedItem(this IfcStore model, ThSUCompDefinitionData def, ThSUComponentData component)
        {
            return model.CreateIfcMappedItem(
                model.ToIfcShapeRepresentation(def), 
                component.Transformations.ToXbimMatrix3D());
        }

        public static IfcShapeRepresentation ToIfcShapeRepresentation(this IfcStore model, ThSUCompDefinitionData def)
        {
            IfcFaceBasedSurfaceModel mesh = model.ToIfcFaceBasedSurface(def);
            return ThIFC2x3Factory.CreateFaceBasedSurfaceBody(model, mesh);
        }
    }
}
