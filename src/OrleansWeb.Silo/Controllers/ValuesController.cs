using System.Collections.Generic;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc;

#pragma warning disable CA1801 // Remove unused parameter
#pragma warning disable SCS0012
namespace OrleansWeb.Silo.Controllers
{
	[Route("api/values")]
	[ApiController]
	public sealed class ValuesController : ControllerBase
	{
		// GET api/values
		[HttpGet]
		public ActionResult<IEnumerable<string>> Get()
		{
			return new[] { "value1", "value2" };
		}

		// GET api/values/5
		[HttpGet("{id:int}")]
		public ActionResult<string> Get(int id)
		{
			return "value";
		}

		// POST api/values
		[HttpPost("/googlebm/callback")]
		public void Post([FromBody] JsonDocument value)
		{
		}

		// PUT api/values/5
		[HttpPut("{id:int}")]
		public void Put(int id, [FromBody] string value)
		{
		}

		// DELETE api/values/5
		[HttpDelete("{id:int}")]
		public void Delete(int id)
#pragma warning restore CA1801 // Remove unused parameter
		{
		}
	}
}
