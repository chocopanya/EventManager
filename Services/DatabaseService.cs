using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        _connectionString = ConfigurationManager.ConnectionStrings["PostgreSQL"].ConnectionString;
    }

    // Хеширование пароля
    public string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }
    }

    // Регистрация нового пользователя
    public bool RegisterUser(string email, string password, string avatarPath)
    {
        try
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();

                // Проверка, существует ли уже пользователь с таким email
                using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE email = @email", conn))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    var count = (long)cmd.ExecuteScalar();
                    if (count > 0) return false;
                }

                // Хеширование пароля
                var passwordHash = HashPassword(password);
                var avatarUrl = string.IsNullOrEmpty(avatarPath) ?
                    "default_avatar.png" :
                    Path.GetFileName(avatarPath);

                // Вставка нового пользователя
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO users (email, password_hash, avatar_url, created_at, is_active) " +
                    "VALUES (@email, @password, @avatar, @created, true) RETURNING user_id", conn))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@password", passwordHash);
                    cmd.Parameters.AddWithValue("@avatar", avatarUrl);
                    cmd.Parameters.AddWithValue("@created", DateTime.Now);

                    var userId = (int)cmd.ExecuteScalar();

                    // Назначение роли по умолчанию
                    using (var roleCmd = new NpgsqlCommand(
                        "INSERT INTO user_roles (user_id, role_id, granted_at) " +
                        "VALUES (@userId, (SELECT role_id FROM roles WHERE name = 'participant'), @granted)", conn))
                    {
                        roleCmd.Parameters.AddWithValue("@userId", userId);
                        roleCmd.Parameters.AddWithValue("@granted", DateTime.Now);
                        roleCmd.ExecuteNonQuery();
                    }

                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка регистрации: {ex.Message}");
            return false;
        }
    }

    // Аутентификация пользователя
    public User AuthenticateUser(string email, string password)
    {
        try
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();

                // Получаем хеш пароля из базы
                string storedHash;
                int userId;

                using (var cmd = new NpgsqlCommand(
                    "SELECT user_id, password_hash FROM users WHERE email = @email", conn))
                {
                    cmd.Parameters.AddWithValue("@email", email);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read()) return null;

                        userId = reader.GetInt32(0);
                        storedHash = reader.GetString(1);
                    }
                }

                // Проверяем пароль
                var inputHash = HashPassword(password);
                if (inputHash != storedHash) return null;

                // Обновляем время последнего входа
                using (var updateCmd = new NpgsqlCommand(
                    "UPDATE users SET last_login = @now WHERE user_id = @id", conn))
                {
                    updateCmd.Parameters.AddWithValue("@now", DateTime.Now);
                    updateCmd.Parameters.AddWithValue("@id", userId);
                    updateCmd.ExecuteNonQuery();
                }

                // Получаем данные пользователя
                var user = new User { Id = userId, Email = email };

                using (var userCmd = new NpgsqlCommand(
                    "SELECT first_name, last_name, phone, avatar_url, created_at FROM users WHERE user_id = @id", conn))
                {
                    userCmd.Parameters.AddWithValue("@id", userId);

                    using (var reader = userCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user.FirstName = reader.IsDBNull(0) ? null : reader.GetString(0);
                            user.LastName = reader.IsDBNull(1) ? null : reader.GetString(1);
                            user.Phone = reader.IsDBNull(2) ? null : reader.GetString(2);
                            user.AvatarUrl = reader.IsDBNull(3) ? null : reader.GetString(3);
                            user.CreatedAt = reader.GetDateTime(4);
                        }
                    }
                }

                // Получаем роли пользователя
                using (var rolesCmd = new NpgsqlCommand(
                    "SELECT r.name FROM user_roles ur " +
                    "JOIN roles r ON ur.role_id = r.role_id " +
                    "WHERE ur.user_id = @id", conn))
                {
                    rolesCmd.Parameters.AddWithValue("@id", userId);

                    using (var reader = rolesCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            user.Roles.Add(new Role { Name = reader.GetString(0) });
                        }
                    }
                }

                return user;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка аутентификации: {ex.Message}");
            return null;
        }
    }

    // Получение списка мероприятий
    public List<Event> GetEvents()
    {
        var events = new List<Event>();

        try
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand(
                    "SELECT e.event_id, e.title, e.description, e.start_date, e.end_date, " +
                    "e.location_id, l.name as location_name, e.status " +
                    "FROM events e " +
                    "JOIN locations l ON e.location_id = l.location_id " +
                    "WHERE e.status = 'active'", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            events.Add(new Event
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                StartDate = reader.GetDateTime(3),
                                EndDate = reader.GetDateTime(4),
                                LocationId = reader.GetInt32(5),
                                Location = new Location { Name = reader.GetString(6) },
                                Status = reader.GetString(7)
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка получения мероприятий: {ex.Message}");
        }

        return events;
    }

    // Другие методы для работы с промокодами, билетами, волонтерами и т.д.
    // ...
}