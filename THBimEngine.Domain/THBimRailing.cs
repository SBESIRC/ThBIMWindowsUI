﻿using System;

namespace THBimEngine.Domain
{
    public class THBimRailing : THBimEntity,IEquatable<THBimRailing>
    {
        public THBimRailing(int id,string name,string material,GeometryParam geometryParam, string describe,string uid):base(id,name, material,geometryParam, describe,uid)
        {
            
        }
        public override object Clone()
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(THBimRailing other)
        {
            if (!base.Equals(other)) return false;
            return true;
        }
    }
}
