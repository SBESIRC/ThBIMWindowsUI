using System;
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
using XbimXplorer.Deduct.Model;

namespace XbimXplorer.Deduct
{
    public class DeductEngine
    {

        private THDocument currDoc;
        public THBimProject ArchiProject;
        public THBimProject StructProject;

        public Dictionary<string, DeductGFCModel> ModelList;

        public DeductEngine(THDocument currDoc)
        {
            this.currDoc = currDoc;
            var sProject = currDoc.AllBimProjects.Where(x => x.Major == EMajor.Structure && x.ApplcationName == EApplcationName.IFC).FirstOrDefault();
            //var aProject = currDoc.AllBimProjects.Where(x => x.Major == EMajor.Architecture && x.ApplcationName == EApplcationName.IFC).FirstOrDefault();

            var aProject = currDoc.AllBimProjects.Where(x => x.Major != EMajor.Structure && x.ApplcationName == EApplcationName.IFC).FirstOrDefault();

            StructProject = sProject;
            ArchiProject = aProject;
        }

        public void DoIfcVsIfc()
        {
            if (!CheckProjetInvalid())
            {
                return;
            }

            var archIfcStore = ArchiProject.SourceProject as Xbim.Ifc.IfcStore;
            var structIfcStore = StructProject.SourceProject as Xbim.Ifc.IfcStore;

            if (archIfcStore.IfcSchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3 && structIfcStore.IfcSchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3)
            {
                var build2D = new Build2DModelService();
                build2D.IfcStruct = structIfcStore;
                build2D.IfcArchi = archIfcStore;
                build2D.Build2DModel();

                var engine = new DeductEngineIfcVsIfc();
                engine.ModelList = build2D.ModelList;
                engine.DeductEngine();
                ModelList = engine.ModelList;
            }
            //Demo For zxr（这里是否有两个IFC4,两个2*3,一个2*3一个4...）
            else if (structIfcStore.IfcSchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc4)
            {
                //DeductIFC4Engine(structIfcStore);
            }
        }

        private bool CheckProjetInvalid()
        {
            if (ArchiProject == null || StructProject == null)
            {
                return false;
            }
            ////暂时注释掉
            //if (ArchiProject.Major != EMajor.Architecture)
            //{
            //    return false;
            //}
            if (StructProject.Major != EMajor.Structure)
            {
                return false;
            }
            if (ArchiProject.ApplcationName != EApplcationName.IFC )
            {
                return false;
            }

            return true;
        }
    }
}
