# README #

### What is this repository for? ###

* sync_youtube_dl is a simple c# application to synchronize a youtube playlist with a local folder, it will check which files are missing and will download them.
Right now it serves the purpose of synchronizing a music/sound playlist, with the option to use mp3gain to normalize the files' volumes.
It also normalizes videos' titles, removing unecessary clutter (tags, symbols, etc.), changing authors format (Authors are separated by a comma) among other title quality changes (Check SongParser.cs).
* Version 0.0.1

### How do I get set up? ###

* Just look into "SOFTWARE_NEEDED.txt" and "sample_execute.bat"

### Dependencies ###

Libraries:
* TagLib by https://www.nuget.org/packages/taglib/
* Newtonsoft Json https://www.nuget.org/packages/newtonsoft.json/

External Software:
* youtube-dl https://rg3.github.io/youtube-dl/
* ffmpeg https://www.ffmpeg.org/
* mp3gain http://mp3gain.sourceforge.net/