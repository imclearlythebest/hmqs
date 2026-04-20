namespace WebApp.Models.Dtos;

public class SpotifyHistoryItem
{
    public string? endTime { get; set; }
    public string? artistName { get; set; }
    public string? trackName { get; set; }
    public int? msPlayed { get; set; }

    public string? ts { get; set; }
    public string? master_metadata_album_artist_name { get; set; }
    public string? master_metadata_track_name { get; set; }
    public int? ms_played { get; set; }
}
