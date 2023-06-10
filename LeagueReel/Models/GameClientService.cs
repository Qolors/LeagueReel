﻿using System.Threading.Tasks;

namespace LeagueReel.Models
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    public class GameClientService
    {
        private readonly HttpClient client;

        public GameClientService()
        {
            var handler = new HttpClientHandler();

            //TODO --> Saving this for reference but eventually remove
            //handler.ServerCertificateCustomValidationCallback =
            //    (HttpRequestMessage httpRequestMessage, X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors) =>
            //    {
            //        // Compare the certificate with the one you got from Riot
            //        var validCert = new X509Certificate2("riotgames.pem");
            //        return cert.Equals(validCert);
            //    };

            // TODO --> Remove this when we have a valid cert
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            client = new HttpClient(handler);
        }

        public async Task<string> GetGameData(string endpoint)
        {
            HttpResponseMessage response = await client.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            else
            {
                throw new Exception($"Request failed with status code {response.StatusCode}");
            }
        }
    }

}
