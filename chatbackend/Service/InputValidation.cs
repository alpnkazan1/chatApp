using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

public static class InputValidation
{
    // Regular expression to allow only alphanumeric characters and underscores
    private static readonly Regex _usernameRegex = new Regex("^[a-zA-Z0-9_]+$", RegexOptions.Compiled);
    public static IActionResult ValidateUsername(string username, ILogger logger)
    {
        if (string.IsNullOrEmpty(username))
        {
            logger.LogWarning("Username is required.");
            return new BadRequestObjectResult("Username is required.");
        }

        // Check for spaces
        if (username.Contains(" "))
        {
            logger.LogWarning("Username cannot contain spaces.");
            return new BadRequestObjectResult("Username cannot contain spaces.");
        }

        // Check for invalid characters using Regex
        if (!_usernameRegex.IsMatch(username))
        {
            logger.LogWarning("Username contains invalid characters. Only alphanumeric characters and underscores are allowed.");
            return new BadRequestObjectResult("Username contains invalid characters. Only alphanumeric characters and underscores are allowed.");
        }

        if (username.Length < 3)
        {
            logger.LogWarning("Username must be at least 3 characters long.");
            return new BadRequestObjectResult("Username must be at least 3 characters long.");
        }

        if (username.Length > 20)
        {
            logger.LogWarning("Username cannot be longer than 20 characters.");
            return new BadRequestObjectResult("Username cannot be longer than 20 characters.");
        }

        return null; // Validation passed
    }
}