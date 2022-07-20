using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THBimEngine.Domain
{
    public abstract class THBimElement: ICloneable, IEquatable<THBimElement>
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
        /// 父元素Id
        /// </summary>
        public string ParentUid { get; set; }
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
            Uid = uid;
            if (string.IsNullOrEmpty(uid))
                Uid = System.Guid.NewGuid().ToString();
            Id = id;
            Name = name;
            Describe = describe;
            Properties = new Dictionary<string, object>();
        }
        public abstract object Clone();

        public override int GetHashCode()
        {
            return Uid.GetHashCode() ^ Name.GetHashCode() ^ ParentUid.GetHashCode();
        }

        public bool Equals(THBimElement other)
        {
            if( this.Uid.Equals(other.Uid) &&
                this.Name.Equals(other.Name) &&
                this.ParentUid.Equals(other.ParentUid))
            {
                return true;
            }
            return false;
        }
    }
}
