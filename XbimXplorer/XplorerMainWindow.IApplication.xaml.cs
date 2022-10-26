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
        public THDocumentManage DocumentManage { get; set; }
        public event EventHandler SelectEntityChanged;
        private void InitDocument() 
        {
            this.DocumentManage = new THDocumentManage(ProgressChanged);

            var testProject = new THDocument(Guid.NewGuid().ToString(), "测试", ProgressChanged);
            testProject.UnShowEntityTypes.Add("open");
            testProject.UnShowEntityTypes.Add(typeof(THBimOpening).Name);
            testProject.UnShowEntityTypes.Add("hole");
            DocumentManage.AddNewDoucment(testProject);
            DocumentManage.SelectDocumentChanged += DocumentManage_SelectDocumentChanged;
            DocumentManage.CurrentDocument = DocumentManage.AllDocuments.FirstOrDefault();
            
        }

        private void DocumentManage_SelectDocumentChanged(object sender, EventArgs e)
        {
            CurrentDocument = DocumentManage.CurrentDocument;
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
