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
        public RouteService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        
        public async Task<List<RouteDefinition>> GetRoutes()
        {
            return await _httpClient.GetFromJsonAsync<List<RouteDefinition>>("Route");
        }
    }
}
