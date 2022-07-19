using System;

namespace THBimEngine.Domain
{
    internal class THBimDoor : THBimEntity, ICloneable
    {
        public string Opening { get; set; }

        public object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
