using Microsoft.AspNetCore.Mvc;
using AzureAssignmentApp.Data;
using AzureAssignmentApp.Models;

namespace AzureAssignmentApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Get() => Ok(_context.Products.ToList());

    [HttpPost]
    public IActionResult Create(Product product)
    {
        _context.Products.Add(product);
        _context.SaveChanges();
        return Ok(product);
    }
}