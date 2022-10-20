using System.Collections.Generic;
using System.Linq;
using THBimEngine.Domain;
using THBimEngine.Domain.MidModel;

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
    }
}
