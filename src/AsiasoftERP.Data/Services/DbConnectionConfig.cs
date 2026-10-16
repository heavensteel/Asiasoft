using System;
using System.Text.RegularExpressions;
using MySqlConnector;

namespace AsiasoftERP.Data.Services;

/// <summary>
/// Configuration and factory for high-performance MySQL/MariaDB database connections.
/// Operates simultaneously with weighbridge TCP sockets:
/// ERP records are fetched & saved to the central DB server (e.g. 192.168.1.*),
/// while the live indicator stream continuously pulls kilograms from the branch scale (e.g. 192.168.45.*) in real time.
/// </summary>
public static class DbConnectionConfig
{
    private static string _server = "127.0.0.1";
    private static int _port = 3306;
    private static string _database = "erp";
    private static string _userId = "root";
    private static string _password = "";
    private static string _connectionString = "Server=127.0.0.1;Port=3306;Database=erp;User ID=root;Password=;AllowUserVariables=true;ConvertZeroDateTime=true;Pooling=true;MinPoolSize=5;MaxPoolSize=100;ConnectionTimeout=15;CharSet=utf8mb4;";

    static DbConnectionConfig()
    {
        ParseCurrentConnectionString();
    }

    public static string Server
    {
        get => _server;
        set
        {
            _server = value;
            RebuildConnectionString();
        }
    }

    public static int Port
    {
        get => _port;
        set
        {
            _port = value;
            RebuildConnectionString();
        }
    }

    public static string Database
    {
        get => _database;
        set
        {
            _database = value;
            RebuildConnectionString();
        }
    }

    public static string ConnectionString
    {
        get => _connectionString;
        set
        {
            _connectionString = value;
            ParseCurrentConnectionString();
        }
    }

    public static MySqlConnection CreateConnection()
    {
        return new MySqlConnection(_connectionString);
    }

    private static void RebuildConnectionString()
    {
        _connectionString = $"Server={_server};Port={_port};Database={_database};User ID={_userId};Password={_password};AllowUserVariables=true;ConvertZeroDateTime=true;Pooling=true;MinPoolSize=5;MaxPoolSize=100;ConnectionTimeout=15;CharSet=utf8mb4;";
    }

    private static void ParseCurrentConnectionString()
    {
        try
        {
            var matchServer = Regex.Match(_connectionString, @"Server\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
            if (matchServer.Success) _server = matchServer.Groups[1].Value.Trim();

            var matchPort = Regex.Match(_connectionString, @"Port\s*=\s*(\d+)", RegexOptions.IgnoreCase);
            if (matchPort.Success && int.TryParse(matchPort.Groups[1].Value, out int p)) _port = p;

            var matchDb = Regex.Match(_connectionString, @"Database\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
            if (matchDb.Success) _database = matchDb.Groups[1].Value.Trim();
        }
        catch { }
    }
}
