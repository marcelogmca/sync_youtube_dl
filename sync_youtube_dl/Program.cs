using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace sync_youtube_dl
{
    class MainClass
	{
        const string MP3GAINDIR = "mp3gain\\mp3gain.exe";
        public struct Video
		{
			private string Title;
			public string title
			{
				get
				{
					return Title;
				}
				set
				{
					Title = value;
				}
			}

			private string Id;
			public string id
			{
				get
				{
					return Id;
				}
				set
				{
					Id = value;
				}
			}

            private int Number;
            public int number
            {
                get
                {
                    return Number;
                }
                set
                {
                    Number = value;
                }
            }
        }

		public static void Main(string[] args)
		{
            //Check of arguments
            string playlistTITLE;
            string playlistID;
            string dir;

            if (args.Length != 3)
            {
                Console.WriteLine("Syntax: sync_youtube_dl.exe playlistTITLE playlistID Directory/Folder");
                return;
            }
            else
            {
                playlistTITLE = args[0];
                playlistID = args[1];
                dir = args[2];
            }

            bool dependencieCheckFailed = false;
            //Check dependencies

            if (RunningPlatform() == Platform.Windows) //WIP, this step is only for windows
            {
                if (!File.Exists("youtube-dl.exe"))
                {
                    Console.WriteLine("ERROR: youtube-dl.exe is missing.");
                    dependencieCheckFailed = true;
                }
                if (!File.Exists("ffmpeg.exe"))
                {
                    Console.WriteLine("ERROR: ffmpeg.exe is missing.");
                    dependencieCheckFailed = true;
                }
                if (!File.Exists("ffprobe.exe"))
                {
                    Console.WriteLine("ERROR: ffprobe.exe is missing.");
                    dependencieCheckFailed = true;
                }
                if (dependencieCheckFailed)
                {
                    return;
                }
            }
            

            //Scan directory for files
            var allfiles = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
            List<String> allfilenames = new List<String>();
            List<String> existentIds = new List<String>();

            //Scan every files' tags
            Console.WriteLine("\r\nSearching directory for existing files.");
            int fileC = 0;
            foreach (var file in allfiles)
            {
                try
                {
                    if (Path.GetExtension(file).ToLower() == ".mp3")
                    {
                        TagLib.File f = TagLib.File.Create(file);
                        existentIds.Add(f.Tag.Album);
                        Console.WriteLine("tag:" + f.Tag.Album);
                        allfilenames.Add(Path.GetFileNameWithoutExtension(file));
                        fileC++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex);
                    return;
                }
            }
            Console.WriteLine(fileC + " files found.");
            allfiles = null; //free memory

			List<Video> videos = new List<Video>();

			// Use ProcessStartInfo class
			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.CreateNoWindow = false;
			startInfo.UseShellExecute = false;
            if (RunningPlatform() != Platform.Linux) //Linux support, WIP
            {
                startInfo.FileName = "youtube-dl.exe";
            }
            else
            {
                startInfo.FileName = "youtube-dl";
            }
			startInfo.WindowStyle = ProcessWindowStyle.Hidden;
			startInfo.RedirectStandardOutput = true;
            startInfo.StandardOutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine(playlistID);
			Console.WriteLine("\r\nyoutube-dl done searching for " + playlistID);

            //retrieve a list of all videos
            try
            {
                videos = getPlayListsVideos(playlistID, existentIds);
                Console.WriteLine("\r\nStarting sync, downloading what was not found locally (" + videos.Count + " videos) on " + playlistTITLE);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error retrieving playlist's videos.");
            }
            int cont=0;
            DateTime a = DateTime.Now;
            foreach (var video in videos) //Iterate through every video
            {
                if (videos != null)
                {
                    try
                    {
                        //Calculate average time
                        Console.WriteLine("Video " + (++cont) + "\\" + videos.Count);
                        Console.WriteLine(video.title + " doesn't exist.");


                        startInfo.Arguments = "-o " + Path.DirectorySeparatorChar + "music" + Path.DirectorySeparatorChar + playlistTITLE + Path.DirectorySeparatorChar + "%(title)s.%(ext)s -f 149/140/best -i -x --audio-format mp3 --embed-thumbnail https://www.youtube.com/watch?v=" + video.id;
                        string temp = startInfo.Arguments;
                        var newFile = "";
                        using (var p = new Process()) //Start youtube-dl and download this video, capture it's output
                        {
                            p.StartInfo = startInfo;
                            p.EnableRaisingEvents = true;
                            Console.Write("Downloading");
                            p.OutputDataReceived += (s, o) =>
                            {
                                Console.Write(".");
                                if (o.Data != null && o.Data.Contains("[ffmpeg] Destination: "))
                                {
                                    newFile = toUtf8(o.Data.Replace("[ffmpeg] Destination: ", "").Trim());
                                    Console.Write("\nDestination File: " + newFile + "\nConverting to mp3");
                                }
                            };
                            p.Start();

                            p.BeginOutputReadLine();

                            p.WaitForExit();
                        }
                        //Join directory + filename to make full path
                        string startDir = AppDomain.CurrentDomain.BaseDirectory;

                        string path = toUtf8(startDir) + newFile;

                        if(!File.Exists(path))
                        {
                            Console.WriteLine("Failed to find downloaded file, searching for it...");
                            foreach(var fileScan in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
                            {
                                if(StringCompare(fileScan,newFile)<5)
                                {
                                    newFile = fileScan;
                                    path = toUtf8(startDir) + newFile;
                                    break;
                                }
                            }
                        }

                        TagLib.File f = TagLib.File.Create(path);
                        f.Tag.Track = Convert.ToUInt32(video.number);
                        f.Tag.Album = video.id; //Tag mp3 album with video ID
                        Console.WriteLine("new tag:" + f.Tag.Album);

                        string extension = Path.GetExtension(newFile);
                        string normalizedTitle = SongParser.normalize(video.title);
                        string normalizedFileName = normalizedTitle + extension;
                        try
                        {
                            string title = normalizedTitle.Split('-')[1].Trim();
                            f.Tag.Title = title;
                            string artist = normalizedTitle.Split('-')[0].Trim();
                            string[] artists;
                            artists = SongParser.toListArtists(artist).ToArray();
                            f.Tag.Performers = null;
                            f.Tag.Performers = artists;
                            f.Tag.AlbumArtists = null;
                            f.Tag.AlbumArtists = artists;
                        }
                        catch { }

                        f.Save();
                        File.Move(toUtf8(startDir) + newFile, toUtf8(startDir) + Path.GetDirectoryName(newFile) + "\\" + normalizedFileName);
                        Console.WriteLine("File added, new name: "+normalizedTitle);

                        //Normalize volume IF mp3gain is present
                        Console.WriteLine("Normalizing volume...");
                        if (File.Exists(MP3GAINDIR))
                        {
                            Process mp3gain = new Process();
                            mp3gain.StartInfo.FileName = MP3GAINDIR;
                            mp3gain.StartInfo.CreateNoWindow = true;
                            mp3gain.StartInfo.RedirectStandardOutput = true;
                            mp3gain.StartInfo.UseShellExecute = false;
                            mp3gain.StartInfo.Arguments = "/m 5 /c /r \"" + toUtf8(startDir) + Path.GetDirectoryName(newFile) + "\\" + normalizedFileName + "\"";
                            mp3gain.Start();
                            mp3gain.WaitForExit();
                        }

                        //Calculate average time
                        DateTime b = DateTime.Now;
                        double seconds = Math.Round((b.Subtract(a).TotalSeconds / cont) * (videos.Count - cont));
                        double minutes = Math.Floor(seconds / 60);
                        Console.WriteLine("ETA: " + minutes + "m " + (seconds - minutes * 60) + " s\n");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR: " + ex);
                    }
                }
                else
                {
                    Console.WriteLine("Failed to retrieve playlist's videos. Skipping.");
                }
            }

			Console.WriteLine("\r\nEverything is done on this playlist!");
		}

        private static List<Video> getPlayListsVideos(string playlistId, List<String> existentIds)
        {
            //Retrieves playlist's videos from google API
            List<Video> videos = new List<Video>();
            try
            {
                var url = "https://www.googleapis.com/youtube/v3/playlistItems?part=snippet&maxResults=50&playlistId=" + playlistId + "&key=AIzaSyC35eUah1lyAIvW1aURVSaqoorimtHSjoI";

                var urlWithTokens = url;

                string tokenTag = "&pageToken=";
                string nextPage = null;
                int cont = 0;
                do
                {
                    if (nextPage != null)
                        urlWithTokens = url + tokenTag + nextPage;

                    var json = new WebClient().DownloadString(urlWithTokens);

                    //Convert Json
                    dynamic deserializedProduct = JsonConvert.DeserializeObject(json);
                    dynamic items = deserializedProduct.items;
                    for (int i = 0; i < items.Count; i++)
                    {
                        cont++;
                        Video vid = new Video();
                        vid.number = cont;
                        var id_ = Convert.ToString(items[i].snippet.resourceId.videoId);
                        vid.id = id_;

                        //Convert to UTF-8
                        string temp = items[i].snippet.title;
                        var title_ = toUtf8(temp);
                        
                        vid.title = title_;
                        if (!existentIds.Contains(id_)) //If video has been downloaded aready, do not include it
                        {
                            videos.Add(vid);
                        }
                        else
                        {
                            Console.WriteLine(title_ + " exists.");
                        }
                    }
                    nextPage = deserializedProduct.nextPageToken;

                } while (nextPage != null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving some videos: " + ex);
                return null;
            }
            return videos;
        }

        public static string toUtf8(string input)
        {
            byte[] bytes = Encoding.Default.GetBytes(input);
            return Encoding.UTF8.GetString(bytes);
        }

        public static int StringCompare(string s, string t)
        {
            //LevenshteinDistance algorithm
            //Minimum number of edits to make two strings the same
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }


        /**
         * 
         * WIP, multiplatform support using Mono
         * 
         * **/

        public enum Platform
        {
            Windows,
            Linux,
            Mac
        }

        public static Platform RunningPlatform()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    // Well, there are chances MacOSX is reported as Unix instead of MacOSX.
                    // Instead of platform check, we'll do a feature checks (Mac specific root folders)
                    if (Directory.Exists("/Applications")
                        & Directory.Exists("/System")
                        & Directory.Exists("/Users")
                        & Directory.Exists("/Volumes"))
                        return Platform.Mac;
                    else
                        return Platform.Linux;

                case PlatformID.MacOSX:
                    return Platform.Mac;

                default:
                    return Platform.Windows;
            }
        }
    }
}
