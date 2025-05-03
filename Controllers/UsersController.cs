using AutoMapper;
using Microsoft.AspNetCore.Authorization; // Eklendi
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims; // Eklendi
using System.Threading.Tasks;
using TaskTitan.Api.Dtos;
using TaskTitanData;
using TaskTitanData.Entities;

namespace TaskTitan.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Controller seviyesinde yetkilendirme
    public class UsersController : ControllerBase
    {
        private readonly TaskTitanDbContext _context;
        private readonly IMapper _mapper;

        public UsersController(TaskTitanDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/users (Sadece giriş yapanlar görebilir)
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _context.Users.OrderBy(u => u.Name).AsNoTracking().ToListAsync();
            var userDtos = _mapper.Map<List<UserDto>>(users);
            return Ok(userDtos);
        }

        // GET: api/users/5 (Sadece giriş yapanlar görebilir)
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            // Belki kullanıcı sadece kendi detayını görebilmeli?
            // var currentUserId = GetCurrentUserId();
            // if (currentUserId != id && !User.IsInRole("Admin")) return Forbid();

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            var userDto = _mapper.Map<UserDto>(user);
            return Ok(userDto);
        }

        // POST: api/users (Yetki gerektirebilir)
        [HttpPost]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        // [Authorize(Roles = "Admin")] // Örnek: Sadece Admin rolü ekleyebilir
        public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto createUserDto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == createUserDto.Email))
            { return BadRequest("Email address already in use."); }

            var newUser = _mapper.Map<User>(createUserDto);
            // TODO: Password Hashing eklenmeli
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            var userDto = _mapper.Map<UserDto>(newUser);
            return CreatedAtAction(nameof(GetUser), new { id = userDto.Id }, userDto);
        }

        // PUT: api/users/5 (Yetki gerektirebilir)
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateUser(int id, CreateUserDto updateUserDto) // Ayrı UpdateUserDto kullanmak daha iyi
        {
            var currentUserId = GetCurrentUserId();
            // Yetki kontrolü: Sadece admin veya kullanıcının kendisi güncelleyebilir
            if (currentUserId != id /* && !User.IsInRole("Admin") */) // Rol kontrolü eklenebilir
            {
                return Forbid();
            }

            var userToUpdate = await _context.Users.FindAsync(id);
            if (userToUpdate == null) return NotFound();

            // Email unique kontrolü
            if (userToUpdate.Email != updateUserDto.Email &&
                await _context.Users.AnyAsync(u => u.Email == updateUserDto.Email && u.Id != id))
            { return BadRequest("New email address is already in use."); }

            // Sadece Name ve Email güncelleniyor (şimdilik)
            userToUpdate.Name = updateUserDto.Name;
            userToUpdate.Email = updateUserDto.Email;
            // TODO: Password güncelleme ayrı bir endpoint/mantık gerektirir

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { if (!await _context.Users.AnyAsync(u => u.Id == id)) return NotFound(); else throw; }
            return NoContent();
        }

        // DELETE: api/users/5 (Yetki gerektirebilir)
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        // [Authorize(Roles = "Admin")] // Örnek: Sadece Admin silebilir
        public async Task<IActionResult> DeleteUser(int id)
        {
            var currentUserId = GetCurrentUserId();
            // Kendini silmesini engelle
            if (id == currentUserId)
            {
                return BadRequest("Cannot delete your own account.");
            }

            var userToDelete = await _context.Users.FindAsync(id);
            if (userToDelete == null) return NotFound();

            // Kullanıcının atanmış görevi var mı? (Restrict FK)
            var hasAssignedTasks = await _context.Tasks.AnyAsync(t => t.AssignedUserId == id);
            if (hasAssignedTasks) return BadRequest("Cannot delete user. Reassign or delete assigned tasks first.");

            // Kullanıcının oluşturduğu görevler var mı? (Restrict FK)
            var hasCreatedTasks = await _context.Tasks.AnyAsync(t => t.CreatedById == id);
            if (hasCreatedTasks) return BadRequest("Cannot delete user. Reassign or delete created tasks first.");


            // TODO: Many-to-many ilişkisini temizle (ProjectUser)
            // EF Core 7+ bunu otomatik yapabilir, ama emin olmak için kontrol etmek iyi olur.
            // var userWithProjects = await _context.Users.Include(u => u.Projects).FirstOrDefaultAsync(u => u.Id == id);
            // if (userWithProjects != null) userWithProjects.Projects.Clear();
            // await _context.SaveChangesAsync(); // Önce ilişkileri temizle

            _context.Users.Remove(userToDelete);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out var userId)) return userId;
            // [Authorize] olduğu için buraya gelinmemeli ama yine de kontrol
            throw new InvalidOperationException("User ID could not be determined from token.");
        }
    }
}