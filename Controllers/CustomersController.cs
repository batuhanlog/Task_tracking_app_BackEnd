using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskTitan.Api.Dtos;
using TaskTitan.Data.Entities;
using TaskTitanData;
using TaskTitanData.Entities;

namespace TaskTitan.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class CustomersController : ControllerBase
    {
        private readonly TaskTitanDbContext _context;
        private readonly IMapper _mapper;

        public CustomersController(TaskTitanDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/customers 
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CustomerDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers()
        {
            
            var customers = await _context.Customers
                                        .OrderBy(c => c.Name)
                                        .AsNoTracking() 
                                        .ToListAsync();
            var customerDtos = _mapper.Map<List<CustomerDto>>(customers);
            return Ok(customerDtos);
        }

        // GET: api/customers/5 
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CustomerDto>> GetCustomer(int id)
        {
            
            var customer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (customer == null)
            {
                return NotFound($"Customer with ID {id} not found.");
            }
            var customerDto = _mapper.Map<CustomerDto>(customer);
            return Ok(customerDto);
        }

        // POST: api/customers
        [HttpPost]
        [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        // [Authorize(Roles = "Admin,Editor")] 
        public async Task<ActionResult<CustomerDto>> CreateCustomer(CreateCustomerDto createCustomerDto)
        {
            var newCustomer = _mapper.Map<Customer>(createCustomerDto);
            _context.Customers.Add(newCustomer);
            await _context.SaveChangesAsync();
            var customerDto = _mapper.Map<CustomerDto>(newCustomer);
            return CreatedAtAction(nameof(GetCustomer), new { id = customerDto.Id }, customerDto);
        }

        // PUT: api/customers/5 
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        // [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> UpdateCustomer(int id, CreateCustomerDto updateCustomerDto)
        {
            var customerToUpdate = await _context.Customers.FindAsync(id);
            if (customerToUpdate == null)
            {
                return NotFound($"Customer with ID {id} not found.");
            }
            customerToUpdate.Name = updateCustomerDto.Name;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { if (!await _context.Customers.AnyAsync(c => c.Id == id)) return NotFound(); else throw; }
            return NoContent();
        }

        // DELETE: api/customers/5 
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customerToDelete = await _context.Customers.FindAsync(id);
            if (customerToDelete == null)
            {
                return NotFound($"Customer with ID {id} not found.");
            }
            var hasProjects = await _context.Projects.AnyAsync(p => p.CustomerId == id);
            if (hasProjects)
            {
                return BadRequest("Cannot delete customer. Associated projects exist.");
            }
            _context.Customers.Remove(customerToDelete);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}