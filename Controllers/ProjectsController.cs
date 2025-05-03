using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TaskTitan.Api.Dtos;
using TaskTitanData;
using TaskTitanData.Entities; 

namespace TaskTitan.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly TaskTitanDbContext _context;
        private readonly IMapper _mapper;

        public ProjectsController(TaskTitanDbContext context, IMapper mapper)
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

        // GET: api/projects
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProjectDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            try
            {
                var projects = await _context.Projects
                                             .Include(p => p.Customer)
                                             .Include(p => p.Users)
                                             .Where(p => p.Users.Any(u => u.Id == currentUserId.Value)) 
                                             .OrderBy(p => p.Name)
                                             .AsNoTracking()
                                             .ToListAsync();

               
                var projectIds = projects.Select(p => p.Id).ToList();
                var taskCounts = await _context.Tasks
                                            .Where(t => projectIds.Contains(t.ProjectId)) 
                                            .GroupBy(t => t.ProjectId)
                                            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
                                            .ToDictionaryAsync(x => x.ProjectId, x => x.Count);

                var projectDtos = _mapper.Map<List<ProjectDto>>(projects);

                // DTO'ya görev sayısını ekle
                projectDtos.ForEach(dto => dto.TaskCount = taskCounts.GetValueOrDefault(dto.Id, 0));

                return Ok(projectDtos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting projects for user {currentUserId}: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving projects.");
            }
        }

        // GET: api/projects/5 
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProjectDto>> GetProject(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            var project = await _context.Projects
                                        .Include(p => p.Customer)
                                        .Include(p => p.Users)
                                        .Include(p => p.Tasks) 
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            // Yetki Kontrolü
            if (!project.Users.Any(u => u.Id == currentUserId.Value))
            {
                return Forbid();
            }

            var projectDto = _mapper.Map<ProjectDto>(project);
            projectDto.TaskCount = project.Tasks.Count; 

            return Ok(projectDto);
        }

        // POST: api/projects
        [HttpPost]
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProjectDto>> CreateProject(CreateProjectDto createProjectDto)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            var customerExists = await _context.Customers.AnyAsync(c => c.Id == createProjectDto.CustomerId);
            if (!customerExists) return BadRequest("Invalid CustomerId.");

            var newProject = _mapper.Map<Project>(createProjectDto);

            var creatorUser = await _context.Users.FindAsync(currentUserId.Value);
            if (creatorUser == null)
            {
                return Unauthorized("Creator user not found.");
            }
            newProject.Users.Add(creatorUser);

            _context.Projects.Add(newProject);
            await _context.SaveChangesAsync();

            // Dönen DTO için Customer bilgisini yükle
            newProject.Customer = await _context.Customers.FindAsync(newProject.CustomerId); 

            var projectDto = _mapper.Map<ProjectDto>(newProject);
            projectDto.TaskCount = 0;

            return CreatedAtAction(nameof(GetProject), new { id = projectDto.Id }, projectDto);
        }

        // PUT: api/projects/5 
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateProject(int id, CreateProjectDto updateProjectDto) 
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            var projectToUpdate = await _context.Projects
                                            .Include(p => p.Users)
                                            .FirstOrDefaultAsync(p => p.Id == id);

            if (projectToUpdate == null) return NotFound();

            if (!projectToUpdate.Users.Any(u => u.Id == currentUserId.Value))
            {
                return Forbid();
            }

            projectToUpdate.Name = updateProjectDto.Name;

            try
            {
                _context.Entry(projectToUpdate).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) { if (!await _context.Projects.AnyAsync(p => p.Id == id)) return NotFound(); else throw; }
            catch (DbUpdateException ex) { Console.WriteLine($"DbUpdateException updating project {id}: {ex}"); return StatusCode(500); }


            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            var projectToDelete = await _context.Projects
                                           .Include(p => p.Users)
                                           .Include(p => p.Tasks)
                                           .FirstOrDefaultAsync(p => p.Id == id);

            if (projectToDelete == null) return NotFound();

            if (!projectToDelete.Users.Any(u => u.Id == currentUserId.Value))
            {
                return Forbid();
            }

            // Many-to-Many 
            _context.Projects.Remove(projectToDelete);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/projects/1/users/2 
        [HttpPost("{projectId}/users/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AddUserToProject(int projectId, int userId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            var project = await _context.Projects.Include(p => p.Users).FirstOrDefaultAsync(p => p.Id == projectId);
            var userToAdd = await _context.Users.FindAsync(userId);

            if (project == null || userToAdd == null) return NotFound("Project or User to add not found.");

            if (!project.Users.Any(u => u.Id == currentUserId.Value))
            {
                return Forbid();
            }

            if (project.Users.Any(u => u.Id == userId)) return BadRequest("User is already in the project.");

            project.Users.Add(userToAdd);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/projects/1/users/2 
        [HttpDelete("{projectId}/users/{userId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)] 
        public async Task<IActionResult> RemoveUserFromProject(int projectId, int userId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return Unauthorized();

            var project = await _context.Projects.Include(p => p.Users).FirstOrDefaultAsync(p => p.Id == projectId);
            var userToRemove = project?.Users.FirstOrDefault(u => u.Id == userId);

            if (project == null || userToRemove == null) return NotFound("Project not found or User not in project.");

            if (!project.Users.Any(u => u.Id == currentUserId.Value))
            {
                return Forbid();
            }

            if (userToRemove.Id == currentUserId.Value && project.Users.Count == 1)
                return BadRequest("Cannot remove the last member of the project.");

            project.Users.Remove(userToRemove);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}