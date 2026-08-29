using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PC_Parts_Scrapper.Data;
using System.Security.Claims;

namespace PC_Parts_Scrapper.Controllers
{
	[ApiController]
	[Route("api/notifications")]
	[Authorize]
	public class NotificationsApiController : ControllerBase
	{
		private readonly PcPartsContext _context;

		public NotificationsApiController(PcPartsContext context)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<IActionResult> GetMyNotifications()
		{
			var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (userId == null) return Unauthorized();

			var notifications = await _context.Notifications
				.Where(n => n.UserId == userId && !n.IsRead)
				.OrderByDescending(n => n.CreatedAt)
				.ToListAsync();

			return Ok(notifications);
		}

		[HttpPost("{id}/read")]
		public async Task<IActionResult> MarkAsRead(int id)
		{
			var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (userId == null) return Unauthorized();

			var notification = await _context.Notifications
				.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

			if (notification == null) return NotFound();

			notification.IsRead = true;
			await _context.SaveChangesAsync();

			return NoContent();
		}
	}
}