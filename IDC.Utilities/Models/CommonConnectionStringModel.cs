// ReSharper disable UnusedMember.Global

using System.Collections.Immutable;
using System.Web;

namespace IDC.Utilities.Models;

/// <summary>
///     Model koneksi universal yang dioptimalkan untuk .NET 10.
///     Digunakan oleh pustaka pembungkus untuk menyatukan SQLite, PostgreSql,
///     MongoDB, dan Couchbase dalam satu struktur data.
/// </summary>
/// <remarks>
///     Contoh penggunaan sederhana:
///     <code language="csharp">
///         var model = CommonConnectionStringModel.Parse(
///             "Host=localhost;Port=5432;Database=mydb;Username=admin;Password=secret",
///             CommonConnectionStringModel.DatabaseEngine.PostgreSql);
///         string connStr = model.ToConnectionString();
///         // connStr sekarang berisi string koneksi yang siap digunakan
///     </code>
/// </remarks>
/// <param name="Engine">
///     Mesin basis data yang ditargetkan.
/// </param>
public record CommonConnectionString(CommonConnectionString.DatabaseEngine Engine)
{
    #region Nested Types

    /// <summary>
    ///     Mesin basis data yang didukung.
    /// </summary>
    public enum DatabaseEngine
    {
        Sqlite,
        PostgreSql,
        MongoDb,
        Couchbase,
    }

    #endregion

    #region Properties

    /// <summary>
    ///     Nama host atau alamat server basis data.
    /// </summary>
    public string? Host { get; init; }

    /// <summary>
    ///     Port yang digunakan oleh basis data.
    /// </summary>
    public int? Port { get; init; }

    /// <summary>
    ///     Nama basis data, jalur file (SQLite), atau nama bucket (Couchbase).
    /// </summary>
    public string? Database { get; init; }

    /// <summary>
    ///     Nama pengguna untuk otentikasi.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    ///     Kata sandi untuk otentikasi.
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    ///     Untuk SQLite: jika true, gunakan basis data dalam memori (":memory:").
    /// </summary>
    public bool IsInMemory { get; init; }

    /// <summary>
    ///     Parameter khusus mesin (misalnya pooling, SSL, timeout, dll.).
    /// </summary>
    public IReadOnlyDictionary<string, string> AdditionalParameters { get; init; } =
        ImmutableDictionary<string, string>.Empty;

    #endregion

    #region Factory Methods

    /// <summary>
    ///     Menguraikan string koneksi menjadi objek <see cref="CommonConnectionString"/>.
    /// </summary>
    /// <param name="connectionString">
    ///     String koneksi yang akan diparsing.
    /// </param>
    /// <param name="engine">
    ///     Mesin basis data yang sesuai dengan string koneksi.
    /// </param>
    /// <returns>
    ///     Instance <see cref="CommonConnectionString"/> yang sudah terisi.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     Jika <paramref name="connectionString"/> null, kosong, atau hanya spasi putih.
    /// </exception>
    /// <exception cref="NotSupportedException">
    ///     Jika <paramref name="engine"/> bukan salah satu nilai yang didukung.
    /// </exception>
    /// <remarks>
    ///     Contoh penggunaan:
    ///     <code language="csharp">
    ///         var pgModel = CommonConnectionStringModel.Parse(
    ///             "Server=db.mycompany.com;Port=5432;Db=Sales;Uid=appuser;Pwd=apppwd",
    ///             CommonConnectionStringModel.DatabaseEngine.PostgreSql);
    ///     </code>
    /// </remarks>
    public static CommonConnectionString Parse(string connectionString, DatabaseEngine engine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return engine switch
        {
            DatabaseEngine.Sqlite => ParseSqlite(connectionString),
            DatabaseEngine.PostgreSql => ParsePostgre(connectionString),
            DatabaseEngine.MongoDb => ParseMongo(connectionString),
            DatabaseEngine.Couchbase => ParseCouchbase(connectionString),
            _ => throw new NotSupportedException($"Engine {engine} is not supported."),
        };
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Mengonversi model menjadi string koneksi yang dapat digunakan oleh penyedia basis data.
    /// </summary>
    /// <returns>
    ///     String koneksi yang sesuai dengan mesin basis data yang ditentukan.
    /// </returns>
    /// <remarks>
    ///     Contoh hasil untuk tiap mesin:
    ///     <list type="bullet">
    ///         <item><description>SQLite (file): <c>Data Source=mydb.sqlite;</c></description></item>
    ///         <item><description>SQLite (in‑memory): <c>Data Source=:memory:;Mode=Memory;Cache=Shared</c></description></item>
    ///         <item><description>PostgreSql: <c>Host=localhost;Port=5432;Database=mydb;Username=admin;Password=secret;</c></description></item>
    ///         <item><description>MongoDb: <c>mongodb://admin:secret@localhost:27017/mydb</c></description></item>
    ///         <item><description>Couchbase: <c>couchbase://localhost</c></description></item>
    ///     </list>
    /// </remarks>
    public string ToConnectionString()
    {
        return Engine switch
        {
            DatabaseEngine.Sqlite => IsInMemory
                ? "Data Source=:memory:;Mode=Memory;Cache=Shared"
                : $"Data Source={Database};{BuildParams("=")}",

            DatabaseEngine.PostgreSql =>
                $"Host={Host};Port={Port ?? 5432};Database={Database};Username={Username};Password={Password};{BuildParams("=")}",

            DatabaseEngine.MongoDb =>
                $"mongodb://{GetAuthInfo()}{Host}{GetPortStr(27017)}/{Database}{BuildParams("=", true)}",

            DatabaseEngine.Couchbase => $"couchbase://{Host}{GetPortStr()}{BuildParams("=", true)}",

            _ => throw new InvalidOperationException(
                $"Conversion for {Engine} is not implemented."
            ),
        };
    }

    #endregion

    #region Private Helpers

    private static CommonConnectionString ParseSqlite(string cs)
    {
        var dict = ParseToDictionary(cs: cs);
        var dataSource = GetValue(dict: dict, "Data Source", "Uri", "FullUri") ?? ":memory:";

        return new CommonConnectionString(Engine: DatabaseEngine.Sqlite)
        {
            Database = dataSource,
            IsInMemory = dataSource.Equals(
                value: ":memory:",
                comparisonType: StringComparison.OrdinalIgnoreCase
            ),
            AdditionalParameters = FilterParams(
                dict: dict,
                excludeKeys: ["Data Source", "Uri", "FullUri"]
            ),
        };
    }

    private static CommonConnectionString ParsePostgre(string cs)
    {
        var dict = ParseToDictionary(cs: cs);
        return new CommonConnectionString(Engine: DatabaseEngine.PostgreSql)
        {
            Host = GetValue(dict: dict, "Host", "Server", "Addr"),
            Port = int.TryParse(s: GetValue(dict: dict, keys: "Port"), out var p) ? p : 5432,
            Database = GetValue(dict: dict, "Database", "Db"),
            Username = GetValue(dict: dict, "Username", "User Id", "User"),
            Password = GetValue(dict: dict, "Password", "Pwd"),
            AdditionalParameters = FilterParams(
                dict: dict,
                [
                    "Host",
                    "Server",
                    "Addr",
                    "Port",
                    "Database",
                    "Db",
                    "Username",
                    "User Id",
                    "User",
                    "Password",
                    "Pwd",
                ]
            ),
        };
    }

    private static CommonConnectionString ParseMongo(string cs)
    {
        var uri = new Uri(uriString: cs);
        var userInfo = uri.UserInfo.Split(separator: ':');
        var queryParams = HttpUtility.ParseQueryString(query: uri.Query);

        return new CommonConnectionString(Engine: DatabaseEngine.MongoDb)
        {
            Host = uri.Host,
            Port = uri.Port != -1 ? uri.Port : 27017,
            Database = uri.AbsolutePath.Trim(trimChar: '/'),
            Username = userInfo.Length > 0 ? userInfo[0] : null,
            Password = userInfo.Length > 1 ? userInfo[1] : null,
            AdditionalParameters = queryParams.AllKeys.ToImmutableDictionary(
                keySelector: k => k!,
                elementSelector: k => queryParams[name: k]!
            ),
        };
    }

    private static CommonConnectionString ParseCouchbase(string cs)
    {
        var uri = new Uri(uriString: cs);
        return new CommonConnectionString(Engine: DatabaseEngine.Couchbase)
        {
            Host = uri.Host,
            Port = uri.Port != -1 ? uri.Port : null,
            Database = uri.AbsolutePath.Trim(trimChar: '/'),
            AdditionalParameters = HttpUtility
                .ParseQueryString(query: uri.Query)
                .AllKeys.ToImmutableDictionary(
                    keySelector: k => k!,
                    elementSelector: k => HttpUtility.ParseQueryString(query: uri.Query)[name: k]!
                ),
        };
    }

    private static Dictionary<string, string> ParseToDictionary(string cs) =>
        cs.Split(separator: ';', options: StringSplitOptions.RemoveEmptyEntries)
            .Select(selector: p => p.Split(separator: '=', count: 2))
            .Where(predicate: kv => kv.Length == 2)
            .ToDictionary(
                keySelector: kv => kv[0].Trim(),
                elementSelector: kv => kv[1].Trim(),
                comparer: StringComparer.OrdinalIgnoreCase
            );

    private string BuildParams(string kvSeparator, bool isQueryString = false)
    {
        if (AdditionalParameters.Count == 0)
            return string.Empty;
        var body = string.Join(
            separator: isQueryString ? "&" : ";",
            values: AdditionalParameters.Select(selector: x => $"{x.Key}{kvSeparator}{x.Value}")
        );
        return (isQueryString ? "?" : "") + body;
    }

    private string GetAuthInfo() =>
        !string.IsNullOrEmpty(value: Username) && !string.IsNullOrEmpty(value: Password)
            ? $"{Username}:{Password}@"
            : "";

    private string GetPortStr(int? defaultPort = null) =>
        Port.HasValue && Port != defaultPort ? $":{Port}" : "";

    private static string? GetValue(Dictionary<string, string> dict, params string[] keys)
    {
        foreach (var key in keys)
            if (dict.TryGetValue(key: key, value: out var val))
                return val;
        return null;
    }

    private static ImmutableDictionary<string, string> FilterParams(
        Dictionary<string, string> dict,
        string[] excludeKeys
    )
    {
        return dict.Where(predicate: kv =>
                !new HashSet<string>(
                    collection: excludeKeys,
                    comparer: StringComparer.OrdinalIgnoreCase
                ).Contains(item: kv.Key)
            )
            .ToImmutableDictionary(keySelector: kv => kv.Key, elementSelector: kv => kv.Value);
    }

    #endregion
}
