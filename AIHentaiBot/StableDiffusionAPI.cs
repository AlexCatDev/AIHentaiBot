using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AIHentaiBot
{
    public class Txt2ImgRequest
    {
        [JsonPropertyName("denoising_strength")]
        public double DenoisingStrength { get; set; } = 0;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = "cat";

        [JsonPropertyName("seed")]
        public int Seed { get; set; } = -1;

        [JsonPropertyName("subseed")]
        public int Subseed { get; set; } = -1;

        [JsonPropertyName("subseed_strength")]
        public int SubseedStrength { get; set; } = 0;

        [JsonPropertyName("batch_size")]
        public int BatchSize { get; set; } = 1;

        [JsonPropertyName("n_iter")]
        public int Iterations { get; set; } = 1;

        [JsonPropertyName("steps")]
        public int Steps { get; set; } = 30;

        [JsonPropertyName("cfg_scale")]
        public double CFGScale { get; set; } = 7;

        [JsonPropertyName("width")]
        public int Width { get; set; } = 512;

        [JsonPropertyName("height")]
        public int Height { get; set; } = 512;

        [JsonPropertyName("negative_prompt")]
        public string NegativePrompt { get; set; } = "";

        [JsonPropertyName("sampler_index")]
        public string SamplerIndex { get; set; } = "Euler";
    }

    public class Txt2ImgResponse
    {
		//Normally it returns json object with the full request but its not useful to me
		
        [JsonPropertyName("images")]
        public List<string> Images { get; set; }

        [JsonPropertyName("info")]
        public string Info { get; set; }

        public Exception? Exception { get; set; }
    }

    public static class StableDiffusionAPI
    {
        private static readonly string BaseEndPoint = "http://127.0.0.1:7860/";

        private static readonly HttpClient client = new HttpClient();

        public static Txt2ImgResponse Txt2Img(Txt2ImgRequest request)
        {
            const string URL = "sdapi/v1/txt2img";
            //I should probably just decode the images directly in here actually, and return those as a byte array
            Txt2ImgResponse output;

            try
            {
                var response = client.PostAsJsonAsync(BaseEndPoint + URL, request).Result;

                string result = response.Content.ReadAsStringAsync().Result;

                output = JsonSerializer.Deserialize<Txt2ImgResponse>(result) ?? new Txt2ImgResponse() { Exception = new Exception("json failed to be parsed") }; ;
            }
            catch (Exception ex)
            {
                output = new Txt2ImgResponse() { Exception = ex };
            }

            return output;
        }
    }
}
