# Spotify-Web-Recorder
Fork from https://spotifywebrecorder.codeplex.com/

The Codeplex archive exists only read-only anymore, and the code doesn't work anymore because of its usage of outdated APIs.

Aim is to make:
* the program work again by using the Spotify client API provided by the Nuget SpotifyAPI package
* record audio using the WasapiLoopbackCapture class from NAudio Nuget package to remove possibility of recording using the microphone
* show cover art using a simple image instead of embedding Firefox rendering engine
