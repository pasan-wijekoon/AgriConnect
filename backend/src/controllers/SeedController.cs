using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using backend.src.config;

namespace backend.src.controllers;

[ApiController]
[Route("api/[controller]")]
public class SeedController : ControllerBase
{
    private readonly AppDbContext _context;

    public SeedController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("reset")]
    public async Task<IActionResult> ResetAndSeed()
    {
        await _context.Database.EnsureDeletedAsync();
        await DbSeeder.SeedAsync(_context);
        return Ok(new { message = "Database successfully reset and re-seeded with demo data." });
    }
}
