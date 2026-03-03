using JADirect.Data.Repositories;
using JADirect.Domain.Entities;
using JADirect.Domain.Enums;
using JADirect.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JADirect.Web.Controllers;

/// <summary>
/// Controlador responsável por gerenciar as telas de usuários.
/// Acesso restrito a usuários com perfil 'Manager'
/// </summary>
[Authorize(Roles = "Manager")]
public class UsersController : Controller
{
    // Importando o repositório para dentro do controller
    private readonly UserRepository _userRepository;

    public UsersController(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// Função que lista todos os usuário por meio da função GetAll do UserRepository.cs
    /// </summary>
    /// <returns></returns>
    public IActionResult Index(string? searchString)
    {
        var users = _userRepository.GetAll(searchString);
        return View(users);
    }

    /// <summary>
    /// Essa ação apenas abre a tela de cadastro.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// A ação POST recebe os dados do formulário e chama a função Add no arquivos UserRespository para realizar o cadastro.
    /// O cadastro acontece apenas após a verificação se o e-mail já está cadastrado.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="plainPassord"></param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(User user, string plainPassword)
    {
        
        ModelState.Remove("PasswordHash");
        ModelState.Remove("CreatedAt");
        ModelState.Remove("Status");
        
        //1. Validar se o Model está consistente de acordo com as DataAnnotations da Entidade
        if (!ModelState.IsValid)
        {
            return View(user);
        }
        
        //2. Blindagem contra valores nulos antes de processar o Hash
        if (string.IsNullOrWhiteSpace(plainPassword))
        {
            ModelState.AddModelError("plainPassword", "A temporary password is required for new accounts.");
            return View(user);
        }
        
        // 3. Verificação de Unicidade: O e-mail é a chave de login, não pode ser duplicado
        var existingUser = _userRepository.GetByEmail(user.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("email", "This email address is already registered in the system.");
            return View(user);
        }

        try
        {
            // 4. Preparação da Entidade
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
            user.CreatedAt = DateTime.Now;
            user.Status = UserStatus.Active;

            // 5. Persistência
            _userRepository.Add(user);

            return RedirectToAction("Index");

        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "An internal error occurred while saving the user. Please try again.");
            return View(user);
        }
    }

    /// <summary>
    /// Ação que desativa o usuário.
    /// Chama o método 'Deactivate' do seu UserRepository.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult Deactivate(int id)
    {
        if (id <= 0)
        {
            return BadRequest();
        }
        _userRepository.Deactivate(id);
        return Ok();
    }


    /// <summary>
    /// /// Carrega os detalhes de um usuário para gestão administrativa.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet]
    public IActionResult Manage(int id)
    {
        var user = _userRepository.GetById(id);
        if (user == null)
        {
            return NotFound();
        }

        var viewModel = new UserManagementViewModel
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            Surname = user.Surname,
            Email = user.Email,
            Role = user.Role,
            Status = user.Status,
        };
        return View(viewModel);
    }
}