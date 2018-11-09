using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace SampleDiagnosedApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly IHttpClientFactory _factory;

        public ValuesController(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        // GET api/values
        [HttpGet]
        public async Task<ActionResult<string>> Get()
        {
            // Everytime we call this controller, http client metrics must be available in
            // http://<host>:9303/metrics
            var client = _factory.CreateClient("JsonPlaceholder");
            var response = await client.GetAsync("/posts").ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<string>> Get(int id)
        {
            var client = _factory.CreateClient("JsonPlaceholder");
            var response = await client.GetAsync($"/todos/{id}").ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
    }
}
