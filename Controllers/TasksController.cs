using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TaskTitan.Api.Dtos;
using TaskTitanData;
using TaskTitanData.Entities;
using Task = TaskTitanData.Entities.Task;

namespace TaskTitan.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly TaskTitanDbContext _context;
        private readonly IMapper _mapper;

        public TasksController(TaskTitanDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        private int? GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out var userId))
            {
                return userId;
            }
            return null;
        }

        private async Task<bool> IsUserMemberOfProject(int userId, int projectId)
        {
            return await _context.Projects
                .Where(p => p.Id == projectId)
                .AnyAsync(p => p.Users.Any(u => u.Id == userId));
        }
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<TaskDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasks(
            [FromQuery] int? projectId,
            [FromQuery] int? userId, // Atanan kullanıcı filtresi
            [FromQuery] TaskTitanData.Entities.TaskStatus? status,
            [FromQuery] string? dueDateStart,
            [FromQuery] string? dueDateEnd,
            [FromQuery] string? createdDateStart,
            [FromQuery] string? createdDateEnd)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized("Invalid user token.");

            try
            {
                // Önce temel sorguyu oluşturalım
                var query = _context.Tasks
                                    .Include(t => t.Project).ThenInclude(p => p.Users) // Yetkilendirme için gerekli
                                    .Include(t => t.AssignedUser)
                                    .Include(t => t.CreatedByUser)
                                    .AsQueryable(); // AsNoTracking kaldırıldı

                // Filtreleri uygula
                if (projectId.HasValue) query = query.Where(t => t.ProjectId == projectId.Value);
                if (userId.HasValue) query = query.Where(t => t.AssignedUserId == userId.Value);
                if (status.HasValue) query = query.Where(t => t.Status == status.Value);

                // Tarih filtrelerini güvenli parse et ve uygula
                DateTime minValidDate = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime maxValidDate = new DateTime(9000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                bool TryParseAndValidateDateUtc(string? d, string pN, out DateTime rUtc)
                { /* ... (parse kodu aynı) ... */
                    rUtc = default; if (string.IsNullOrEmpty(d)) return true;
                    if (DateTime.TryParseExact(d, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime p)) { rUtc = DateTime.SpecifyKind(p, DateTimeKind.Utc); if (rUtc >= minValidDate && rUtc <= maxValidDate) return true; ModelState.AddModelError(pN, $"Date '{d}' out of range."); return false; }
                    ModelState.AddModelError(pN, $"Invalid date format for {pN}: '{d}'."); return false;
                }
                if (!TryParseAndValidateDateUtc(dueDateStart, "dS", out var pDSU) || !TryParseAndValidateDateUtc(dueDateEnd, "dE", out var pDEU) || !TryParseAndValidateDateUtc(createdDateStart, "cS", out var pCSU) || !TryParseAndValidateDateUtc(createdDateEnd, "cE", out var pCEU)) return BadRequest(ModelState);
                if (!string.IsNullOrEmpty(dueDateStart)) query = query.Where(t => t.DueDate >= pDSU);
                if (!string.IsNullOrEmpty(dueDateEnd)) query = query.Where(t => t.DueDate < pDEU.AddDays(1));
                if (!string.IsNullOrEmpty(createdDateStart)) query = query.Where(t => t.CreatedAt >= pCSU);
                if (!string.IsNullOrEmpty(createdDateEnd)) query = query.Where(t => t.CreatedAt < pCEU.AddDays(1));

                // === Yetkilendirme filtresini EN SON uygula ===
                query = query.Where(t => t.Project.Users.Any(u => u.Id == currentUserId.Value));
                // =============================================

                // Sıralama ve sonuçları alma
                var tasks = await query.OrderByDescending(t => t.CreatedAt).ToListAsync(); // OrderBy sona alındı
                var taskDtos = _mapper.Map<List<TaskDto>>(tasks);
                return Ok(taskDtos);
            }
            catch (Exception ex) { /* ... (hata loglama) ... */ return StatusCode(500, "..."); }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TaskDto>> GetTask(int id)
        {
            var task = await _context.Tasks
                                     .Include(t => t.Project)
                                     .Include(t => t.AssignedUser)
                                     .Include(t => t.CreatedByUser)
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return NotFound();
            var taskDto = _mapper.Map<TaskDto>(task);
            return Ok(taskDto);
        }

        [HttpPost]
        [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TaskDto>> CreateTask(CreateTaskDto createTaskDto)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            var projectExists = await _context.Projects.AnyAsync(p => p.Id == createTaskDto.ProjectId);
            var userExists = await _context.Users.AnyAsync(u => u.Id == createTaskDto.AssignedUserId);
            if (!projectExists || !userExists) return BadRequest("Invalid ProjectId or AssignedUserId.");

            var newTask = _mapper.Map<Task>(createTaskDto);
            if (!string.IsNullOrEmpty(createTaskDto.Status) && Enum.TryParse<TaskTitanData.Entities.TaskStatus>(createTaskDto.Status, true, out var parsedStatus))
            { newTask.Status = parsedStatus; }
            else { newTask.Status = TaskTitanData.Entities.TaskStatus.Pending; }
            newTask.IsDaily = createTaskDto.IsDaily;
            newTask.CreatedById = currentUserId.Value;

            _context.Tasks.Add(newTask);
            await _context.SaveChangesAsync();

            var createdTask = await _context.Tasks
                                            .Include(t => t.Project).Include(t => t.AssignedUser).Include(t => t.CreatedByUser)
                                            .AsNoTracking().FirstOrDefaultAsync(t => t.Id == newTask.Id);
            if (createdTask == null) return Problem("Failed to retrieve created task after saving.");

            var taskDto = _mapper.Map<TaskDto>(createdTask);
            return CreatedAtAction(nameof(GetTask), new { id = taskDto.Id }, taskDto);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTask(int id, UpdateTaskDto updateTaskDto)
        {
            var taskToUpdate = await _context.Tasks.FindAsync(id);
            if (taskToUpdate == null) return NotFound();

            if (updateTaskDto.AssignedUserId.HasValue && !await _context.Users.AnyAsync(u => u.Id == updateTaskDto.AssignedUserId.Value)) { return BadRequest("Invalid AssignedUserId."); }
            if (updateTaskDto.ProjectId.HasValue && updateTaskDto.ProjectId != taskToUpdate.ProjectId && !await _context.Projects.AnyAsync(p => p.Id == updateTaskDto.ProjectId.Value)) { return BadRequest("Invalid ProjectId."); }

            _mapper.Map(updateTaskDto, taskToUpdate);

            _context.Entry(taskToUpdate).Property(t => t.Title).IsModified = updateTaskDto.Title != null;
            _context.Entry(taskToUpdate).Property(t => t.Description).IsModified = updateTaskDto.Description != null;
            _context.Entry(taskToUpdate).Property(t => t.DueDate).IsModified = updateTaskDto.DueDate.HasValue;
            _context.Entry(taskToUpdate).Property(t => t.Status).IsModified = updateTaskDto.Status.HasValue;
            _context.Entry(taskToUpdate).Property(t => t.AssignedUserId).IsModified = updateTaskDto.AssignedUserId.HasValue;
            _context.Entry(taskToUpdate).Property(t => t.ProjectId).IsModified = updateTaskDto.ProjectId.HasValue;
            _context.Entry(taskToUpdate).Property(t => t.IsDaily).IsModified = updateTaskDto.IsDaily.HasValue;
            _context.Entry(taskToUpdate).Property(t => t.CreatedById).IsModified = false;
            _context.Entry(taskToUpdate).Property(t => t.CreatedAt).IsModified = false;
            taskToUpdate.UpdatedAt = DateTime.UtcNow;
            _context.Entry(taskToUpdate).Property(t => t.UpdatedAt).IsModified = true;

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { if (!await _context.Tasks.AnyAsync(e => e.Id == id)) return NotFound(); else throw; }
            catch (DbUpdateException ex) { Console.WriteLine($"DbUpdateException: {ex.InnerException?.Message ?? ex.Message}"); return StatusCode(500, "Update error."); }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTask(int id)
        {
            // Yetkilendirme kontrolü (örneğin sadece oluşturan veya admin silebilir) eklenebilir
            var taskToDelete = await _context.Tasks.FindAsync(id);
            if (taskToDelete == null) return NotFound();
            _context.Tasks.Remove(taskToDelete);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}