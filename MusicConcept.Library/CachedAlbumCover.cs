using Newtonsoft.Json.Linq;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Web.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using Windows.Storage;
using System.Diagnostics;
using Windows.Networking.Connectivity;

namespace MusicConcept.Library
{
    public class CachedAlbumCover : NotifyPropertyChangedObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Album { get; set; }
        public string Artist { get; set; }

        public string ImageFileName { get; set; }
        public DateTime LastDownloadAttempt { get; set; }

        public CachedAlbumCover() { }

        public CachedAlbumCover(string album, string artist)
        {
            this.Album = album;
            this.Artist = artist;
            this.ImageFileName = null;
            this.LastDownloadAttempt = DateTime.MinValue;
        }

        private bool? isConnectionAvailable;
        private async Task<bool> GetIsConnectionAvailable()
        {
            if (isConnectionAvailable == null)
                isConnectionAvailable = (await NetworkInformation.FindConnectionProfilesAsync(
                    new ConnectionProfileFilter() { IsConnected = true, NetworkCostType = NetworkCostType.Unrestricted })).Any();

            return isConnectionAvailable.Value;
        }

        public async Task Load(string songFileName)
        {
            var fileName = ApplicationData.Current.LocalFolder.Path + "/" + Guid.NewGuid().ToString();
            Debug.WriteLine("Extracting Album Cover: " + this.Album);
            try
            {
                if (await AlbumCoverParser.ExtractAlbumPicture(songFileName, fileName))
                {
                    this.ImageFileName = fileName;
                }
                else if (await GetIsConnectionAvailable() && this.ImageFileName == null &&
                    (DateTime.Now - LastDownloadAttempt) > TimeSpan.FromDays(30 + new Random().Next(30)))
                {
                 /*   if (await XboxMusicAlbumCover.DownloadAlbumPicture(this.Album, this.Artist, fileName))
                        this.ImageFileName = fileName;
                    
                    this.LastDownloadAttempt = DateTime.Now;*/
                }
            }
            catch { /*Dont care*/ }
        }

        /*
        private async Task TryDownload()
        {
            if (!await GetIsConnectionAvailable() || this.ImageFileName != null ||
                (DateTime.Now - LastDownloadAttempt) < TimeSpan.FromDays(30 + new Random().Next(30)))
                return;

            Debug.WriteLine("Loading Album Cover: " + this.Album);
            
            try
            {
                var client = new Windows.Web.Http.HttpClient();

                // Define the data needed to request an authorization token.
                var service = "https://datamarket.accesscontrol.windows.net/v2/OAuth2-13";
                var clientId = "MusicConcept";
                var clientSecret = "maQ54hCgRS+wgRCZZU5G/rjO/LIXCpUqvhxU2rIjPOo=";
                var scope = "http://music.xboxlive.com";
                var grantType = "client_credentials";

                // Create the request data.
                var requestData = new Dictionary<string, string>();
                requestData["client_id"] = clientId;
                requestData["client_secret"] = clientSecret;
                requestData["scope"] = scope;
                requestData["grant_type"] = grantType;

                // Post the request and retrieve the response.
                var response = await client.PostAsync(new Uri(service), new HttpFormUrlEncodedContent(requestData));
                var responseString = await response.Content.ReadAsStringAsync();
                var token = Regex.Match(responseString, ".*\"access_token\":\"(.*?)\".*", RegexOptions.IgnoreCase).Groups[1].Value;

                // Use the token in a new request.
                service = "https://music.xboxlive.com/1/content/music/search?q={0}&filters=albums&accessToken=Bearer+{1}";
                response = await client.GetAsync(new Uri(string.Format(service, WebUtility.UrlEncode(this.Artist + " " + this.Album), WebUtility.UrlEncode(token))));
                responseString = await response.Content.ReadAsStringAsync();
                
                this.LastDownloadAttempt = DateTime.Now;

                var json = JObject.Parse(responseString);
                var albumToken = json.SelectToken("$.Albums.Items").Children().FirstOrDefault(p => p.SelectToken("Name").Value<String>().Equals(this.Album, StringComparison.OrdinalIgnoreCase));
                var urlToken = albumToken.SelectToken("ImageUrl");
                if (urlToken == null)
                    return;
                
                var url = urlToken.Value<string>() + "&w=480&h=480";
                var fileName = Guid.NewGuid().ToString() + ".png";
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName);
                BackgroundDownloader backgroundDownloader = new BackgroundDownloader();
                backgroundDownloader.CostPolicy = BackgroundTransferCostPolicy.UnrestrictedOnly;
                var operation = backgroundDownloader.CreateDownload(new Uri(url), file);
                await operation.StartAsync();

                this.ImageFileName = file.Path;
            }
            catch { return; }
        }*/
    }
}
