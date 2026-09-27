using System.Security.Claims;
using Cooperativa.Application;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Cooperativa.CooperativaSolution
{
    public class AuthController : Controller
    {
        private readonly UsuarioService _service;

        public AuthController(UsuarioService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string login, string senha)
        {
            var usuario = await _service.Autenticar(login, senha);

            if (usuario == null)
            {
                ViewBag.Error = "Login inválido";
                return View();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, usuario.Login),
                new(ClaimTypes.Role, usuario.Perfil.ToString())
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(identity));

            return RedirectToAction("Index", "Home");
        }
    }
}