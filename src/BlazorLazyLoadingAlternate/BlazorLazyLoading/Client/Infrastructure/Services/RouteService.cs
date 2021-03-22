using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BlazorLazyLoading.Shared;

namespace BlazorLazyLoading.Client.Services
{
    public class RouteService
    {
        private HttpClient _httpClient;
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

        public RouteService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        
        public async Task<List<RouteDefinition>> GetRoutes()
        {
            //return await _httpClient.GetFromJsonAsync<List<RouteDefinition>>("/Routes");
            return Routes;
        }
    }
}
