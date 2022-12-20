using System.Linq;
using System.Threading.Tasks;
using Discordance.Services;
using Microsoft.AspNetCore.Mvc;

namespace Discordance.Controllers;

[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly SearchService _searchService;

    public SearchController(SearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet]
    [Route("[action]")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        var searchResults = await _searchService.SearchAsync(query).ConfigureAwait(false);
        return Ok(searchResults.Select(x => new Track(x.Title, x.Url, x.CoverUrl)));
    }

    private record Track(string Title, string Url, string CoverUrl);
}