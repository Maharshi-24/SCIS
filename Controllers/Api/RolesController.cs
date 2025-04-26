using Microsoft.AspNetCore.Mvc;
using SCIS.Services;
using System.Threading.Tasks;

namespace SCIS.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly UserService _userService;

        public RolesController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet("GetRolesByUserType")]
        public IActionResult GetRolesByUserType(string userType)
        {
            if (string.IsNullOrEmpty(userType))
            {
                return BadRequest("User type is required");
            }

            var roles = _userService.GetRolesForUserType(userType);
            return Ok(roles);
        }
    }
}
