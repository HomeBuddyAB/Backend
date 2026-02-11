using HomeBuddy_API.Data;
using HomeBuddy_API.DTOs.Requests.CustomerDTOs;
using HomeBuddy_API.DTOs.Responses;
using HomeBuddy_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HomeBuddy_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "User")]
    public class CustomersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public CustomersController(ApplicationDbContext db)
        {
            _db = db;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CustomerResponse>), 200)]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var userId = GetUserId();
            var list = await _db.SavedCustomers
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedUtc)
                .Select(c => new CustomerResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Email = c.Email,
                    Phone = c.Phone,
                    StreetAddress = c.StreetAddress,
                    City = c.City,
                    PostalCode = c.PostalCode,
                    CountryCode = c.CountryCode,
                    CreatedUtc = c.CreatedUtc
                })
                .ToListAsync(ct);
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CustomerResponse), 200)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var userId = GetUserId();
            var c = await _db.SavedCustomers
                .Where(x => x.Id == id && x.UserId == userId)
                .Select(x => new CustomerResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    Email = x.Email,
                    Phone = x.Phone,
                    StreetAddress = x.StreetAddress,
                    City = x.City,
                    PostalCode = x.PostalCode,
                    CountryCode = x.CountryCode,
                    CreatedUtc = x.CreatedUtc
                })
                .FirstOrDefaultAsync(ct);
            if (c == null) return NotFound();
            return Ok(c);
        }

        [HttpPost]
        [ProducesResponseType(typeof(CustomerResponse), 201)]
        public async Task<IActionResult> Create([FromBody] CustomerCreateDto dto, CancellationToken ct)
        {
            var userId = GetUserId();
            var entity = new SavedCustomer
            {
                UserId = userId,
                Name = dto.Name.Trim(),
                Email = dto.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
                StreetAddress = string.IsNullOrWhiteSpace(dto.StreetAddress) ? null : dto.StreetAddress.Trim(),
                City = string.IsNullOrWhiteSpace(dto.City) ? null : dto.City.Trim(),
                PostalCode = string.IsNullOrWhiteSpace(dto.PostalCode) ? null : dto.PostalCode.Trim(),
                CountryCode = string.IsNullOrWhiteSpace(dto.CountryCode) ? null : dto.CountryCode.Trim().ToUpperInvariant()
            };
            _db.SavedCustomers.Add(entity);
            await _db.SaveChangesAsync(ct);
            var response = new CustomerResponse
            {
                Id = entity.Id,
                Name = entity.Name,
                Email = entity.Email,
                Phone = entity.Phone,
                StreetAddress = entity.StreetAddress,
                City = entity.City,
                PostalCode = entity.PostalCode,
                CountryCode = entity.CountryCode,
                CreatedUtc = entity.CreatedUtc
            };
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerUpdateDto dto, CancellationToken ct)
        {
            var userId = GetUserId();
            var entity = await _db.SavedCustomers
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
            if (entity == null) return NotFound();
            entity.Name = dto.Name.Trim();
            entity.Email = dto.Email.Trim();
            entity.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
            entity.StreetAddress = string.IsNullOrWhiteSpace(dto.StreetAddress) ? null : dto.StreetAddress.Trim();
            entity.City = string.IsNullOrWhiteSpace(dto.City) ? null : dto.City.Trim();
            entity.PostalCode = string.IsNullOrWhiteSpace(dto.PostalCode) ? null : dto.PostalCode.Trim();
            entity.CountryCode = string.IsNullOrWhiteSpace(dto.CountryCode) ? null : dto.CountryCode.Trim().ToUpperInvariant();
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var userId = GetUserId();
            var entity = await _db.SavedCustomers
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
            if (entity == null) return NotFound();
            _db.SavedCustomers.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
    }
}
