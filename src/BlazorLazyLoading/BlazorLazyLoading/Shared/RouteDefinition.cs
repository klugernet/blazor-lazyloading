using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorLazyLoading.Shared
{
    public class RouteDefinition
    {
        public string Path { get; set; }
        public bool LazyLoad { get; set; }
        public string TypeFullName { get; set; }
        public string AssemblyName { get; set; }

    }
}
