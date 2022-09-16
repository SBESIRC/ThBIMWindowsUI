using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;

namespace THBimEngine.Domain
{
    public class THBimIFCEntity : THBimEntity, IEquatable<THBimIFCEntity>
    {
        public IPersistEntity IfcEntity { get; }
        public THBimIFCEntity(IPersistEntity entity) : this(entity.EntityLabel, entity.ExpressType.ExpressName,"", null) 
        {
            IfcEntity = entity;
        }
        public THBimIFCEntity(int id, string name, string material, GeometryParam geometryParam, string describe = "", string uid = "") : base(id, name, material, geometryParam, describe, uid)
        {
        }
        public override string FriendlyTypeName 
        {
            /*
             * IFC数据有好几种数据格式
             * 比如墙 IIfcWall,
             */
            get 
            { 
                if (null == IfcEntity)
                    return "";
                var typeStr = IfcEntity.ExpressType.ExpressName.ToLower();
                if (typeStr.Contains("wall"))
                    return "wall";
                if (typeStr.Contains("window"))
                    return "window";
                if (typeStr.Contains("door"))
                    return "door";
                if (typeStr.Contains("beam"))
                    return "beam";
                if (typeStr.Contains("slabe"))
                    return "slabe";
                if (typeStr.Contains("railing"))
                    return "railing";
                return typeStr.Replace("ifc", "");
            }
        }
        public override object Clone()
        {
            throw new NotImplementedException();
        }

        public bool Equals(THBimIFCEntity other)
        {
            throw new NotImplementedException();
        }
    }
}
