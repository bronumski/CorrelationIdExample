using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WebAppA
{
    public class WeatherForecast
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int) (TemperatureC / 0.5556);

        public string Summary { get; set; }
    }

    public class WeatherAppClient
    {
        private readonly HttpClient _httpClient;

        public WeatherAppClient(HttpClient httpClient)
            => _httpClient = httpClient;

        public async Task<IEnumerable<WeatherForecast>> GetForecastAsync()
        {
            var response = await _httpClient.GetAsync("weatherforecast");
            return JsonConvert.DeserializeObject<IEnumerable<WeatherForecast>>(
                await response.Content.ReadAsStringAsync());
        }
    }
}