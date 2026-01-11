using HomeBuddy_API.DTOs.Requests.AdminDashDTOs;
using HomeBuddy_API.Interfaces.UserInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeBuddy_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]

    public class UserManagementController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserManagementController(IUserService userService) => _userService = userService;

        [HttpGet]
        public async Task<IActionResult> GetAll(int page) => Ok(await _userService.GetAllUsersAsync(page));

        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var count = await _userService.GetUserCountAsync();
            return Ok(new { count });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            return user == null ? NotFound("User not found") : Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto updatedUser)
        {
            await _userService.UpdateUserAsync(id, updatedUser);
            return Ok("User updated");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _userService.DeleteUserAsync(id);
            return Ok("User deleted");
        }
    }
}

// M.B