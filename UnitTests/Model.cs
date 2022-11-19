using Xunit.Abstractions;

namespace UnitTests
{
    public class Model
    {
        PlaylistWeigher playlist_weigher = new("85bfa24f31c2414eba026ef1bea0c575");
        ITestOutputHelper output;

        public Model(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async void Login()
        {
            var playlists = await playlist_weigher.getPlaylists();

            List<WeightedTrack> tracks = await playlist_weigher.getTracks(playlists.Find(p => p.Name == "TEST PLAYLIST")!);

            foreach (WeightedTrack wtrack in tracks)
            {
                output.WriteLine($"{PlaylistWeigher.getField<string>(wtrack.track, "Name")}\t:\t{wtrack.Weight}");
            
            
            }

            tracks[0].Weight = 10;

            var plist = await playlist_weigher.createPlaylist("TEST PLAYLIST");

            await playlist_weigher.pushTracks(tracks, plist);
        }

        [Fact]
        public void playlits()
        {
        }
    }
}