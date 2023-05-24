using DaprPOC.Infrastructure.Models;
using DaprPOC.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DaprPOC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CageController : ControllerBase
    {
        private readonly MyDbContext _dbContext;

        public CageController(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: api/cage
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cage>>> GetCages()
        {
            var cages = await _dbContext.Cages.ToListAsync();
            return Ok(cages);
        }

        // GET: api/cage/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Cage>> GetCage(int id)
        {
            var cage = await _dbContext.Cages.FindAsync(id);

            if (cage == null)
            {
                return NotFound();
            }

            return Ok(cage);
        }

        // POST: api/cage
        [HttpPost]
        public async Task<ActionResult<Cage>> CreateCage(Cage cage)
        {
            _dbContext.Cages.Add(cage);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCage), new { id = cage.Id }, cage);
        }

        // PUT: api/cage/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCage(int id, Cage cage)
        {
            if (id != cage.Id)
            {
                return BadRequest();
            }

            _dbContext.Entry(cage).State = EntityState.Modified;

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CageExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/cage/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCage(int id)
        {
            var cage = await _dbContext.Cages.FindAsync(id);

            if (cage == null)
            {
                return NotFound();
            }

            _dbContext.Cages.Remove(cage);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        private bool CageExists(int id)
        {
            return _dbContext.Cages.Any(e => e.Id == id);
        }
    }
}
