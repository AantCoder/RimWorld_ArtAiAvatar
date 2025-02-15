using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using ArtAi.data;
using Steamworks;
using UnityEngine;
using Verse;

namespace ArtAi
{
    public abstract class Generator
    {
        private static readonly Dictionary<Description, GeneratedImage> _images
            = new Dictionary<Description, GeneratedImage>();

        public static GeneratedImage Generate(Description description, bool withoutNewGenerate = false)
        {
            GeneratedImage image = null;
            if (_images.ContainsKey(description) && !(image = _images[description]).NeedUpdate(withoutNewGenerate))
            {
                return image;
            }

            try
            {
                var cached = ImageRepo.GetImage(description);
                if (cached != null)
                {
                    return image = GeneratedImage.Done(cached);
                }
                if (withoutNewGenerate)
                {
                    if (_images.ContainsKey(description)) return _images[description];
                    else return image = GeneratedImage.NeedGenerate();
                }
                try
                {
                    Log.Message("AI Art request");
                    var steamAccountID = SteamAccountID();
                    var request = MakeRequest(description.ArtDescription, description.ThingDescription,
                        steamAccountID, description.Language);

                    using (var response = request.GetResponse())
                    {
                        using (var rsDataStream = response.GetResponseStream())
                        {
                            return image = ProcessResponse(rsDataStream, response.ContentType, description);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e.Message);
                    return image = GeneratedImage.Error();
                }
            }
            finally
            {
                if (image != null) _images[description] = image;
            }
        }

        private static WebRequest MakeRequest(string artDescription, string thingDescription, string steamAccountID,
            string language)
        {
            var serverUrl = ArtAiSettings.ServerUrl;
            var request = WebRequest.Create(serverUrl);
            request.Method = "POST";
            var postData = artDescription + ';' + thingDescription + ';' + steamAccountID + ';' + language;
            var byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentType = "text/plain";
            request.ContentLength = byteArray.Length;
            using (var rqDataStream = request.GetRequestStream())
            {
                rqDataStream.Write(byteArray, 0, byteArray.Length);
                rqDataStream.Close();
            }
            return request;
        }

        private static GeneratedImage ProcessResponse(Stream response, string contentType, Description description)
        {
            if (response == null)
            {
                return GeneratedImage.Error();
            }

            switch (contentType)
            {
                case "text":
                case "text/plain":
                    using (var reader = new StreamReader(response))
                    {
                        var responseFromServer = reader.ReadToEnd();
                        var processedResponse = TranslateResponse(responseFromServer);
                        return GeneratedImage.InProgress(processedResponse);
                    }
                case "image/png":
                    using (var ms = new MemoryStream())
                    {
                        response.CopyTo(ms);
                        var array = ms.ToArray();
                        ImageRepo.SaveImage(array, description);
                        Texture2D tex = new Texture2D(2, 2, TextureFormat.Alpha8, true);
                        tex.LoadImage(array);
                        tex.Apply();
                        return GeneratedImage.Done(tex);
                    }
                default:
                    return GeneratedImage.Error();
            }
        }

        private static string TranslateResponse(string responseFromServer)
        {
            const string queued = "Queued: ";
            const int approximateGenerationTimeSeconds = 30;
            if (responseFromServer.Contains(queued))
            {
                string queuePos = responseFromServer.Substring(responseFromServer.IndexOf(queued) + queued.Length);
                int waitTimeSeconds = (int.Parse(queuePos) + 1) * approximateGenerationTimeSeconds;
                return "AiArtInProgress".Translate()
                       + Environment.NewLine + Environment.NewLine +
                       "AiArtTimeReaming".Translate() + TimeSpan.FromSeconds(waitTimeSeconds).ToString();
            }
            if (responseFromServer.Contains("Try later"))
            {
                return "AiArtLimit".Translate();
            }

            return responseFromServer;
        }

        private static string SteamAccountID()
        {
            try
            {
                return SteamUser.GetSteamID().GetAccountID().m_AccountID.ToString();
            }
            catch (InvalidOperationException)
            {
                return "unknown";
            }
        }
    }
}