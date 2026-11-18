using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using toka_fitness.Models;

namespace toka_fitness.Controllers
{
    public class AccountController : Controller
    {
        private readonly string connectionString;

        public AccountController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection"); // Get the default connection string from appsettings.json
        }
        public IActionResult Register()
        {
            return View();
        }
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null) // If the user is already logged in, redirect them to the home page
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.UserId = HttpContext.Session.GetInt32("UserId");
                return View();
            }
        }
        public IActionResult Profile()
        {
            if (HttpContext.Session.GetString("Username") == null) // If the user is not logged in, redirect them to the home page
            {
                return RedirectToAction("Index", "Home");
            }
            else { return View(); }

        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
        [HttpPost]
        public IActionResult Register(User User)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // Pass the user inputs as parameters for security
                    string query = "INSERT INTO [User] (Username, Password, Email, AccountType) VALUES (@Username, @Password, @Email, @AccountType)";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", User.Username);
                        command.Parameters.AddWithValue("@Password", User.Password);
                        command.Parameters.AddWithValue("@Email", User.Email);

                        // Assuming the account type value is 1 for customer
                        command.Parameters.AddWithValue("@AccountType", 1);

                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    ViewBag.SuccessMessage = "Registration Successful!";
                    return View();
                }
            }
            catch (Exception ex) // Log the exception and tell the user an error occurred
            {
                Console.WriteLine($"An error occurred {ex.Message}");

                // Check if the Username is unique
                if (ex.Message.Contains("UQ_Username"))
                {
                    ViewBag.ErrorMessage = "That username is already taken, please try again.";
                    return View();
                }
                // Check if the Email is unique
                if (ex.Message.Contains("UQ_Email"))
                {
                    ViewBag.ErrorMessage = "That email address is already in use, please try again.";
                    return View();
                }
                // Do not show the user an error message if
                // They forget to enter an input in one of the input areas
                // Or if one of their inputs are too short
                // The error message is not shown because the user model's validation already shows an error message below the input box
                if (ex.Message.Contains("not supplied") || ex.Message.Contains("CK_Username_Len") || ex.Message.Contains("CK_Password_Len"))
                {
                    return View();
                }

                // If the error is something else, show a generic error message
                else
                {
                    ViewBag.ErrorMessage = "An error has occurred, please try again or contact support.";
                    return View();
                }

            }
        }
        [HttpPost]
        public IActionResult Login(User User)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // Pass the user inputs as parameters for security
                    string query = "SELECT * FROM [User] WHERE Username=@Username AND Password = @Password";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", User.Username);
                        command.Parameters.AddWithValue("@Password", User.Password);

                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();
                        if (reader.Read()) // If the query returns a result
                        {
                            ViewBag.SuccessMessage = "Login succsesful!";

                            // Remember who this is for the rest of the session
                            HttpContext.Session.SetInt32("UserId", (int)reader["UserId"]);
                            HttpContext.Session.SetString("Username", (string)reader["Username"]);
                            HttpContext.Session.SetString("IsAdmin", (int)reader["AccountType"] == 3 ? "True" : "False"); // Check if the account is an admin

                            // Direct the user to the home page
                            return RedirectToAction("Index", "Home");
                        }
                        else
                        {
                            ViewBag.ErrorMessage = "Incorrect password or username"; // Do not tell the user which one was wrong
                            return View();
                        }
                    }
                }
            }
            catch (Exception ex) // Log the exception and tell the user an error has occurred
            {
                Console.WriteLine($"An error has occurred {ex.Message}");

                // If the user forgets to enter an input, show an error message
                if (ex.Message.Contains("not supplied"))
                {
                    ViewBag.ErrorMessage = "Please enter your username and password";
                    return View();
                }

                // If the error is something else, show a generic error message
                else
                {
                    ViewBag.ErrorMessage = "An error has occurred, please try again or contact support.";
                    return View();
                }
            }
        }
        [HttpPost]
        public IActionResult ChangePassword(string newPassword, string currentPassword)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // If the user is not logged in, do not let them change password
                    if (userId == null)
                    {
                        return RedirectToAction("Login");
                    }
                    // Reject empty current passwords
                    if (string.IsNullOrEmpty(currentPassword))
                    {
                        TempData["WrongPassword"] = "Please enter your current password";
                        return RedirectToAction("Profile");
                    }
                    // Reject empty new passwords
                    if (string.IsNullOrEmpty(newPassword))
                    {
                        TempData["InvalidPassword"] = "Please enter a new password";
                        return RedirectToAction("Profile");
                    }
                    // Reject passwords shorter than 8 characters AND longer than 50 characters
                    if (newPassword.Length < 8 || newPassword.Length > 50)
                    {
                        TempData["InvalidPassword"] = "New password must be between 8-50 characters";
                        return RedirectToAction("Profile");
                    }

                    // Check current password is correct
                    string checkQuery = "SELECT COUNT (*) FROM [User] WHERE UserId=@UserId AND Password=@Password";
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@UserId", userId);
                        checkCommand.Parameters.AddWithValue("@Password", currentPassword);

                        connection.Open();
                        // If there are no results with the userid and the inputted password
                        if ((int)checkCommand.ExecuteScalar() == 0)
                        {
                            TempData["WrongPassword"] = "Current password is incorrect.";
                            return RedirectToAction("Profile");
                        }
                    }
                    // Do not update the password if it is the same as the old one
                    if (newPassword == currentPassword)
                    {
                        TempData["InvalidPassword"] = "New password can not be the same as the current password";
                        return RedirectToAction("Profile");
                    }
                    // Update the user's password after checking everything is valid
                    string updateQuery = "UPDATE [User] SET Password=@Password WHERE UserId=@UserId";
                    using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@UserId", userId);
                        updateCommand.Parameters.AddWithValue("@Password", newPassword);

                        updateCommand.ExecuteNonQuery();
                    }
                    TempData["Success"] = "Password change successful!";
                    return RedirectToAction("Profile");
                }
            }
            catch (Exception ex) // Log the exception and tell the user an error has occurred
            {
                Console.WriteLine($"An error has occurred, {ex.Message}");
                TempData["Error"] = "An error has occurred, please try again or contact support";
                return RedirectToAction("Profile");

            }
        }
    }
}