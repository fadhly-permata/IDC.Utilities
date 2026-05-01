// ReSharper disable UnusedMember.Global

using System.Text;

namespace IDC.Utilities.Extensions;

/// <summary>
///     Extension methods untuk pencatetan dan penangkapkan detail exception.
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    ///     Mengambil informasi detail dari sebuah exception dan menyusunnya menjadi string yang mudah dibaca.
    /// </summary>
    /// <param name="exception">
    ///     exception yang akan ditangkap. Jika null, metode mengembalikan string kosong.
    /// </param>
    /// <param name="includeStacktrace">
    ///     Jika true, stack trace dan semua exception inner akan disertakan dalam output.
    /// </param>
    /// <returns>
    ///     String yang berisi detail exception, termasuk waktu pembuatan, jenis, pesan,
    ///     sumber, dan (opsional) stack trace.
    /// </returns>
    /// <remarks>
    ///     Contoh penggunaan:
    ///     <code language="csharp">
    ///         try
    ///         {
    ///             // kode yang mungkin melempar exception
    ///         }
    ///         catch (Exception ex)
    ///         {
    ///             string log = ex.ToLogString(); // includeStacktrace default true
    ///             _logger.Error(log);
    ///         }
    ///     </code>
    /// </remarks>
    public static string ToLogString(this Exception? exception, bool includeStacktrace = true)
    {
        if (exception == null)
            return string.Empty;

        var sb = new StringBuilder();
        var timestamp = DateTime.Now.ToString(format: "yyyy-MM-dd HH:mm:ss");

        sb.AppendLine(handler: $"[Exception Log - {timestamp}]");
        CaptureDetails(ex: exception, sb: sb, includeStacktrace: includeStacktrace, indentLevel: 0);

        return sb.ToString();
    }

    private static void CaptureDetails(
        Exception ex,
        StringBuilder sb,
        bool includeStacktrace,
        int indentLevel
    )
    {
        // Membuat string spasi untuk indentasi sesuai level (4 spasi per level)
        var indent = new string(c: ' ', count: indentLevel * 4);
        // Prefix berbeda antara exception utama dan inner exception
        var prefix = indentLevel == 0 ? "Exception:" : "Inner Exception:";

        sb.AppendLine(handler: $"{indent}{prefix} [{ex.GetType().Name}]");
        sb.AppendLine(handler: $"{indent}Message: {ex.Message}");

        if (includeStacktrace)
        {
            // Tambahkan source jika tersedia dan tidak hanya whitespace
            if (!string.IsNullOrWhiteSpace(value: ex.Source))
                sb.AppendLine(handler: $"{indent}Source: {ex.Source}");

            // Tambahkan stack trace jika tersedia
            if (!string.IsNullOrWhiteSpace(value: ex.StackTrace))
            {
                sb.AppendLine(handler: $"{indent}StackTrace:");
                sb.AppendLine(handler: $"{indent}{ex.StackTrace}");
            }
        }

        // Rekursif untuk InnerException (jika ada)
        if (ex.InnerException != null)
        {
            sb.AppendLine(handler: $"{indent}{new string(c: '-', count: 20)}");
            CaptureDetails(
                ex: ex.InnerException,
                sb: sb,
                includeStacktrace: includeStacktrace,
                indentLevel: indentLevel + 1
            );
        }

        // Penanganan khusus untuk AggregateException (sering muncul di Task/Async)
        // Jika bukan AggregateException atau hanya memiliki satu inner exception, tidak perlu ditangkap lagi
        if (ex is not AggregateException aggEx || aggEx.InnerExceptions.Count <= 1)
            return;

        // Tampilkan jumlah total inner exception yang ada di AggregateException
        sb.AppendLine(
            handler: $"{indent}Aggregate Exceptions (Total: {aggEx.InnerExceptions.Count}):"
        );

        foreach (var inner in aggEx.InnerExceptions)
        {
            // Hindari duplikasi: inner exception yang sudah diproses sebagai InnerException di atas
            if (inner == ex.InnerException)
                continue;

            CaptureDetails(
                ex: inner,
                sb: sb,
                includeStacktrace: includeStacktrace,
                indentLevel: indentLevel + 1
            );
        }
    }
}
