﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xbim.Ifc;
using ifc4 = Xbim.Ifc4;
using ifc23 = Xbim.Ifc2x3;

using THBimEngine.Application;
using ThBIMServer.NTS;
using THBimEngine.Domain;

namespace XbimXplorer.Deduct
{
    public class DeductEngine
    {

        private THDocument currDoc;
        private THBimProject ArchiProject;
        private THBimProject StructProject;

        public DeductEngine(THDocument currDoc)
        {
            this.currDoc = currDoc;
            var sProject = currDoc.AllBimProjects.Where(x => x.Major == EMajor.Structure).FirstOrDefault();
            var aProject = currDoc.AllBimProjects.Where(x => x.Major == EMajor.Architecture && x.ApplcationName == EApplcationName.CAD).FirstOrDefault();
           
            StructProject = sProject;
            ArchiProject = aProject;
        }

        public void Do()
        {
            if (!CheckProjetInvalid())
            {
                return;
            }

            var ifcStore = StructProject.SourceProject as Xbim.Ifc.IfcStore;

            /////////////////////////////////////////
            ////if (ifcStore.IfcSchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc4)
            ////{
            ////    // GetSpIdxIfc4(ifcStore);
            ////    TryIfc4(ifcStore);
            ////}
            ////else if (ifcStore.IfcSchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3)
            ////{
            ////    TryIfc23(ifcStore);
            ////}
            /////////////////////////////////////////


            if (ifcStore.IfcSchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3)
            {
                var engine = new DeductEngine23();
                engine.IfcStore = ifcStore;
                engine.ArchiProject = ArchiProject;
                engine.DeductIFC23Engine();
            }
            else if (ifcStore.IfcSchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc4)
            {
                DeductIFC4Engine(ifcStore);
            }
        }

        private static void TryIfc4(Xbim.Ifc.IfcStore ifcStore)
        {
            ifc4.SharedBldgElements.IfcWall w111 = null;
            var project = ifcStore.Instances.FirstOrDefault<ifc4.Kernel.IfcProject>();
            var buildlings = project.Sites.FirstOrDefault()?.Buildings.FirstOrDefault() as ifc4.ProductExtension.IfcBuilding;
            var storeys = buildlings.BuildingStoreys.OfType<ifc4.ProductExtension.IfcBuildingStorey>().ToList();

            foreach (ifc4.ProductExtension.IfcBuildingStorey BuildingStorey in storeys)
            {
                foreach (var containElement in BuildingStorey.ContainsElements)
                {
                    var walls = containElement.RelatedElements.OfType<ifc4.SharedBldgElements.IfcWall>().ToList();
                    foreach (var item in walls)
                    {
                        if (item.GlobalId == "3bJmGpFz8HxO2j0AOO8G05")
                        {
                            w111 = item;
                            break;
                        }
                    }
                }
            }
            if (w111 != null)
            {
                var testProf = ((ifc4.GeometricModelResource.IfcSweptAreaSolid)w111.Representation.Representations[0].Items[0]).SweptArea;
                var testPosition = ((ifc4.GeometricModelResource.IfcSweptAreaSolid)w111.Representation.Representations[0].Items[0]).Position;
                var testPlacement = ((ifc4.GeometricConstraintResource.IfcLocalPlacement)w111.ObjectPlacement).RelativePlacement;

                var aaageom = ThIFCNTSExtension4.ToNTSLineString(testProf, testPlacement);
            }
        }

        private static void TryIfc23(Xbim.Ifc.IfcStore ifcStore)
        {
            Xbim.Ifc2x3.SharedBldgElements.IfcWall w111 = null;
            var project = ifcStore.Instances.FirstOrDefault<Xbim.Ifc2x3.Kernel.IfcProject>();
            var buildlings = project.Sites.FirstOrDefault()?.Buildings.FirstOrDefault() as Xbim.Ifc2x3.ProductExtension.IfcBuilding;
            var storeys = buildlings.BuildingStoreys.OfType<Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey>().ToList();

            foreach (Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey BuildingStorey in storeys)
            {
                foreach (var containElement in BuildingStorey.ContainsElements)
                {
                    var walls = containElement.RelatedElements.OfType<Xbim.Ifc2x3.SharedBldgElements.IfcWall>().ToList();
                    foreach (var item in walls)
                    {
                        if (item.GlobalId == "3bJmGpFz8HxO2j0AOO8G05")
                        {
                            w111 = item;
                            break;
                        }
                    }
                }
            }
            if (w111 != null)
            {
                var testProf = ((Xbim.Ifc2x3.GeometricModelResource.IfcSweptAreaSolid)w111.Representation.Representations[0].Items[0]).SweptArea;
                var testPosition = ((Xbim.Ifc2x3.GeometricModelResource.IfcSweptAreaSolid)w111.Representation.Representations[0].Items[0]).Position;
                var testPlacement = ((Xbim.Ifc2x3.GeometricConstraintResource.IfcLocalPlacement)w111.ObjectPlacement).RelativePlacement;

                var aaageom = ThIFCNTSExtension.ToNTSLineString(testProf, testPlacement);
            }
        }

        private static void TryIfc23Bim(Xbim.Ifc.IfcStore ifcStore)
        {
            Xbim.Ifc2x3.SharedBldgElements.IfcBeam b111 = null;

            var project = ifcStore.Instances.FirstOrDefault<Xbim.Ifc2x3.Kernel.IfcProject>();
            var buildlings = project.Sites.FirstOrDefault()?.Buildings.FirstOrDefault() as Xbim.Ifc2x3.ProductExtension.IfcBuilding;
            var storeys = buildlings.BuildingStoreys.OfType<Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey>().ToList();

            foreach (var buildingStorey in storeys)
            {
                foreach (var containElement in buildingStorey.ContainsElements)
                {
                    var beams = containElement.RelatedElements.OfType<Xbim.Ifc2x3.SharedBldgElements.IfcBeam>().ToList();
                    foreach (var item in beams)
                    {
                        if (item.GlobalId == "1pULpiaZ54LBwxhA09Wmim")
                        {
                            b111 = item;
                            break;
                        }
                    }

                }
            }

            if (b111 != null)
            {
                var testProf = ((Xbim.Ifc2x3.GeometricModelResource.IfcSweptAreaSolid)b111.Representation.Representations[0].Items[0]).SweptArea;
                var testPosition = ((Xbim.Ifc2x3.GeometricModelResource.IfcSweptAreaSolid)b111.Representation.Representations[0].Items[0]).Position;
                var testPlacement = ((Xbim.Ifc2x3.GeometricConstraintResource.IfcLocalPlacement)b111.ObjectPlacement).RelativePlacement;

                var aaageom = ThIFCNTSExtension.ToNTSLineString(testProf, testPlacement);
            }

        }

        private void DeductIFC4Engine(Xbim.Ifc.IfcStore ifcStore)
        {
            var project = ifcStore.Instances.FirstOrDefault<ifc4.Kernel.IfcProject>();
            var buildlings = project.Sites.FirstOrDefault()?.Buildings.FirstOrDefault() as ifc4.ProductExtension.IfcBuilding;
            var struStoreys = buildlings.BuildingStoreys.OfType<ifc4.ProductExtension.IfcBuildingStorey>().ToList();

            var storeyWallDict = GetSpIdxIfc4(struStoreys);

            var wlist = new List<THBimWall>();
            var storeyDict = new Dictionary<string, Dictionary<THBimWall, List<Tuple<ifc4.ProfileResource.IfcProfileDef, ifc4.GeometryResource.IfcAxis2Placement>>>>();

            foreach (var building in ArchiProject.ProjectSite.SiteBuildings)
            {
                foreach (var storey in building.Value.BuildingStoreys)
                {
                    var wChange = new Dictionary<THBimWall, List<Tuple<ifc4.ProfileResource.IfcProfileDef, ifc4.GeometryResource.IfcAxis2Placement>>>();
                    storeyDict.Add(storey.Key, wChange);
                    var storeyItem = storey.Value;
                    if (storeyItem.MemoryStoreyId == string.Empty || storeyItem.MemoryStoreyId == "")
                    {
                        //标准层第一层或非标层

                        var sStorey = struStoreys.Where(x =>
                        {
                            double elevation = x.Elevation.Value;
                            if (Math.Abs(elevation - storeyItem.Elevation) <= 50)
                                return true;
                            else
                                return false;
                        }).FirstOrDefault();

                        if (sStorey != null)
                        {
                            storeyWallDict.TryGetValue(sStorey, out var spIdx);

                            if (spIdx != null)
                            {
                                foreach (var w in storeyItem.FloorEntitys)
                                {
                                    if (w.Value is THBimWall wallItem)
                                    {
                                        wlist.Add(wallItem);
                                        var selectItem = spIdx.SelectCrossingPolygon(wallItem.GeometryParam as GeometryStretch);
                                        if (selectItem.Count > 0)
                                        {
                                            wChange.Add(wallItem, selectItem);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }


        }
        private static Dictionary<ifc4.ProductExtension.IfcBuildingStorey, ThIFCNTSSpatialIndex4> GetSpIdxIfc4(List<ifc4.ProductExtension.IfcBuildingStorey> storeys)
        {
            var storeyWallDict = new Dictionary<ifc4.ProductExtension.IfcBuildingStorey, ThIFCNTSSpatialIndex4>();
            foreach (ifc4.ProductExtension.IfcBuildingStorey BuildingStorey in storeys)
            {
                var struProfileInfos = new List<Tuple<ifc4.ProfileResource.IfcProfileDef, ifc4.GeometryResource.IfcAxis2Placement>>();
                foreach (var containElement in BuildingStorey.ContainsElements)
                {
                    var walls = containElement.RelatedElements.OfType<ifc4.SharedBldgElements.IfcWall>().ToList();
                    foreach (var item in walls)
                    {
                        if (item.Representation.Representations[0].Items[0] is ifc4.GeometricModelResource.IfcSweptAreaSolid)
                        {
                            var profile = ((ifc4.GeometricModelResource.IfcSweptAreaSolid)item.Representation.Representations[0].Items[0]).SweptArea;
                            var placement = ((ifc4.GeometricConstraintResource.IfcLocalPlacement)item.ObjectPlacement).RelativePlacement;
                            var wTuple = Tuple.Create(profile, placement);
                            struProfileInfos.Add(wTuple);
                        }
                        else
                        {
                            var area = item.Representation.Representations[0].Items[0];
                        }

                    }
                }
                var spIdx = new ThIFCNTSSpatialIndex4(struProfileInfos);
                storeyWallDict.Add(BuildingStorey, spIdx);
            }

            return storeyWallDict;
        }

        private bool CheckProjetInvalid()
        {
            if (ArchiProject == null || StructProject == null)
            {
                return false;
            }
            if (ArchiProject.Major != EMajor.Architecture)
            {
                return false;
            }
            if (StructProject.Major != EMajor.Structure)
            {
                return false;
            }
            if (ArchiProject.ApplcationName != EApplcationName.CAD)
            {
                return false;
            }

            return true;
        }
    }
}
