using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace AVT.Agent
{
    public class HttpHelper
    {
        private static readonly HttpClient Client = new HttpClient();
        public static async Task<bool> SendStatus(string vin, int status, DateTime statusDate, CancellationToken cancellationToken = default(CancellationToken))
        {
            var url = $"{Shared.Url}?vin={vin}&status={status}&date={statusDate:yyyy-MM-dd_HH:mm:ss.fff}";
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", "avt_2018test"); // simply a static token for authorization ;)

            var response = await Client.GetAsync(url, cancellationToken);
            return await Task.FromResult(response.StatusCode == HttpStatusCode.OK);
        }
    }
}