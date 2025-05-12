using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Closy.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace closy.Pages
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<ApplicationUser> signInManager,
                         UserManager<ApplicationUser> userManager,
                         ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "L'email � obbligatoria")]
            [EmailAddress(ErrorMessage = "Inserisci un indirizzo email valido")]
            public string Email { get; set; }

            [Required(ErrorMessage = "La password � obbligatoria")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Ricordami")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Cancella i cookie esterni esistenti
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            // Default to a specific protected page if returnUrl is null or root
            if (string.IsNullOrEmpty(returnUrl) || returnUrl == Url.Content("~/"))
            {
                returnUrl = Url.Page("/Wardrobe/AllItems");
            }

            if (ModelState.IsValid)
            {
                // Tenta il login
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Utente loggato.");

                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (user != null)
                    {
                        user.LastLoginDate = DateTime.UtcNow; // Update last login date
                        await _userManager.UpdateAsync(user); // Save changes to the user

                        if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            return RedirectToPage("/Admin/Dashboard");
                        }
                        // For non-admins, redirect to the determined returnUrl (e.g., /Wardrobe/AllItems)
                        return LocalRedirect(returnUrl);
                    }
                    // If user is null for some reason after successful login, redirect to a safe page
                    return LocalRedirect(Url.Page("/Wardrobe/AllItems"));
                }

                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Account utente bloccato.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Tentativo di accesso non valido.");
                    return Page();
                }
            }

            // Se arriviamo qui, c'� stato un errore, mostra di nuovo il form
            return Page();
        }
    }
}