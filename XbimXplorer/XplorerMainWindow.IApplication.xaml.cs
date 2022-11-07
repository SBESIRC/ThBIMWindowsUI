using System;
using System.Linq;
using THBimEngine.Application;
using THBimEngine.Domain;

namespace XbimXplorer
{
    public partial class XplorerMainWindow
    {
        public THBimScene CurrentScene { get; set; }
        public THDocument CurrentDocument { get; set; }
        public THDocumentManager DocumentManager { get; set; }
        public event EventHandler SelectEntityChanged;
        private void InitDocument() 
        {
            this.DocumentManager = new THDocumentManager(ProgressChanged);

            var testProject = new THDocument(Guid.NewGuid().ToString(), "测试", ProgressChanged,Log);
            testProject.UnShowEntityTypes.Add("open");
            testProject.UnShowEntityTypes.Add(typeof(THBimOpening).Name);
            testProject.UnShowEntityTypes.Add("hole");
            DocumentManager.AddNewDoucment(testProject);
            DocumentManager.SelectDocumentChanged += DocumentManage_SelectDocumentChanged;
            DocumentManager.CurrentDocument = DocumentManager.AllDocuments.FirstOrDefault();
            
        }

        private void DocumentManage_SelectDocumentChanged(object sender, EventArgs e)
        {
            CurrentDocument = DocumentManager.CurrentDocument;
            if (null == CurrentDocument)
                return;
            this.CurrentDocument.DocumentChanged += CurrentDocument_DocumentChanged;
        }

        private void CurrentDocument_DocumentChanged(object sender, EventArgs e)
        {
            RenderScene();
        }
    }

}
