/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using Newtonsoft.Json;
using System.Dynamic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace Ubriaco.Util;

public class AIU
{
    public string Host { get; set; } = "http://localhost:11434";

    public static AIU I { get; private set; } = new AIU();

    public class AIResponseDetail
    {
        public string? Response { get; set; }
        public int TotalSeconds { get; set; }
        public bool IsError { get; set; } = false;
        public string? ErrorMessage { get; set; }
    }

    public AIU() { }
    public AIU(string host)
    {
        Host = host;
    }


    public async Task<string?> SendRequest(string prompt, string aiModelName)
    {
        using (var client = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(5)
        })
        {
            var startDate = DateTime.UtcNow;

            dynamic req = new ExpandoObject();
            req.model = aiModelName;
            req.prompt = prompt;
            req.stream = false;
            var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

            string url = Host + "/api/generate";

            using (var resp = await client.PostAsync(url, content))
            {
                var body = await resp.Content.ReadAsStringAsync();
                if (resp.IsSuccessStatusCode)
                {
                    dynamic? o = JsonConvert.DeserializeObject(body);
                    if (o == null)
                        return null;

                    if (o.response != null)
                    {
                        var endDate = DateTime.UtcNow;

                        var delta = endDate.Subtract(startDate);

                        string rcont = (string)o.response;

                        return rcont;
                    }

                    return null;
                }

            }

        }
        return null;

    }

    public async Task<AIResponseDetail> SendRequestReturnDetail(string prompt, string aiModelName, List<string>? imagesBase64 = null)
    {
        using (var client = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(5)
        })
        {
            var startDate = DateTime.UtcNow;

            dynamic req = new ExpandoObject();
            req.model = aiModelName;
            req.prompt = prompt;
            req.stream = false;

            if (imagesBase64 != null)
                req.images = imagesBase64;

            var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

            string url = Host + "/api/generate";

            using (var resp = await client.PostAsync(url, content))
            {
                var body = await resp.Content.ReadAsStringAsync();
                //Console.WriteLine("AI: SendRequest: RESPONSE: " + body);
                if (resp.IsSuccessStatusCode)
                {
                    dynamic? o = JsonConvert.DeserializeObject(body);
                    if (o == null)
                    {
                        return new AIResponseDetail()
                        {
                            IsError = true,
                            ErrorMessage = "Response object is invalid."
                        };
                    }

                    if (o.response != null)
                    {
                        var endDate = DateTime.UtcNow;

                        var delta = endDate.Subtract(startDate);

                        var totalSeconds = Convert.ToInt32(delta.TotalSeconds);

                        string rcont = (string)o.response;
                        return new AIResponseDetail()
                        {
                            Response = rcont,
                            TotalSeconds = totalSeconds
                        };
                    }

                    return new AIResponseDetail()
                    {
                        IsError = true,
                        ErrorMessage = "Response code is not success."
                    };
                }

            }

        }
        return new AIResponseDetail()
        {
            IsError = true,
            ErrorMessage = "Unknown error."
        };

    }


    public class ImageResponseDataItem
    {
        [JsonProperty("revised_prompt")]
        public string RevisedPrompt { get; set; } = string.Empty;
        [JsonProperty("url")]
        public string URL { get; set; } = string.Empty;
    }

    public class ImageResponse
    {
        [JsonProperty("data")]
        public List<ImageResponseDataItem> Data { get; set; } = new List<ImageResponseDataItem>();

        public string RevisedPrompt
        {
            get
            {
                if (Data.Count > 0)
                    return Data[0].RevisedPrompt;
                return string.Empty;
            }
        }

        public string URL
        {
            get
            {
                if (Data.Count > 0)
                    return Data[0].URL;
                return string.Empty;
            }
        }

        public string? FileName { get; set; }
        public byte[]? BinaryData { get; set; }
    }

    public const string ComfyUIRequestData = @"{
""prompt"": {
  ""9"": {
    ""inputs"": {
      ""filename_prefix"": ""z-image-turbo"",
      ""images"": [
        ""57:8"",
        0
      ]
    },
    ""class_type"": ""SaveImage"",
    ""_meta"": {
      ""title"": ""Save Image""
    }
  },
  ""57:30"": {
    ""inputs"": {
      ""clip_name"": ""qwen_3_4b.safetensors"",
      ""type"": ""lumina2"",
      ""device"": ""default""
    },
    ""class_type"": ""CLIPLoader"",
    ""_meta"": {
      ""title"": ""Load CLIP""
    }
  },
  ""57:29"": {
    ""inputs"": {
      ""vae_name"": ""ae.safetensors""
    },
    ""class_type"": ""VAELoader"",
    ""_meta"": {
      ""title"": ""Load VAE""
    }
  },
  ""57:33"": {
    ""inputs"": {
      ""conditioning"": [
        ""57:27"",
        0
      ]
    },
    ""class_type"": ""ConditioningZeroOut"",
    ""_meta"": {
      ""title"": ""ConditioningZeroOut""
    }
  },
  ""57:8"": {
    ""inputs"": {
      ""samples"": [
        ""57:3"",
        0
      ],
      ""vae"": [
        ""57:29"",
        0
      ]
    },
    ""class_type"": ""VAEDecode"",
    ""_meta"": {
      ""title"": ""VAE Decode""
    }
  },
  ""57:28"": {
    ""inputs"": {
      ""unet_name"": ""z_image_turbo_bf16.safetensors"",
      ""weight_dtype"": ""default""
    },
    ""class_type"": ""UNETLoader"",
    ""_meta"": {
      ""title"": ""Load Diffusion Model""
    }
  },
  ""57:27"": {
    ""inputs"": {
      ""text"": ""${PROMPT}"",
      ""clip"": [
        ""57:30"",
        0
      ]
    },
    ""class_type"": ""CLIPTextEncode"",
    ""_meta"": {
      ""title"": ""CLIP Text Encode (Prompt)""
    }
  },
  ""57:13"": {
    ""inputs"": {
      ""width"": ${WIDTH},
      ""height"": ${HEIGHT},
      ""batch_size"": 1
    },
    ""class_type"": ""EmptySD3LatentImage"",
    ""_meta"": {
      ""title"": ""EmptySD3LatentImage""
    }
  },
  ""57:11"": {
    ""inputs"": {
      ""shift"": 3,
      ""model"": [
        ""57:28"",
        0
      ]
    },
    ""class_type"": ""ModelSamplingAuraFlow"",
    ""_meta"": {
      ""title"": ""ModelSamplingAuraFlow""
    }
  },
  ""57:3"": {
    ""inputs"": {
      ""seed"": ${SEED},
      ""steps"": 8,
      ""cfg"": 1,
      ""sampler_name"": ""res_multistep"",
      ""scheduler"": ""simple"",
      ""denoise"": 1,
      ""model"": [
        ""57:11"",
        0
      ],
      ""positive"": [
        ""57:27"",
        0
      ],
      ""negative"": [
        ""57:33"",
        0
      ],
      ""latent_image"": [
        ""57:13"",
        0
      ]
    },
    ""class_type"": ""KSampler"",
    ""_meta"": {
      ""title"": ""KSampler""
    }
  }
}
}";

    public async Task<ImageResponse> SendImageRequestComfyUI(string prompt, int width = 1024, int height = 1024)
    {
        string promptID;
        string? fileName = null, subFolder = null, outputType = null;
        using (var client = new HttpClient())
        {
            prompt = prompt.Replace("\"", string.Empty).Replace("\\", string.Empty);
            var reqStr = ComfyUIRequestData.Replace("${PROMPT}", prompt).Replace("${WIDTH}", width.ToString()).Replace("${HEIGHT}", height.ToString()).Replace("${SEED}", Random.Shared.NextInt64(1, long.MaxValue).ToString());

            Console.WriteLine("AI: SendImageRequestComfyUI: REQUEST: " + reqStr);
            var content = new StringContent(reqStr, Encoding.UTF8, "application/json");

            // PROMPT
            string url = Host + "/prompt";
            using (var resp = await client.PostAsync(url, content))
            {
                var body = await resp.Content.ReadAsStringAsync();
                Console.WriteLine("AI: SendImageRequestComfyUI: RESPONSE: " + body);
                if (resp.IsSuccessStatusCode)
                {
                    dynamic? data = JsonConvert.DeserializeObject(body);
                    if (data == null) throw new Exception("Failed to generate image: Response data failed to parse.");
                    if (data.prompt_id == null) throw new Exception("Failed to generate image: Response data: prompt_id is null.");
                    promptID = (string)data.prompt_id;
                }
                else
                {
                    Console.WriteLine("AI: SendImageRequestComfyUI: ERROR: " + body);
                    throw new Exception("AI: SendImageRequestComfyUI: ERROR: " + body);
                }
            }

            // QUEUE
            while (true)
            {
                url = Host + "/queue";
                using (var resp = await client.GetAsync(url))
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    //Console.WriteLine("AI: SendImageRequestComfyUI: QUEUE: RESPONSE: " + body);
                    if (resp.IsSuccessStatusCode)
                    {
                        if (body.Contains(promptID))
                        {
                            //Console.WriteLine("Waiting for generation process...");
                            await Task.Delay(1000);
                        }
                        else
                            break; //exit
                    }
                    else
                    {
                        Console.WriteLine("AI: SendImageRequestComfyUI: ERROR: " + body);
                        throw new Exception("AI: SendImageRequestComfyUI: ERROR: " + body);
                    }
                }
            }

            // QUEUE
            url = Host + "/history/" + promptID;
            using (var resp = await client.GetAsync(url))
            {
                var body = await resp.Content.ReadAsStringAsync();
                Console.WriteLine("AI: SendImageRequestComfyUI: HISTORY: RESPONSE: " + body);
                if (resp.IsSuccessStatusCode)
                {
                    var data = JObject.Parse(body);

                    var obj = data[promptID] as JObject
                        ?? throw new Exception("Failed to generate image: promptID not found.");

                    var outputs = obj["outputs"] as JObject
                        ?? throw new Exception("Failed to generate image: HISTORY: NO OUTPUTS");

                    bool found = false;

                    foreach (var outputProp in outputs.Properties())
                    {
                        var output = outputProp.Value as JObject;
                        if (output == null) continue;

                        var images = output["images"] as JArray;
                        if (images == null || images.Count == 0) continue;

                        foreach (var img in images)
                        {
                            fileName = img["filename"]?.ToString();
                            subFolder = img["subfolder"]?.ToString();
                            outputType = img["type"]?.ToString();

                            found = true;
                            break;
                        }

                        if (found) break;
                    }

                    if (!found)
                        throw new Exception("Failed to generate image: HISTORY: NO IMAGES");

                }
                else
                {
                    Console.WriteLine("AI: SendImageRequestComfyUI: ERROR: " + body);
                    throw new Exception("AI: SendImageRequestComfyUI: ERROR: " + body);
                }
            }

            if (fileName == null || subFolder == null || outputType == null)
                throw new Exception("AI: SendImageRequestComfyUI: Missing VIEW DATA.");

            // VIEW
            url = Host + $"/view?filename={fileName}&subfolder={subFolder}&type={outputType}";
            using (var resp = await client.GetAsync(url))
            {
                byte[] data = await resp.Content.ReadAsByteArrayAsync();

                return new ImageResponse()
                {
                    FileName = fileName,
                    BinaryData = data
                };
            }
        }
    }

    public async Task UpscaleImage(byte[] imageBytes)
    {
        using (var http = new HttpClient())
        {
            using (var form = new MultipartFormDataContent())
            {
                var imageContent = new ByteArrayContent(imageBytes);
                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
                form.Add(imageContent, "image", "image.png");
                form.Add(new StringContent("input"), "type");
                form.Add(new StringContent(""), "subfolder");
                form.Add(new StringContent("true"), "overwrite");
                using (var uploadResponse = await http.PostAsync($"{Host}/upload/image", form))
                {
                    uploadResponse.EnsureSuccessStatusCode();
                    var uploadJson = await uploadResponse.Content.ReadAsStringAsync();
                    Console.WriteLine(uploadJson);
                    //todo
                }
            }
        }
    }


    public class Image
    {
        public string URL { get; set; }
        public byte[] Data { get; set; }
        public string MimeType { get; set; }
        public string RevisedPrompt { get; set; }
        public string Ext { get; set; }

        public Image(string url, byte[] data, string mimeType, string revisedPrompt, string ext)
        {
            URL = url;
            Data = data;
            MimeType = mimeType;
            RevisedPrompt = revisedPrompt;
            Ext = ext;
        }
    }


}
