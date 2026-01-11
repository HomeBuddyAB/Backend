using HomeBuddy_API.Interfaces.AdminInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeBuddy_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminsController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminsController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(int page = 1)
        {
            var admins = await _adminService.GetAllAdminsAsync(page);
            return Ok(admins);
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var count = await _adminService.GetAdminCountAsync();
            return Ok(new { count });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var admin = await _adminService.GetAdminByIdAsync(id);
            if (admin == null) return NotFound("Admin not found");
            return Ok(admin);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] string username, [FromQuery] string password)
        {
            try
            {
                await _adminService.CreateAdminAsync(username, password);
                return Ok("Admin created successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}/password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromQuery] string currentPassword, [FromQuery] string newPassword)
        {
            try
            {
                await _adminService.UpdateAdminPasswordAsync(id, currentPassword, newPassword);
                return Ok("Password updated successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] string confirmPassword)
        {
            try
            {
                await _adminService.DeleteAdminAsync(id, confirmPassword);
                return Ok("Admin deleted successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

// M.B