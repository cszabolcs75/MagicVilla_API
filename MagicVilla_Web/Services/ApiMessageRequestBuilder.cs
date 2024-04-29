using MagicVilla_Utility;
using MagicVilla_Web.Models;
using MagicVilla_Web.Services.IServices;
using Newtonsoft.Json;
using System.Text;

namespace MagicVilla_Web.Services
{
    public class ApiMessageRequestBuilder : IApiMessageRequestBuilder
    {
        public HttpRequestMessage Build(APIRequest apiRequest)
        {
            HttpRequestMessage message = new();
            if (apiRequest.ContentType == Details.ContentType.MultipartFormData)
            {
                message.Headers.Add("Accept", "*/*");
            }
            else
            {
                message.Headers.Add("Accept", "application/json");
            }

            message.RequestUri = new Uri(apiRequest.Url);

            if (apiRequest.ContentType == Details.ContentType.MultipartFormData)
            {
                var content = new MultipartFormDataContent();

                foreach (var item in apiRequest.Data.GetType().GetProperties())
                {
                    var value = item.GetValue(apiRequest.Data);
                    if (value is FormFile)
                    {
                        var file = (FormFile)value;
                        if (file != null)
                        {
                            content.Add(new StreamContent(file.OpenReadStream()), item.Name, file.FileName);
                        }
                    }
                    else
                    {
                        content.Add(new StringContent(value == null ? "" : value.ToString()), item.Name);
                    }
                }

                message.Content = content;
            }
            else
            {
                if (apiRequest.Data != null)
                {
                    message.Content = new StringContent(JsonConvert.SerializeObject(apiRequest.Data),
                        Encoding.UTF8, "application/json");
                }
            }


            switch (apiRequest.ApiType)
            {
                case MagicVilla_Utility.Details.ApiType.GET:
                    message.Method = HttpMethod.Get;
                    break;
                case MagicVilla_Utility.Details.ApiType.POST:
                    message.Method = HttpMethod.Post;
                    break;
                case MagicVilla_Utility.Details.ApiType.PUT:
                    message.Method = HttpMethod.Put;
                    break;
                case MagicVilla_Utility.Details.ApiType.DELETE:
                    message.Method = HttpMethod.Delete;
                    break;
            }

            return message;
        }
    }
}
