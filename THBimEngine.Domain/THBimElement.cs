using System;
using System.Collections.Generic;
using Xbim.Common.Geometry;

namespace THBimEngine.Domain
{
    /// <summary>
    /// 元素基类
    /// </summary>
    public abstract class THBimElement: ICloneable
    {
        /// <summary>
        /// Guid
        /// </summary>
        public string Uid { get; set; }
        /// <summary>
        /// 索引Id(目前不能保证唯一性)
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 类别名称
        /// </summary>
        public virtual string FriendlyTypeName { get { return this.GetType().Name.ToString(); } }
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
        public XbimMatrix3D Matrix3D { get; set; }
        public THBimElement(int id, string name, string describe = "", string uid = ""):this(id,name,"",uid,describe)
        {
            Matrix3D = XbimMatrix3D.CreateTranslation(XbimVector3D.Zero);
        }
        public THBimElement(int id, string name, string parentUid, string uid,string describe)
        {
            ParentUid = parentUid;
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
                //this.Name.Equals(other.Name) &&
                this.ParentUid.Equals(other.ParentUid))
            {
                return true;
            }
            return false;
        }
    }
}
