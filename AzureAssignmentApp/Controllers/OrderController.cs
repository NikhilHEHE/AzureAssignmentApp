using Microsoft.AspNetCore.Mvc;
using AzureAssignmentApp.Data;
using AzureAssignmentApp.Models;

namespace AzureAssignmentApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrderController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public IActionResult Create(Order order)
    {
        _context.Orders.Add(order);
        _context.SaveChanges();

        // Later: trigger Azure Function here

        return Ok(order);
    }
}