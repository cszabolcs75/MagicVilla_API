﻿using AutoMapper.Internal;
using MagicVilla_Utility;
using MagicVilla_Web.Models;
using MagicVilla_Web.Models.Dto;
using MagicVilla_Web.Services.IServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Newtonsoft.Json;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace MagicVilla_Web.Services
{
    public class BaseService : IBaseService
    {
        public APIResponse responseModel { get; set; }
        public IHttpClientFactory httpClient{ get; set; }
        private readonly ITokenProvider _tokenProvider;
        private readonly IApiMessageRequestBuilder _apiMessageRequestBuilder;

        private readonly string VillaApiUrl;
        private IHttpContextAccessor _httpContextAccessor;

        public BaseService(IHttpClientFactory httpClient, ITokenProvider tokenProvider, IConfiguration configuration, 
            IHttpContextAccessor httpContextAccessor, IApiMessageRequestBuilder apiMessageRequestBuilder)
        {
            responseModel = new();
            this.httpClient = httpClient;
            _tokenProvider = tokenProvider;
            VillaApiUrl = configuration.GetValue<string>("ServiceUrls:VillaAPI");
            _httpContextAccessor = httpContextAccessor;
            _apiMessageRequestBuilder = apiMessageRequestBuilder;

        }

        public async Task<T> SendAsync<T>(APIRequest apiRequest, bool withBearer = true)
        {
            try
            {
                var client = httpClient.CreateClient("MagicAPI");

                var messageFactory = () =>
                {
                    return _apiMessageRequestBuilder.Build(apiRequest);
                };

                HttpResponseMessage httpResponseMessage = null;



                httpResponseMessage = await SendWithRefreshTokenAsync(client, messageFactory, withBearer);

                APIResponse FinalApiResponse = new()
                {
                    IsSuccess = false
                };

                try
                {

                    switch (httpResponseMessage.StatusCode)
                    {
                        case HttpStatusCode.NotFound:
                            FinalApiResponse.ErrorMessages = new List<string>() { "Not Found"};
                            break;
                        case HttpStatusCode.Forbidden:
                            FinalApiResponse.ErrorMessages = new List<string>() { "Access Denied" };
                            break;
                        case HttpStatusCode.Unauthorized:
                            FinalApiResponse.ErrorMessages = new List<string>() { "Unauthorized" };
                            break;
                        case HttpStatusCode.InternalServerError:
                            FinalApiResponse.ErrorMessages = new List<string>() { "Internal Server Error" };
                            break;
                        default:
                            var apiContent = await httpResponseMessage.Content.ReadAsStringAsync();
                            FinalApiResponse.IsSuccess = true;
                            FinalApiResponse = JsonConvert.DeserializeObject<APIResponse>(apiContent);
                            break;

                    }                 
                }
                catch (Exception e)
                {

                    FinalApiResponse.ErrorMessages = new List<string>() { "Error Encountered", e.Message.ToString() };
                }
                var res = JsonConvert.SerializeObject(FinalApiResponse);
                var returnObj = JsonConvert.DeserializeObject<T>(res);
                return returnObj;
            }
            catch(AuthException)
            {
                throw;
            }
            catch (Exception e)
            {
                var dto = new APIResponse
                {
                    ErrorMessages = new List<string> { Convert.ToString(e.Message) },
                    IsSuccess = false,
                };
                var res = JsonConvert.SerializeObject(dto);
                var APIResponse = JsonConvert.DeserializeObject<T>(res);
                return APIResponse;
            }
        }

        private async Task<HttpResponseMessage> SendWithRefreshTokenAsync(HttpClient httpClient, 
            Func<HttpRequestMessage> httpRequestMessageFactory, bool withBearer = true)
        {
            if (!withBearer)
            {
                return await httpClient.SendAsync(httpRequestMessageFactory());
            }
            else
            {
                TokenDTO tokenDTO = _tokenProvider.GetToken();
                if (tokenDTO != null && !string.IsNullOrEmpty(tokenDTO.AccessToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenDTO.AccessToken);   
                }

                try
                {
                    var response = await httpClient.SendAsync(httpRequestMessageFactory());
                    if (response.IsSuccessStatusCode)
                    {
                        if (!response.IsSuccessStatusCode && response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            await InvokeRefreshTokenEndpoint(httpClient, tokenDTO.AccessToken, tokenDTO.RefreshToken);
                             response = await httpClient.SendAsync(httpRequestMessageFactory());
                            return response;
                        }
                        return response;
                    }

                    return response;

                }
                catch(AuthException)
                {
                    throw;
                }
                catch (HttpRequestException ex)
                {
                    if (ex.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        await InvokeRefreshTokenEndpoint(httpClient, tokenDTO.AccessToken, tokenDTO.RefreshToken);
                        return await httpClient.SendAsync(httpRequestMessageFactory());
                        
                    }
                    throw;
                }
            }

           

           
        }


        private async Task InvokeRefreshTokenEndpoint(HttpClient httpClient, string existringAccessToken, string existringRefreshToken)
        {
            HttpRequestMessage message = new();
            message.Headers.Add("Accept", "application/json");
            message.RequestUri = new Uri($"{VillaApiUrl}/api/Users/refresh");
            message.Method = HttpMethod.Post;
            message.Content = new StringContent(JsonConvert.SerializeObject(new TokenDTO()
            {
                AccessToken = existringAccessToken,
                RefreshToken = existringRefreshToken
            }), Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(message);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<APIResponse>(content);

            if (apiResponse?.IsSuccess != true)
            {
                await _httpContextAccessor.HttpContext.SignOutAsync();
                _tokenProvider.ClearToken();
                throw new AuthException();
            }
            else
            {
                var tokenDataStr = JsonConvert.SerializeObject(apiResponse.Result);
                var tokenDto = JsonConvert.DeserializeObject<TokenDTO>(tokenDataStr);

                if (tokenDto != null && !string.IsNullOrEmpty(tokenDto.AccessToken))
                {
                    await SignInWithNewTokens(tokenDto);
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenDto.AccessToken);
                }
            }
        }

        private async Task SignInWithNewTokens(TokenDTO tokenDTO)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(tokenDTO.AccessToken);

            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, jwt.Claims.FirstOrDefault(x => x.Type == "unique_name").Value));
            identity.AddClaim(new Claim(ClaimTypes.Role, jwt.Claims.FirstOrDefault(x => x.Type == "role").Value));
            var principal = new ClaimsPrincipal(identity);
            await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);


            _tokenProvider.SetToken(tokenDTO);
        }
    }
}
