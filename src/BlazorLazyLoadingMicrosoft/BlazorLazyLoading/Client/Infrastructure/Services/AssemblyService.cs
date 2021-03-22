using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace BlazorLazyLoading.Client.Services
{
    public class AssemblyService
    {
        private const string ModuleIdentifier = "BlazorLazyLoading";
        private List<Assembly> _loadedAssemblies = null;
        private readonly HttpClient _httpClient;

        public AssemblyService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _loadedAssemblies = new List<Assembly>();
        }

        internal async Task<Assembly> LoadAssembly(string assemblyFullName)
        {
            if (_loadedAssemblies.Any(x => x.FullName == assemblyFullName) == true)
            {
                return _loadedAssemblies.First(x => x.FullName == assemblyFullName);
            }

            var assemblyZipBytes = await _httpClient.GetByteArrayAsync($"Assembly/{assemblyFullName}");

            using (ZipArchive archive = new ZipArchive(new MemoryStream(assemblyZipBytes)))
            {
                var dlls = new Dictionary<string, byte[]>();
                var pdbs = new Dictionary<string, byte[]>();

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    var name = Path.GetFileNameWithoutExtension(entry.Name);
                    if (_loadedAssemblies.Any(x => x.FullName == name) == false)
                    {
                        await using (var memoryStream = new MemoryStream())
                        {
                            await entry.Open().CopyToAsync(memoryStream);
                            byte[] file = memoryStream.ToArray();
                            switch (Path.GetExtension(entry.Name))
                            {
                                case ".dll":
                                    dlls.Add(name, file);
                                    break;
                                case ".pdb":
                                    pdbs.Add(name, file);
                                    break;
                                default:
                                    //TODO: handle additional assembly resources if needed
                                    break;
                            }
                        }
                    }
                }

                foreach (var dllKeyValuePair in dlls)
                {
                    Assembly loadedAssembly;
                    if (pdbs.ContainsKey(dllKeyValuePair.Key) == true)
                    {
                        loadedAssembly = AssemblyLoadContext.Default.LoadFromStream(
                            new MemoryStream(dllKeyValuePair.Value),
                            new MemoryStream(pdbs[dllKeyValuePair.Key]));
                    }
                    else
                    {
                        loadedAssembly = AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(dllKeyValuePair.Value));
                    }

                    if (loadedAssembly.FullName != null &&
                        loadedAssembly.FullName.Contains(ModuleIdentifier, StringComparison.OrdinalIgnoreCase))
                    {
                        _loadedAssemblies.Add(loadedAssembly);
                    }
                }
            }

            return _loadedAssemblies.FirstOrDefault(x => x.GetName().Name == assemblyFullName);
        }


    }
}
