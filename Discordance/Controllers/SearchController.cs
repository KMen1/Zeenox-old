using System.Linq;
using System.Threading.Tasks;
using Discordance.Services;
using Microsoft.AspNetCore.Mvc;

namespace Discordance.Controllers;

[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly SearchService _searchService;
    private readonly MongoService _mongoService;

    public SearchController(SearchService searchService, MongoService mongoService)
    {
        _searchService = searchService;
        _mongoService = mongoService;
    }

    [HttpGet]
    [Route("[action]")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        var searchResults = await _searchService.SearchAsync(query).ConfigureAwait(false);
        return Ok(searchResults.Select(x => new Track(x.Title, x.Url, x.CoverUrl)));
    }

    [HttpGet]
    [Route("[action]")]
    public async Task<IActionResult> Favorite(ulong userId)
    {
        var user = await _mongoService.GetUserAsync(userId).ConfigureAwait(false);
        return Ok(user.Playlists[0].Songs);
    }

    private record Track(string Title, string Url, string CoverUrl);
}