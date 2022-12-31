using System.Collections.Generic;
using System.Linq;
using Lavalink4NET.Decoding;

namespace Zeenox.Models.Socket.Server;

public readonly struct PlaylistsMessage : IServerMessage
{
    public List<TrackData> Favorites { get; init; }
    public List<Playlist> Playlists { get; init; }

    public static PlaylistsMessage FromPlaylists(List<Playlist> playlists)
    {
        var favorites = playlists[0].Songs.ConvertAll(x => TrackData.FromLavalinkTrack(TrackDecoder.DecodeTrack(x)));
        var playlistsData = playlists.Skip(1).ToList();
        return new PlaylistsMessage
        {
            Favorites = favorites,
            Playlists = playlistsData
        };
    }
}