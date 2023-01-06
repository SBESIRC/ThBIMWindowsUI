using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xbim.Ifc;
using Xbim.Ifc2x3;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.GeometricConstraintResource;

using ThBIMServer.NTS;
using XbimXplorer.Deduct.Model;

namespace XbimXplorer.Deduct
{
    internal class Build2DModelService
    {
        //----input
        public IfcStore IfcStruct;
        public IfcStore IfcArchi;

        //----output
        public Dictionary<string, DeductGFCModel> ModelList;//key：uid value：model

        public void Build2DModel()
        {
            ModelList = new Dictionary<string, DeductGFCModel>();
            BuildStruct2D();
            BuildArchi2D();
        }

        private void BuildStruct2D()
        {
            bool isArchi = false;
            var prjStruct = IfcStruct.Instances.FirstOrDefault<IfcProject>();
            var buildStruct = prjStruct.Sites.FirstOrDefault()?.Buildings.FirstOrDefault() as IfcBuilding;
            var storeyStruct = buildStruct.BuildingStoreys.OfType<IfcBuildingStorey>().ToList();

            ////这里暂时只放建筑的，GFC只需要一个building就好了
            //var dmBuilding = ToDeductModel(buildStruct);
            //modelList.Add(dmBuilding.UID, dmBuilding);

            foreach (var ifcStorey in storeyStruct)
            {
                var dmStorey = ToDeductModel(ifcStorey, isArchi);
                ModelList.Add(dmStorey.UID, dmStorey);

                foreach (var containElement in ifcStorey.ContainsElements)
                {
                    var elements = containElement.RelatedElements.OfType<IfcProduct>();
                    var walls = elements.OfType<IfcWall>().ToList();
                    foreach (var w in walls)
                    {
                        var wm = new DeductGFCModel(w, isArchi);
                        ModelList.Add(wm.UID, wm);
                        dmStorey.ChildItems.Add(wm.UID);
                    }

                    //暂定楼板使用结构板
                    var slab = elements.OfType<IfcSlab>().ToList();
                    foreach (var s in slab)
                    {
                        var slabModel = new DeductGFCModel(s, isArchi);
                        ModelList.Add(slabModel.UID, slabModel);
                        dmStorey.ChildItems.Add(slabModel.UID);
                    }
                }
            }
        }

        private void BuildArchi2D()
        {
            bool isArchi = true;
            var prjArchi = IfcArchi.Instances.FirstOrDefault<IfcProject>();
            var buildArchi = prjArchi.Sites.FirstOrDefault()?.Buildings.FirstOrDefault() as IfcBuilding;
            var storeyArchi = buildArchi.BuildingStoreys.OfType<IfcBuildingStorey>().ToList();

            var dmBuilding = ToDeductModel(buildArchi, isArchi);
            ModelList.Add(dmBuilding.UID, dmBuilding);

            foreach (var ifcStorey in storeyArchi)
            {
                var dmStorey = ToDeductModel(ifcStorey, isArchi);
                ModelList.Add(dmStorey.UID, dmStorey);
                dmBuilding.ChildItems.Add(dmStorey.UID);

                foreach (var containElement in ifcStorey.ContainsElements)
                {
                    var elements = containElement.RelatedElements.OfType<IfcProduct>();
                    var walls = elements.OfType<IfcWall>().ToList();
                    foreach (var w in walls)
                    {
                        var wm = new DeductGFCModel(w, isArchi);
                        ModelList.Add(wm.UID, wm);
                        dmStorey.ChildItems.Add(wm.UID);
                        var doorWindow = CreateModelWindowDoor(wm);
                        wm.ChildItems.AddRange(doorWindow.Select(x => x.UID));
                        doorWindow.ForEach(x => ModelList.Add(x.UID, x));
                    }

                    var space = elements.OfType<IfcSpace>().ToList();
                    foreach (var s in space)
                    {
                        var roomModel = new DeductGFCModel(s, isArchi);
                        ModelList.Add(roomModel.UID, roomModel);
                        dmStorey.ChildItems.Add(roomModel.UID);
                    }
                }
            }
        }

        private static DeductGFCModel ToDeductModel(IfcBuilding ifc, bool isArchi)
        {
            var dm = new DeductGFCModel();
            dm.IFC = ifc;
            dm.UID = ifc.GlobalId;
            dm.ItemType = DeductGFCModel.GetDeductType(ifc, isArchi);

            return dm;
        }

        private static DeductGFCModel ToDeductModel(IfcBuildingStorey ifc, bool isArchi)
        {
            var dm = new DeductGFCModel();
            dm.IFC = ifc;
            dm.UID = ifc.GlobalId;
            dm.GlobalZ = ifc.Elevation.Value;
            double Storey_Height = double.Parse(((ifc.PropertySets.FirstOrDefault().PropertySetDefinitions.FirstOrDefault() as Xbim.Ifc2x3.Kernel.IfcPropertySet).HasProperties.FirstOrDefault(o => o.Name == "Height") as Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue).NominalValue.Value.ToString());
            dm.ZValue = Storey_Height;
            dm.ItemType = DeductGFCModel.GetDeductType(ifc, isArchi);

            return dm;
        }

        private List<DeductGFCModel> CreateModelWindowDoor(DeductGFCModel wm)
        {
            var doorWindow = new List<DeductGFCModel>();

            var relVoidsElement = IfcArchi.Instances.OfType<IfcRelVoidsElement>();
            var wall_relVoidsElements = relVoidsElement.Where(o => o.RelatingBuildingElement == wm.IFC).ToList();
            var opennings = wall_relVoidsElements.Select(x => x.RelatedOpeningElement).ToList();

            var relFillsElement = IfcArchi.Instances.OfType<IfcRelFillsElement>();
            var openning_rel = opennings.SelectMany(o => relFillsElement.Where(x => x.RelatingOpeningElement == o)).ToList();
            var doorWindowIFC = openning_rel.Select(x => x.RelatedBuildingElement).ToList();

            var door = doorWindowIFC.OfType<IfcDoor>().Select(x => new DeductGFCModel(x, true)).ToList();
            var windows = doorWindowIFC.OfType<IfcWindow>().Select(x => new DeductGFCModel(x, true)).ToList();

            doorWindow.AddRange(door);
            doorWindow.AddRange(windows);

            return doorWindow;

        }
    }
}
