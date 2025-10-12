using Microsoft.AspNetCore.Mvc;

namespace MiloApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase { }
}
