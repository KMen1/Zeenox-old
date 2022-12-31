using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Zeenox.Services;

namespace Zeenox.Controllers;

[Route("api/[controller]/[action]")]
public class SearchController : ControllerBase
{
    private readonly MongoService _mongoService;
    private readonly SearchService _searchService;

    public SearchController(SearchService searchService, MongoService mongoService)
    {
        _searchService = searchService;
        _mongoService = mongoService;
    }

    [HttpGet]
    [Route("")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        var searchResults = await _searchService.SearchAsync(query).ConfigureAwait(false);
        return Ok(searchResults.Select(x => new Track(x.Title, x.Url, x.CoverUrl)));
    }

    [HttpGet]
    [Route("")]
    public async Task<IActionResult> Favorite(ulong userId)
    {
        var user = await _mongoService.GetUserAsync(userId).ConfigureAwait(false);
        return Ok(user.Playlists[0].Songs);
    }

    private record Track(string Title, string Url, string CoverUrl);
}