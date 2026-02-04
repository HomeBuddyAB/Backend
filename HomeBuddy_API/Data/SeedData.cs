using HomeBuddy_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace HomeBuddy_API.Data;

public static class SeedData
{
    public static void EnsureSeeded(ApplicationDbContext db)
    {
        // Seed Admins
        if (!db.Admins.Any())
        {
            for (int i = 1; i <= 30; i++)
            {
                CreatePasswordHash("Admin123!", out string hash, out string salt);
                db.Admins.Add(new Admin
                {
                    UserName = $"admin{i}",
                    PasswordHash = hash,
                    PasswordSalt = salt
                });
            }
            db.SaveChanges();
        }

        // Seed Users
        if (!db.Users.Any())
        {
            for (int i = 1; i <= 30; i++)
            {
                CreatePasswordHash("User123!", out string hash, out string salt);
                db.Users.Add(new User
                {
                    Email = $"user{i}@example.com",
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    Cart = "{}"
                });
            }
            db.SaveChanges();
        }

        // Seed Categories (HomeBuddy IKEA-like home improvement store)
        if (!db.Categories.Any())
        {
            db.Categories.AddRange(
                new Category { Name = "Furniture", Slug = "furniture" },
                new Category { Name = "Materials", Slug = "materials" },
                new Category { Name = "Power Tools", Slug = "power-tools" },
                new Category { Name = "Lighting", Slug = "lighting" }
            );
            db.SaveChanges();
        }

    }
    private static void CreatePasswordHash(string password, out string hash, out string salt)
    {
        using var hmac = new HMACSHA512();
        salt = Convert.ToBase64String(hmac.Key);
        hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }
}