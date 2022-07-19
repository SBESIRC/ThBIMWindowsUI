using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THBimEngine.Domain
{
    public abstract class ITHBimElement
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Des { get; set; }
    }
}
