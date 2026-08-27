using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PC_Parts_Scrapper.Data;
using PC_Parts_Scrapper.Models;
using PC_Parts_Scrapper.ViewModels;
using System.Security.Claims;

namespace PC_Parts_Scrapper.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AlertsController : ControllerBase
    { 
            private readonly PcPartsContext _context;
            private readonly UserManager<ApplicationUser> _userManager;

            public AlertsController(PcPartsContext context, UserManager<ApplicationUser> userManager)
            {
                _context = context;
                _userManager = userManager;
            }

            [HttpGet]
            public async Task<IActionResult> GetMyAlerts()
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                var alerts = await _context.PriceAlerts
                    .Where(a => a.UserId == userId)
                    .Include(a => a.Product)
                    .ToListAsync();

                return Ok(alerts);
            }

            [HttpPost]
            public async Task<IActionResult> CreateAlert([FromBody] CreateAlert dto)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                // Check if alert already exists for this product
                var existing = await _context.PriceAlerts
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.ProductId == dto.ProductId && !a.IsActive);

                if (existing != null)
                {
                    return BadRequest(new { message = "You already have an active alert for this product." });
                }

                var alert = new PriceAlert
                {
                    UserId = userId,
                    ProductId = dto.ProductId,
                    TargetPrice = dto.TargetPrice,
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PriceAlerts.Add(alert);
                await _context.SaveChangesAsync();

                return Ok(alert);
            }

            [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteAlert(int id)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                var alert = await _context.PriceAlerts
                    .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

                if (alert == null) return NotFound();

                _context.PriceAlerts.Remove(alert);
                await _context.SaveChangesAsync();

                return NoContent();
            }
    }
} 
