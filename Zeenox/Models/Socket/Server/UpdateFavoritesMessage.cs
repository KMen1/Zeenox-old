using System.Collections.Generic;

namespace Zeenox.Models.Socket.Server;

public readonly struct UpdateFavoritesMessage : IServerMessage
{
    public List<string> FavoriteIds { get; init; }

    public static UpdateFavoritesMessage Create(List<string> favoriteIds)
    {
        return new UpdateFavoritesMessage
        {
            FavoriteIds = favoriteIds
        };
    }
}