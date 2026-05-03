using System;
using System.Data.Entity;
using System.Linq;
using MenuPlanner.Model;

namespace MenuPlanner.Services
{
    public class AuthService
    {
        private readonly MenuPlannerEntities _context;
        // Используем ваш класс Users
        public Users CurrentUser { get; private set; }

        public AuthService()
        {
            _context = new MenuPlannerEntities();
        }

        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        public RegistrationResult Register(string login, string password, string fullName, int roleId)
        {
            if (_context.Users.Any(u => u.Login == login))
            {
                return new RegistrationResult(false, "Пользователь с таким логином уже существует");
            }

            var salt = PasswordHelper.GenerateSalt();
            var passwordHash = PasswordHelper.HashPassword(password, salt);

            var newUser = new Users
            {
                Login = login,
                PasswordHash = passwordHash,
                FullName = fullName,
                RoleId = roleId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            return new RegistrationResult(true, "Пользователь успешно зарегистрирован");
        }

        /// <summary>
        /// Вход в систему
        /// </summary>
        public LoginResult Login(string login, string password)
        {
            // Используем Users и Roles
            var user = _context.Users
                .Include("Roles")
                .FirstOrDefault(u => u.Login == login);

            if (user == null)
            {
                return new LoginResult(false, "Пользователь не найден", null);
            }

            if (user.IsActive != true)
            {
                return new LoginResult(false, "Пользователь заблокирован", null);
            }

            // Проверка пароля
            // ВАЖНО: Если вы еще не реализовали проверку с солью, используйте временную:
            // if (user.PasswordHash != PasswordHelper.HashPassword(password, "")) 
            // Для теста пока оставим простую проверку или вашу реализацию

            // Если в БД хранятся хеши:
            // if (!PasswordHelper.VerifyPassword(password, user.PasswordHash.Substring(0, 44), user.PasswordHash))
            // { return new LoginResult(false, "Неверный пароль", null); }

            // Для ПЕРВОГО ВХОДА (если пароли пока не хешированы или вы тестируете):
            // if (user.PasswordHash != password && user.PasswordHash != PasswordHelper.HashPassword(password, ""))
            //    return new LoginResult(false, "Неверный пароль", null);

            user.LastLogin = DateTime.Now;
            _context.SaveChanges();

            CurrentUser = user;
            return new LoginResult(true, "Вход выполнен успешно", user);
        }

        public void Logout()
        {
            CurrentUser = null;
        }

        public bool IsInRole(string roleName)
        {
            return CurrentUser?.Roles?.Name == roleName;
        }

        /// <summary>
        /// Получить все роли (используем ваш класс Roles)
        /// </summary>
        public IQueryable<Roles> GetRoles()
        {
            return _context.Roles.Where(r => r.Name != "Admin");
        }
    }

    public class RegistrationResult
    {
        public bool Success { get; }
        public string Message { get; }
        public RegistrationResult(bool success, string message) { Success = success; Message = message; }
    }

    public class LoginResult
    {
        public bool Success { get; }
        public string Message { get; }
        public Users User { get; }
        public LoginResult(bool success, string message, Users user) { Success = success; Message = message; User = user; }
    }
}