using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using THBimEngine.Domain;
using THBimEngine.Domain.MidModel;
using Xbim.Ifc;
using Xbim.IO.Esent;
using Xbim.ModelGeometry.Scene;

namespace XbimXplorer.ThBIMEngine
{
    public class ThBimCutData
    {
        public static void Run(List<THBimProject> bimProjects)
        {
            bool firstPr = true;
            var tempData = new TempModel();
            foreach (var project in bimProjects)
            {
                if (firstPr)
                {
                    tempData.ModelConvert(project);
                    firstPr = false;
                }
                else
                {
                    tempData.AddProject(project);
                }
            }
            tempData.WriteMidFile(bimProjects.First().ProjectIdentity);
        }

        public static void Run(IfcStore ifcStore, List<GeometryMeshModel> allGeoModels, List<PointNormal> allGeoPointNormals)
        {
            var tempData = new TempModel();
            var resList = new Dictionary<string, GeometryMeshModel>();
            foreach (var item in allGeoModels)
            {
                resList.Add(item.EntityLable, item);
            }
            tempData.GetIfcFile(ifcStore, resList, allGeoPointNormals);
            tempData.WriteMidFile(ifcStore.FileName);
        }
        public static void Run(IfcStore ifcStore, List<GeometryMeshModel> allGeoModels, List<PointNormal> allGeoPointNormals,
            IfcStore ifcStorePC, List<GeometryMeshModel> allGeoModelsPC, List<PointNormal> allGeoPointNormalsPC)
        {
            var tempData = new TempModel();
            var resList = new Dictionary<string, GeometryMeshModel>();
            foreach (var item in allGeoModels)
            {
                resList.Add(item.EntityLable, item);
            }
            tempData.GetIfcFile(ifcStore, resList, allGeoPointNormals);
            var resListPC = new Dictionary<string, GeometryMeshModel>();
            foreach (var item in allGeoModelsPC)
            {
                resListPC.Add(item.EntityLable, item);
            }
            tempData.AddIfcFile(ifcStorePC, resListPC, allGeoPointNormalsPC);
            tempData.WriteMidFile(ifcStore.FileName);
        }

        public static IfcStore GetIfcStore(string ifcFileName)
        {
            var model = IfcStore.Open(ifcFileName, null, null, null, XbimDBAccess.Read);
            if (model.GeometryStore.IsEmpty)
            {
                try
                {
                    var context = new Xbim3DModelContext(model);
                    context.UseSimplifiedFastExtruder = false;
                    context.CreateContext(null, App.ContextWcsAdjustment);
                }
                catch (Exception ex)
                {
                    ;
                }
            }
            return model;
        }
    }
}
