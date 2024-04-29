using MagicVilla_Utility;
using MagicVilla_Web.Models;
using MagicVilla_Web.Models.Dto;
using MagicVilla_Web.Services.IServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MagicVilla_Web.Controllers
{
    public class AuthController : Controller
    {

        private readonly IAuthService _authService;
        private readonly ITokenProvider _tokenProvider;

        public AuthController(IAuthService authService, ITokenProvider tokenProvider)
        {
            _authService = authService;
            _tokenProvider = tokenProvider;

        }

        [HttpGet]
        public IActionResult Login()
        {
            LoginRequestDTO obj = new LoginRequestDTO();
            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequestDTO obj)
        {
            APIResponse result = await _authService.LoginAsync<APIResponse>(obj);
            if (result !=null && result.IsSuccess)
            {
                TokenDTO model = JsonConvert.DeserializeObject<TokenDTO>(Convert.ToString(result.Result));

                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(model.AccessToken);

                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim(ClaimTypes.Name, jwt.Claims.FirstOrDefault(x => x.Type == "unique_name").Value));
                identity.AddClaim(new Claim(ClaimTypes.Role, jwt.Claims.FirstOrDefault(x=>x.Type=="role").Value));
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);


                _tokenProvider.SetToken(model);
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError("CustomError", result.ErrorMessages.FirstOrDefault());
                return View(obj);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            var roleList = new List<SelectListItem>()
            {
                new SelectListItem{Text=Details.Admin, Value=Details.Admin},
                new SelectListItem{Text=Details.Customer, Value=Details.Customer},
            };
            ViewBag.RoleList = roleList;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegistrationRequestDTO obj)
        {
            if (string.IsNullOrEmpty(obj.Role))
            {
                obj.Role = Details.Customer;
            }
            APIResponse result = await _authService.RegisterAsync<APIResponse>(obj);
            if (result != null && result.IsSuccess)
            {
                return RedirectToAction("Login");
            }
            var roleList = new List<SelectListItem>()
            {
                new SelectListItem{Text=Details.Admin, Value=Details.Admin},
                new SelectListItem{Text=Details.Customer, Value=Details.Customer},
            };
            ViewBag.RoleList = roleList;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            var token = _tokenProvider.GetToken();
            await _authService.LogoutAsync<APIResponse>(token);
           _tokenProvider.ClearToken();
            
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            
            return View();
        }
    }
}
