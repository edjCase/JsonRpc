using Microsoft.AspNetCore.Mvc;

namespace EdjCase.JsonRpc.Router.Sample.Controllers;

[ApiController]
[Route("[controller]")]
public class ApiController : Microsoft.AspNetCore.Mvc.ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var result = new
        {
            Id = id, 
            Description = $"This is an Api call {id}."
        };

        return (IActionResult)this.Ok(result);
    }
}

[RpcRoute("NonApi")]
public class NonApiController : RpcController
{

	public object GetById(int id)
	{
		var result = new
		{
			Id = id,
			Description = $"This is bar {id}."
		};

		return result;
	}
}