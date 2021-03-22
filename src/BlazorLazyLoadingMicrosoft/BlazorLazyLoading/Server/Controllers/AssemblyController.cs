using BlazorLazyLoading.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorLazyLoading.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AssemblyController : ControllerBase
    {
        private const string BinaryFolder = "Assemblies";
        private readonly ILogger<AssemblyController> _logger;

        public AssemblyController(ILogger<AssemblyController> logger)
        {
            _logger = logger;
        }

        [HttpGet()]
        [Route("{assemblyFullName}")]
        public async Task<ActionResult> Get(string assemblyFullName)
        {
            var filePath = Path.Combine(BinaryFolder, assemblyFullName + ".zip");
            if (System.IO.File.Exists(filePath) == false)
            {
                return NotFound();
            }

            var file = await System.IO.File.ReadAllBytesAsync(filePath);

            return File(file, "application/zip");
        }
    }
}
