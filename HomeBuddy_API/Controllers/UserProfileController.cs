using HomeBuddy_API.DTOs.Requests.User;
using HomeBuddy_API.Interfaces.UserInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeBuddy_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "User")]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserProfileController(IUserService userService) => _userService = userService;

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userService.GetOwnProfileAsync(GetUserId());
            if (user == null) return NotFound("User not found");
            return Ok(new { user.Id, user.Email });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UserUpdateDto dto)
        {
            try
            {
                await _userService.UpdateOwnProfileAsync(GetUserId(), dto);
                return Ok("Profile updated successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAccount([FromBody] UserDeleteDto dto)
        {
            try
            {
                await _userService.DeleteOwnAccountAsync(GetUserId(), dto);
                return Ok("Account deleted successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

// M.B