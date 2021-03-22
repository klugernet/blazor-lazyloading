using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorLazyLoading.Client.Infrastructure
{
    public class NavigationContext
    {
        internal NavigationContext(string path, CancellationToken cancellationToken)
        {
            Path = path;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// The target path for the navigation.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// The <see cref="CancellationToken"/> to use to cancel navigation.
        /// </summary>
        public CancellationToken CancellationToken { get; }
    }
}
