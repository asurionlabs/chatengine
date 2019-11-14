using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Web;
using System.Configuration;
using ChatWeb.Services;
using ChatWeb.Models;
using ChatWeb;

namespace ChatWeb.Services
{

    public class SpellcheckService
    {
        readonly RestApiService ApiService;

        public SpellcheckService(UrlConfig urlConfig)
        {
            ApiService = new RestApiService(urlConfig, null, 0);
        }

        public async Task<string> SpellcheckAsync(string message)
        {
            if (String.IsNullOrEmpty(message))
                return String.Empty;

            var request = new SpellcheckServiceRequest()
            {
                Text = message
            };

            var response = await ApiService.CallRestApi<SpellcheckServiceResponse>("", request, ChatConfiguration.SpellCheckTimeout);
            if (response == null)
                return message;

            return response.Output.Words;
        }
    }
}
