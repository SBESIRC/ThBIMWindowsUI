using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THBimEngine.Domain
{
    public abstract class THBimElement: ICloneable
    {
        /// <summary>
        /// Guid
        /// </summary>
        public string Uid { get; set; }
        /// <summary>
        /// 索引Id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string Describe { get; set; }
        /// <summary>
        /// 属性
        /// </summary>
        public Dictionary<string, object> Properties { get; }
        public THBimElement(int id, string name, string describe = "", string uid = "") 
        {
        
            if(string.IsNullOrEmpty(uid))
                Uid = System.Guid.NewGuid().ToString();
            Uid = uid;
            Id = id;
            Name = name;
            Describe = describe;
            Properties = new Dictionary<string, object>();
        }
        public abstract object Clone();
    }
}
