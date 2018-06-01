namespace SpotifyWebRecorder
{
	public class Mp3Tag
	{
		public System.Drawing.Image AlbumCover
		{
			get;
			set;
		}

		public string AlbumName
		{
			get;
			set;
		}

		public string Artist
		{
			get;
			set;
		}

		public string Title
		{
			get;
			set;
		}

		public string TrackUri
		{
			get;
			set;
		}

		public Mp3Tag(string title, string artist, string trackUri, string albumName, System.Drawing.Image albumCov)
		{
			Title = title;
			Artist = artist;
			TrackUri = trackUri;
			AlbumCover = albumCov;
			AlbumName = albumName;
		}

		public Mp3Tag Clone()
		{
			return new Mp3Tag(Title, Artist, TrackUri, AlbumName, AlbumCover);
		}
	}
}