using BlazorLazyLoading.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorLazyLoading.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RouteController : ControllerBase
    {
        private static readonly List<RouteDefinition> Routes = new List<RouteDefinition>()
        {
            new RouteDefinition()
            {
                Path = "/",
                TypeFullName = "BlazorLazyLoading.Client.Pages.Index",
                LazyLoad = false
            },
            new RouteDefinition()
            {
                Path="StaticLink",
                TypeFullName = "BlazorLazyLoading.Modules.StaticLinkedPages.StaticLinkPage",
                LazyLoad = false
            },
            new RouteDefinition()
            {
                Path = "LazyLoaded",
                TypeFullName = "BlazorLazyLoading.Modules.LazyLoadedPages.LazyLoadedPage",
                LazyLoad = true,
                AssemblyName = "BlazorLazyLoading.Modules.LazyLoadedPages"
            }
        };

        private readonly ILogger<RouteController> _logger;

        public RouteController(ILogger<RouteController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public List<RouteDefinition> Get()
        {
            return Routes;
        }
    }
}
