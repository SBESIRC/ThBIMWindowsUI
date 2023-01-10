using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace THBimEngine.Application
{
    public class THDocumentManager
    {
        private event ProgressChangedEventHandler progressChanged;
        /// <summary>
        /// 所有Document
        /// </summary>
        public List<THDocument> AllDocuments { get; }
        /// <summary>
        /// 当前Document
        /// </summary>
        private THDocument document { get; set; }
        public THDocument CurrentDocument 
        {
            get { return document; }
            set 
            {
                document = value;
                SelectDocumentChanged.Invoke(document, null);
            }
        }
        public THDocumentManager(ProgressChangedEventHandler progress) 
        {
            progressChanged = progress;
            AllDocuments = new List<THDocument>();
            SelectDocumentChanged += THDocumentManage_SelectDocumentChanged;
        }

        private void THDocumentManage_SelectDocumentChanged(object sender, EventArgs e)
        {
            //自身实现一个空的响应事件
        }

        /// <summary>
        /// 新增Document
        /// </summary>
        /// <param name="newDocument"></param>
        public void AddNewDoucment(THDocument newDocument) 
        {
            if (null == newDocument)
                return;
            AllDocuments.Add(newDocument);
        }
        /// <summary>
        /// 移除Document
        /// </summary>
        /// <param name="docId"></param>
        public void RemoveDoucment(string docId) 
        {
            if (string.IsNullOrEmpty(docId))
                return;
            var rmDoc = AllDocuments.Where(c => c.DocumentId == docId).FirstOrDefault();
            RemoveDoucment(rmDoc);
        }
        /// <summary>
        /// 移除Document
        /// </summary>
        /// <param name="docId"></param>
        public void RemoveDoucment(THDocument rmDocument)
        {
            if (null == rmDocument)
                return;
            var rmIsCurrent = rmDocument == CurrentDocument;
            AllDocuments.Remove(rmDocument);
            if (rmIsCurrent)
                CurrentDocument = AllDocuments.FirstOrDefault();
        }
        public void RemoveAllDocument() 
        {
            AllDocuments.Clear();
            CurrentDocument = null;
        }
        /// <summary>
        /// Document切换事件
        /// </summary>
        public event EventHandler SelectDocumentChanged;
    }

    
}
