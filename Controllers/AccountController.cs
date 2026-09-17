using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcommerceApp.Controllers
{
    // Constructor primario: userManager y signInManager quedan disponibles
    // en toda la clase sin declarar campos ni constructor explícito.
    public class AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender) : Controller
    {
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await userManager.FindByEmailAsync(model.Email);
                return RedirectForRole(user);
            }

            ModelState.AddModelError(string.Empty, "Credenciales inválidas");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Address = model.Address
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "User");

                await signInManager.SignInAsync(user, isPersistent: false);

                return RedirectForRole(user);
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Products");
        }

        // --- Restablecimiento de contraseña ---

        // Formulario para pedir el correo de recuperación ("¿Olvidaste tu contraseña?").
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword() => View();

        // Genera el enlace de restablecimiento y lo "envía" por correo (SMTP o consola).
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                // Token único de Identity para restablecer la contraseña.
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var email = user.Email ?? model.Email;
                var resetLink = Url.Action(nameof(ResetPassword), "Account",
                    new { email, token }, protocol: Request.Scheme);

                await emailSender.SendEmailAsync(
                    email,
                    "Restablecer tu contraseña — Tren al Sur",
                    $"Hacé clic en el siguiente enlace para restablecer tu contraseña:<br/>" +
                    $"<a href=\"{resetLink}\">Restablecer contraseña</a>");
            }

            // Siempre se muestra la misma confirmación (no revela qué correos existen).
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        // Confirmación tras pedir el enlace de recuperación.
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation() => View();

        // Formulario de contraseña nueva, abierto desde el enlace del correo.
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? email = null, string? token = null)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return BadRequest("El enlace de restablecimiento es inválido o expiró.");

            return View(new ResetPasswordViewModel { Email = email, Token = token });
        }

        // Guarda la contraseña nueva.
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await userManager.FindByEmailAsync(model.Email);

            // Si el usuario no existe, se muestra la confirmación igual (evita enumeración).
            if (user == null)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            var result = await userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // Confirmación tras restablecer la contraseña.
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation() => View();

        // --- Login social (Google/GitHub) ---

        // Inicia el desafío hacia el proveedor externo (botón "Iniciar sesión con...").
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLogin(string provider, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(provider))
                return RedirectToAction(nameof(Login));

            // Si el proveedor no está configurado (sin credenciales), avisamos amigablemente.
            var schemeProvider = HttpContext.RequestServices
                .GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await schemeProvider.GetSchemeAsync(provider);
            if (scheme == null)
            {
                ModelState.AddModelError(string.Empty,
                    "Este método de inicio de sesión todavía no está configurado.");
                return View(nameof(Login), new LoginViewModel());
            }

            returnUrl ??= Url.Content("~/");
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        // Callback del proveedor externo: si la cuenta ya está vinculada, inicia sesión;
        // si no, deriva al formulario para completar el registro.
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
        {
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction(nameof(Login));

            var signInResult = await signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                var existingUser = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                return RedirectForRole(existingUser);
            }

            if (signInResult.IsLockedOut)
                return RedirectToAction(nameof(Login));

            // GitHub puede no exponer el email público: en ese caso se pide en el formulario.
            var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

            if (!string.IsNullOrEmpty(email) && await userManager.FindByEmailAsync(email) != null)
            {
                // Ya existe una cuenta local con ese email: no se vincula automáticamente.
                ModelState.AddModelError(string.Empty,
                    "Ya existe una cuenta con ese correo. Inicia sesión con tu contraseña y vincula tu cuenta desde tu perfil.");
                return View(nameof(Login), new LoginViewModel { Email = email });
            }

            // Primer ingreso con una cuenta externa: el usuario se registra automáticamente
            // con los datos que trae el proveedor (nombre y email), sin formularios intermedios.
            if (string.IsNullOrEmpty(email))
            {
                return View("ExternalRegister", new ExternalRegisterViewModel
                {
                    Email = string.Empty,
                    ReturnUrl = returnUrl ?? Url.Content("~/")
                });
            }

            var fullName = info.Principal.FindFirstValue(ClaimTypes.Name)
                ?? info.Principal.FindFirstValue("urn:github:name")
                ?? info.Principal.FindFirstValue(ClaimTypes.GivenName)
                ?? "Usuario";

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName
            };

            var createResult = await userManager.CreateAsync(user);
            if (createResult.Succeeded)
            {
                await userManager.AddLoginAsync(user, info);
                await userManager.AddToRoleAsync(user, "User");
                await signInManager.SignInAsync(user, isPersistent: false);
                return RedirectForRole(user);
            }

            foreach (var error in createResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(nameof(Login), new LoginViewModel { Email = email });
        }

        // Muestra el formulario para completar datos del registro externo (GET directo).
        [AllowAnonymous]
        public async Task<IActionResult> ExternalRegister(string? returnUrl = null)
        {
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction(nameof(Login));

            var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            return View(new ExternalRegisterViewModel
            {
                Email = email,
                ReturnUrl = returnUrl ?? Url.Content("~/")
            });
        }

        // Completa el registro: crea la cuenta local, la vincula al proveedor y la asigna a rol User.
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalRegister(ExternalRegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction(nameof(Login));

            if (await userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError(string.Empty,
                    "Ya existe una cuenta con ese correo. Inicia sesión con tu contraseña.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Address = model.Address
            };

            var result = await userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                await userManager.AddLoginAsync(user, info);
                await userManager.AddToRoleAsync(user, "User");
                await signInManager.SignInAsync(user, isPersistent: false);
                return RedirectForRole(user);
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // Helper: redirige según rol del usuario.
        // Admin -> panel de administración; resto -> Home.
        private IActionResult RedirectForRole(ApplicationUser? user)
        {
            if (user != null && userManager.IsInRoleAsync(user, "Admin").GetAwaiter().GetResult())
                return RedirectToAction("Index", "Admin");

            return RedirectToAction("Index", "Home");
        }
    }
}
