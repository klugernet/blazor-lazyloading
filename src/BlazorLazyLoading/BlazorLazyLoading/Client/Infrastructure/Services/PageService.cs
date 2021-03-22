using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using BlazorLazyLoading.Shared;
using Microsoft.AspNetCore.Components;

namespace BlazorLazyLoading.Client.Services
{
    public class PageService
    {
        private AssemblyService _assemblyService;
        private Dictionary<string, Type> _pageTypes = null;


        public PageService(AssemblyService assemblyService)
        {
            _assemblyService = assemblyService;
        }

        public async Task<Type> GetPageTypeByRoute(RouteDefinition route)
        {
            Initialize();

            if (_pageTypes.ContainsKey(route.TypeFullName))
            {
                return _pageTypes[route.TypeFullName];
            }

            //2nd try - perhaps Assembly was not loaded last time the assemblies were analyzed
            Initialize(true);

            if (_pageTypes.ContainsKey(route.TypeFullName))
            {
                return _pageTypes[route.TypeFullName];
            }

            if (route.LazyLoad == false)
            {
                return null;
            }

            //try to load assembly from server
            var assembly = await _assemblyService.LoadAssembly(route.AssemblyName);
            if (assembly != null)
            {
                var types = assembly.GetTypes()
                    .Where(x =>
                        x.IsInterface == false &&
                        x.IsAbstract == false &&
                        x.IsAssignableTo(typeof(ComponentBase)));
                AddTypes(types);
            }

            if (_pageTypes.ContainsKey(route.TypeFullName))
            {
                return _pageTypes[route.TypeFullName];
            }

            return null;
        }

        private void Initialize(bool reload = false)
        {
            var loadedAssemblies = _assemblyService.GetLoadedAssemblies(reload);

            if (_pageTypes == null || _pageTypes.Count < 1 || reload == true)
            {
                //var types = loadedAssemblies
                //    .SelectMany(x => x.GetTypes())
                //        .Where(x =>
                //            x.IsInterface == false &&
                //            x.IsAbstract == false &&
                //            x.IsAssignableTo(typeof(ComponentBase)));

                var assemblies = loadedAssemblies;
                var types = assemblies.SelectMany(x => x.GetTypes());
                var filteredTypes = new List<Type>();
                foreach (var type in types)
                {
                    if (type.IsAbstract == false &&
                        type.IsInterface == false &&
                        type.IsAssignableTo(typeof(ComponentBase))) //perhaps not correct type
                    {
                        filteredTypes.Add(type);
                    }
                }

                _pageTypes = new Dictionary<string, Type>();
                AddTypes(filteredTypes);
            }
        }

        private void AddTypes(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                if (Activator.CreateInstance(type) is ComponentBase instance)
                {
                    //if ComponentBase is exchanged by an own type, this is the point, where init methods can be called 
                    _pageTypes.Add(type.FullName ?? type.Name, type);
                }
            }
        }
    }
}
