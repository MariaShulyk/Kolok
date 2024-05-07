using Kolok1.DTOs;
using Kolok1.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kolok1.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController(IDbService dbService) : ControllerBase
{
    [HttpGet("{id:int}/genres")]
    public async Task<IActionResult> GetGenresById(int id)
    {
        var result = await dbService.GetGenresByIdAsync(id);
        if (result == null) return NotFound($"Book with id: {id} doesn't exist");
        return Ok(result);
    }
    
    [HttpPost]
    public async Task<IActionResult> AddBook(AddBookDTO addBookDto)
    {
        try
        {
            var result = await dbService.AddBookAsync(addBookDto);
            return Ok(result);
        }
        catch (Exception e)
        {
            return NotFound(e.Message);
        }
    }
}