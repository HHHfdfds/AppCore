using AppCore.App.Repositories;
using AppCore.Client.Controllers;
using AppCore.Models;
using AppCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace AppCore.Controllers;

public class AccountManagementController : Controller
{
    private readonly AccountManagerRepository _accountRepository;
    private readonly UserManager<Account> _userManager;
    private readonly ILogger<AccountManagementController> _logger;
    SignInManager<Account> _signInManager;
    public AccountManagementController(AccountManagerRepository accountRepository, UserManager<Account> userManager, ILogger<AccountManagementController> logger, SignInManager<Account> signInManager)
    {
        _accountRepository = accountRepository;
        _userManager = userManager;
        _logger = logger;
        _signInManager = signInManager;
    }
    [Route("Admin/AccountManage")]
    public async Task<IActionResult> Index()
    {
        var accounts = await _accountRepository.GetAllAccountsAsync();
        return View(accounts);
    }
    
    public IActionResult Register()
    {
        var errors = TempData["Errors"] as string;
        // Truyền danh sách lỗi vào view để hiển thị
        ViewBag.Errors = errors;
        return View();
    }

    public async Task<IActionResult> PostRegister(RegisterRequest registerRequest) 
    {
        if (registerRequest == null || registerRequest.Password == null)
        {
            // Xử lý khi dữ liệu đăng ký không hợp lệ
            return View("Register", registerRequest);
        }
        var user = new User
        {
            UserName = registerRequest.Email,
            FirstName = registerRequest.FirstName,
            LastName = registerRequest.LastName,
            PhoneNumber = registerRequest.PhoneNumber,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        var result = await _userManager.CreateAsync(user, registerRequest.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");
            TempData["Success"] = "Register Successfully";
            return RedirectToAction("Index");
        }
        TempData["Errors"] = "Choose another email";
        return RedirectToAction("Register");
    }
    [Route("Admin/AccountManage/Edit/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var account = await _accountRepository.GetAccountByIdAsync(id);

        if (account == null)
        {
            return NotFound();
        }

        var roles = await _accountRepository.GetRolesAsync(account);
        var model = new EditAccountViewModel
        {
            Account = account, // Add this line to include the account in the model
            Roles = roles
        };

        return View(model);
    }
    [HttpPost]
    public async Task<IActionResult> Edit(EditAccountViewModel model)
    {
        var account = await _accountRepository.GetAccountByIdAsync(model.Account.Id);

        if (account == null)
        {
            return NotFound();
        }

        // Update account properties
        
        account.PhoneNumber = model.Account.PhoneNumber;
        account.UserName = model.Account.UserName;
        account.LastName = model.Account.LastName;
        account.FirstName = model.Account.FirstName;
        account.Email = model.Account.Email;
        // Update account
        await _accountRepository.UpdateAccountAsync(account);

        // Update roles
        var selectedRoles = model.SelectedRoles ?? new List<string>();
        foreach (var role in model.Roles)
        {
            if (selectedRoles.Contains(role))
            {
                await _accountRepository.AddToRoleAsync(account, role);
            }
            else
            {
                await _accountRepository.RemoveFromRoleAsync(account, role);
            }
        }

        return RedirectToAction("Index");
    }


    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _accountRepository.DeleteAccountAsync(id);

        if (result)
        {
            return RedirectToAction("Index");
        }

        return Json(new { success = false, message = "Delete unsuccessful." });
    }

}
