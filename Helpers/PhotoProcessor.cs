using System;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Headers;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;

namespace PhotoFiddler.Helpers
{
    public class PhotoProcessor :IPhotoProcessor
    {
        private readonly PhotoApiSettings _photoApiSettings;
          public PhotoProcessor(IOptions<PhotoApiSettings> photoApiSettings)
        {
            _photoApiSettings = photoApiSettings.Value;
        }
        
        public async Task<string> Process(string incomingImageUrl, string sid, string host)
        {
            var imageLocation = SaveImageLocally(incomingImageUrl, sid);

            var privateKey = _photoApiSettings.PrivateKey;
            var appId = _photoApiSettings.AppId;
         

            string imageUrl = string.Empty;
            using (var httpClient = new HttpClient())
            {
                var apiEndPointPost = "http://opeapi.ws.pho.to/addtask";
                var apiEndPointGet = "http://opeapi.ws.pho.to/getresult?request_id=";
                string requestId = "";
                var localImageStore = host + imageLocation;

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                string xmlMessage = $@"<image_process_call>
                                      <image_url>{localImageStore}</image_url>
                                    <methods_list>
                                        <method>
                                            <name>caricature</name>
                                            <params>type=13</params>
                                        </method>
                                    </methods_list>
                                </image_process_call>";

                var key = Encoding.ASCII.GetBytes(privateKey);
                var keySha = EncodeKey(xmlMessage, key);
                var values = new Dictionary<string, string>
                    {
                        { "app_id", appId },
                        { "sign_data", keySha },
                        {"data", xmlMessage}
                    };

                var content = new FormUrlEncodedContent(values);

                var responseMessage = await
                    httpClient
                        .PostAsync(apiEndPointPost, content);

                Console.WriteLine(responseMessage);

                if (responseMessage.IsSuccessStatusCode)
                {
                    var response = await responseMessage.Content.ReadAsStringAsync();

                    var xml = XElement.Parse(response).Descendants().FirstOrDefault(x => x.Name == "request_id");
                    requestId = xml?.Value;
                }

                string status;
               
                int i = 0;
                do
                {
                    System.Threading.Thread.Sleep(1000);
                    var url = apiEndPointGet + requestId;
                    var responseGet = await httpClient.GetAsync(url);
                    var contentString = await responseGet.Content.ReadAsStringAsync();
                    
                    var xmlGet = XElement.Parse(contentString).Descendants();
                    var xmlStatus = xmlGet.FirstOrDefault(x => x.Name == "status");
                    status = xmlStatus?.Value;
                    ++i;

                    if(status == "OK")
                    {
                        var xmlUrl = xmlGet.FirstOrDefault(x => x.Name == "result_url");
                        imageUrl = xmlUrl?.Value ?? "empty node";
                    }
                }
                while (i < 10 && status == "InProgress");

                if (i == 10 && status == "InProgress")
                {
                    Console.WriteLine("Retrieve processed image : Timeout error.");
                    return string.Empty;
                }

            }
            return imageUrl;
        }

        private string SaveImageLocally(string imageUrl, string sid)
        {
            Console.WriteLine();
            var root = "/wwwroot";
            var slug = $@"/images/{sid}.jpg";
            string saveLocation = Environment.CurrentDirectory + root + slug;

            byte[] imageBytes;
            HttpWebRequest imageRequest = (HttpWebRequest)WebRequest.Create(imageUrl);
            WebResponse imageResponse = imageRequest.GetResponse();

            Stream responseStream = imageResponse.GetResponseStream();

            using (BinaryReader br = new BinaryReader(responseStream ))
            {
                imageBytes = br.ReadBytes(500000);
                br.Close();
            }
            responseStream.Close();
            imageResponse.Close();

            FileStream fs = new FileStream(saveLocation, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);
            try
            {
                bw.Write(imageBytes);
            }
            finally
            {
                fs.Close();
                bw.Close();
            }
            return slug;
        }
//https://stackoverflow.com/questions/6067751/how-to-generate-hmac-sha1-in-c
        private string EncodeKey(string input, byte[] key)
        {
            HMACSHA1 myhmacsha1 = new HMACSHA1(key);
            byte[] byteArray = Encoding.ASCII.GetBytes(input);
            MemoryStream stream = new MemoryStream(byteArray);
            return myhmacsha1.ComputeHash(stream).Aggregate("", (s, e) => s + String.Format("{0:x2}", e), s => s);
        }
    }
}

