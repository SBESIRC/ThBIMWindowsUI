using System.Collections.Generic;
using THBimEngine.IO.GFC2;

namespace XbimXplorer.Deduct.Model
{
    public abstract class GFCElementModel
    {
        //private ThGFC2Document _gfcDoc;
        //public string Matirial = "";

        public string Name { get; set; } = "";

        /// <summary>
        /// 是否是构建元素(T:构建，F:图元)
        /// </summary>
        public bool IsConstruct { get; set; }

        /// <summary>
        /// GFC ID
        /// GFC行数
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// 包含图元(构建-图元)
        /// </summary>
        public List<GFCElementModel> Primitives { get; set; }

        /// <summary>
        /// 父子关系关系图元(图元-图元)
        /// </summary>
        public List<GFCElementModel> RelationElements { get; set; }

        /// <summary>
        /// Model
        /// </summary>
        public DeductGFCModel Model { get; set; }

        public GFCElementModel(ThGFC2Document gfcDoc,int globalId, string name)
        {
            Primitives = new List<GFCElementModel>();
            RelationElements = new List<GFCElementModel>();
            //_gfcDoc = gfcDoc;
            Name = name;
            ID = -1;
        }

        public abstract void AddGFCItemToConstruct(List<GFCElementModel> constructList);


    }
}
