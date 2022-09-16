using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THBimEngine.Domain;
using THBimEngine.Domain.MidModel;

namespace XbimXplorer.ThBIMEngine
{
    public class ThBimCutData
    {
        public static void Run()
        {
            var ifcProjects = THBimScene.Instance.AllBimProjects;
            bool firstPr = true;
            var tempData = new TempModel();

            foreach (var project in ifcProjects)
            {
                if(firstPr)
                {
                    tempData.ModelConvert(project);
                    firstPr = false;
                }
                else
                {
                    tempData.AddProject(project);
                }
            }
            tempData.WriteMidFile();
        }
    }
}
