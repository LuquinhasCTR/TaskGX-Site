using Microsoft.AspNetCore.Mvc;
using TaskGX.Models;

namespace TaskGX.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Termos()
        {
            return View();
        }
        public IActionResult Privacidade()
        {
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }



        // ... outras actions (Index, Login, etc.) ...

        // GET: /Home/Registro
        [HttpGet]
        public IActionResult Registrar()
        {
            // Se já estiver autenticado, você pode redirecionar para o dashboard:
            // if (User?.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
            return View();
        }

        // POST: /Home/Registro
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Registrar(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // ModelState contém os erros de validação (exibe na view)
                return View();
            }

            // TODO: substituir por lógica real de criação de usuário (usuário service / Identity)
            // Exemplo de validação simples de existência de e-mail:
            bool emailJaExiste = false; // checar no banco
            if (emailJaExiste)
            {
                ModelState.AddModelError(nameof(model.Email), "Já existe uma conta com esse e-mail.");
                return View(model);
            }

            // Simulação de criação bem-sucedida:
            // - Persistir usuário
            // - Sempre trate a senha com hashing / salted hashing (ex.: Identity / BCrypt)
            TempData["Success"] = "Conta criada com sucesso. Agora faça login.";
            return RedirectToAction("Login");
        }
    }
}