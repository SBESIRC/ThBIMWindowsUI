using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THBimEngine.IO.GFC2;

namespace XbimXplorer.Deduct.Model
{
    public abstract class GFCElementModel
    {
        private ThGFC2Document _gfcDoc;
        private string _matirial = "";

        /// <summary>
        /// 是否是构建元素(T:构建，F:图元)
        /// </summary>
        public bool IsConstruct { get; set; }

        /// <summary>
        /// GFC ID
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// 包含图元(构建-图元)
        /// </summary>
        public List<GFCElementModel> Primitives { get; set; }

        /// <summary>
        /// 关系图元(图元-图元)
        /// </summary>
        public List<GFCElementModel> RelationElements { get; set; }

        /// <summary>
        /// Model
        /// </summary>
        public DeductGFCModel Model { get; set; }

        public GFCElementModel(ThGFC2Document gfcDoc, string matirial)
        {
            Primitives = new List<GFCElementModel>();
            RelationElements = new List<GFCElementModel>();
            _gfcDoc = gfcDoc;
            _matirial = matirial;
            ID = -1;
        }
    }
}
