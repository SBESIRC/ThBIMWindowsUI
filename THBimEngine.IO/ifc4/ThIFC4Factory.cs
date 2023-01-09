﻿using Xbim.Ifc;
using Xbim.Common.Step21;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.GeometricModelResource;

namespace ThBIMServer.Ifc4
{
    public class ThIFC4Factory
    {
        public static IfcShapeRepresentation CreateBrepBody(IfcStore model, IfcRepresentationItem item)
        {
            var context = GetGeometricRepresentationContext(model);
            if (context != null)
            {
                return model.Instances.New<IfcShapeRepresentation>(s =>
                {
                    s.Items.Add(item);
                    s.ContextOfItems = context;
                    s.RepresentationType = "Brep";
                    s.RepresentationIdentifier = "Body";
                });
            }
            return null;
        }

        public static IfcShapeRepresentation CreateFaceBasedSurfaceBody(IfcStore model, IfcRepresentationItem item)
        {
            var context = GetGeometricRepresentationContext(model);
            if (context != null)
            {
                return model.Instances.New<IfcShapeRepresentation>(s =>
                {
                    s.Items.Add(item);
                    s.ContextOfItems = context;
                    s.RepresentationType =item is IfcExtrudedAreaSolid ? "SweptSolid" : "Brep";
                    s.RepresentationIdentifier = "Body";
                });
            }
            return null;
        }

        public static IfcShapeRepresentation CreateSweptSolidBody(IfcStore model, IfcRepresentationItem item)
        {
            var context = GetGeometricRepresentationContext(model);
            if (context != null)
            {
                return model.Instances.New<IfcShapeRepresentation>(s =>
                {
                    s.Items.Add(item);
                    s.ContextOfItems = context;
                    s.RepresentationType = "SweptSolid";
                    s.RepresentationIdentifier = "Body";
                });
            }
            return null;
        }

        public static IfcShapeRepresentation CreateCSGSolidBody(IfcStore model, IfcRepresentationItem item)
        {
            var context = GetGeometricRepresentationContext(model);
            if (context != null)
            {
                return model.Instances.New<IfcShapeRepresentation>(s =>
                {
                    s.Items.Add(item);
                    s.ContextOfItems = context;
                    s.RepresentationType = "CSG";
                    s.RepresentationIdentifier = "Body";
                });
            }
            return null;
        }

        public static IfcShapeRepresentation CreateSolidClippingBody(IfcStore model, IfcRepresentationItem item)
        {
            var context = GetGeometricRepresentationContext(model);
            if (context != null)
            {
                return model.Instances.New<IfcShapeRepresentation>(s =>
                {
                    s.Items.Add(item);
                    s.ContextOfItems = context;
                    s.RepresentationType = "Clipping";
                    s.RepresentationIdentifier = "Body";
                });
            }
            return null;
        }

        public static IfcProductDefinitionShape CreateProductDefinitionShape(IfcStore model, IfcShapeRepresentation representation)
        {
            return model.Instances.New<IfcProductDefinitionShape>(s =>
            {
                s.Representations.Add(representation);
            });
        }

        public static IfcGeometricRepresentationContext GetGeometricRepresentationContext(IfcStore model)
        {
            return model.Instances.FirstOrDefault<IfcGeometricRepresentationContext>();
        }

        public static IfcStore CreateMemoryModel()
        {
            return IfcStore.Create(IfcSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);
        }

        public static IfcCompositeCurve CreateIfcCompositeCurve(IfcStore model)
        {
            return model.Instances.New<IfcCompositeCurve>();
        }

        public static IfcCompositeCurveSegment CreateIfcCompositeCurveSegment(IfcStore model)
        {
            return model.Instances.New<IfcCompositeCurveSegment>(s =>
            {
                s.SameSense = true;
            });
        }
    }
}