using System.Diagnostics;
using System.Text.Json.Serialization;

// [ RESHARPER SUPPRESSION ]
// Suppress ini dilakukan karna resharper salah deteksi, bahwa kode ini berada
// di dalam sebuah project library.
// ============================================================================
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ArrangeModifiersOrder

namespace IDC.Utilities.Models;

/// <summary>
///     Kontrak standar untuk respons API yang konsisten meliputi status
///     sukses, pesan, daftar kesalahan, ID pelacakan, dan timestamp respons.
/// </summary>
public interface IApiResponse
{
    /// <summary>
    ///     Mendapatkan nilai yang menunjukkan apakah operasi berhasil.
    ///     Nilai <c>true</c> menunjukkan sukses, <c>false</c> menunjukkan kegagalan.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    ///     Mendapatkan pesan deskriptif tentang hasil operasi.
    ///     Untuk respons sukses, biasanya berisi informasi konfirmasi.
    ///     Untuk respons gagal, berisi penjelasan kesalahan.
    /// </summary>
    string Message { get; }

    /// <summary>
    ///     Mendapatkan daftar kesalahan yang terjadi selama operasi.
    ///     Nilai null menunjukkan tidak ada kesalahan yang tercatat.
    ///     Setiap entri dalam daftar merepresentasikan detail kesalahan spesifik.
    /// </summary>
    List<ApiErrorDetails>? Errors { get; }

    /// <summary>
    ///     Mendapatkan identifier unik untuk pelacakan permintaan.
    ///     Nilai null menunjukkan ID pelacakan tidak disediakan atau tidak tersedia.
    ///     Digunakan untuk koneksi antara log aplikasi dan permintaan klien.
    /// </summary>
    string? TraceId { get; }

    /// <summary>
    ///     Mendapatkan tanggal dan waktu UTC ketika respons dibuat.
    ///     Selalu dalam format UTC untuk konsistensi zona waktu global.
    /// </summary>
    DateTime Timestamp { get; }
}

/// <summary>
///     Merepresentasikan detail kesalahan spesifik yang terjadi dalam respons API.
///     Digunakan untuk memberikan informasi kontekstual tentang field yang bermasalah
///     dan pesan deskriptif terkait kesalahan tersebut.
/// </summary>
/// <remarks>
///     Kelas ini digunakan untuk memberikan detail spesifik tentang error yang terjadi.
///     <example>
///         Contoh penggunaan untuk validasi field:
///         <code>
///             var errors = new List&lt;ApiError&gt;
///             {
///                 new ApiError("Email", "Format email tidak valid"),
///                 new ApiError("Password", "Password minimal 8 karakter")
///             };
///         </code>
///
///         Contoh untuk error global:
///         <code>
///             var error = new ApiError("", "Terjadi kesalahan internal pada server");
///         </code>
///     </example>
/// </remarks>
public class ApiErrorDetails(string field, string message)
{
    /// <summary>
    ///     Mendapatkan nama field yang terkait dengan kesalahan.
    ///     Nilai null menunjukkan kesalahan tidak terkait field tertentu
    ///     (misalnya: kesalahan validasi tingkat permintaan atau kesalahan sistem).
    ///     Gunakan string kosong untuk kesalahan global.
    /// </summary>
    public string Field { get; init; } = field;

    /// <summary>
    ///     Mendapatkan pesan deskriptif yang menjelaskan kesalahan yang terjadi.
    ///     Harus memberikan informasi yang cukup untuk pemahaman pengguna
    ///     atau pengembang tentang penyebab dan solusi potensial.
    /// </summary>
    public string Message { get; init; } = message;
}

/// <summary>
///     Membungkus respons standar untuk semua komunikasi API.
/// </summary>
/// <typeparam name="T">
///     Tipe data yang akan dikembalikan dalam respons.
/// </typeparam>
/// <remarks>
///     Kelas ini menyediakan struktur standar untuk respons API yang konsisten.
///     <example>
///         Contoh penggunaan untuk respons sukses:
///         <code>
///             var data = new User { Id = 1, Name = "John" };
///             return ApiResponse&lt;User&gt;.Success(data, "User ditemukan");
///         </code>
///
///         Contoh penggunaan untuk respons error:
///         <code>
///             try
///             {
///                 // kode yang mungkin throw exception
///             }
///             catch (Exception ex)
///             {
///                 return ApiResponse&lt;User&gt;.Failure(ex, includeDetails: true);
///             }
///         </code>
///     </example>
/// </remarks>
[method: JsonConstructor]
public class ApiResponse<T>(
    bool isSuccess,
    string message,
    T? data = default,
    List<ApiErrorDetails>? errors = null,
    string? traceId = null
) : IApiResponse
{
    /// <summary>
    ///     Menunjukkan apakah permintaan berhasil diproses.
    /// </summary>
    public bool IsSuccess { get; init; } = isSuccess;

    /// <summary>
    ///     Pesan yang menjelaskan hasil operasi.
    /// </summary>
    public string Message { get; init; } = message;

    /// <summary>
    ///     Data yang dikembalikan dari permintaan. Akan diabaikan saat serialisasi JSON jika null.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; init; } = data;

    /// <summary>
    ///     Daftar error yang terjadi selama pemrosesan. Akan diabaikan saat serialisasi JSON jika null.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ApiErrorDetails>? Errors { get; init; } = errors;

    /// <summary>
    ///     ID unik untuk melacak permintaan di seluruh sistem. Akan diabaikan saat serialisasi JSON jika null.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TraceId { get; init; } = traceId;

    /// <summary>
    ///     Waktu ketika respons dibuat (dalam UTC).
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    #region Static Helper Methods

    /// <summary>
    ///     Membuat instance ApiResponse yang menandakan operasi berhasil.
    /// </summary>
    /// <param name="data">Data yang akan dikembalikan.</param>
    /// <param name="message">Pesan sukses opsional (default: "Success").</param>
    /// <returns>Instance ApiResponse dengan status sukses.</returns>
    public static ApiResponse<T> Success(T data, string message = "Success") =>
        new(isSuccess: true, message: message, data: data);

    /// <summary>
    ///     Membuat instance ApiResponse yang menandakan operasi gagal.
    /// </summary>
    /// <param name="message">Pesan error yang menjelaskan kegagalan.</param>
    /// <param name="errors">Daftar error tambahan (opsional).</param>
    /// <returns>Instance ApiResponse dengan status gagal.</returns>
    public static ApiResponse<T> Failure(string message, List<ApiErrorDetails>? errors = null) =>
        new(
            isSuccess: false,
            message: message,
            data: default,
            errors: errors,
            traceId: GetTraceId()
        );

    /// <summary>
    ///     Membuat instance ApiResponse dari exception.
    /// </summary>
    /// <param name="ex">Exception yang terjadi.</param>
    /// <param name="includeDetails">
    ///     Menentukan apakah akan menyertakan detail stack trace dan inner exception.
    /// </param>
    /// <returns>Instance ApiResponse dengan informasi error dari exception.</returns>
    public static ApiResponse<T> Failure(Exception ex, bool includeDetails = false)
    {
        var errors = new List<ApiErrorDetails>();
        PopulateErrors(ex: ex, errors: errors, includeDetails: includeDetails);
        return Failure(message: ex.Message, errors: errors);
    }

    #endregion

    #region Shared Logic

    protected static string GetTraceId() =>
        Activity.Current?.Id ?? Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();

    protected static void PopulateErrors(
        Exception? ex,
        List<ApiErrorDetails> errors,
        bool includeDetails
    )
    {
        while (true)
        {
            if (ex == null)
                return;

            errors.Add(item: new ApiErrorDetails(field: ex.GetType().Name, message: ex.Message));

            if (!includeDetails)
                return;

            if (!string.IsNullOrEmpty(value: ex.StackTrace))
                errors.Add(item: new ApiErrorDetails(field: "StackTrace", message: ex.StackTrace));

            if (ex.InnerException != null)
            {
                ex = ex.InnerException;
                continue;
            }

            break;
        }
    }

    #endregion
}

/// <summary>
///     Respons khusus untuk operasi yang tidak memerlukan payload data.
///     Merupakan turunan dari <see cref="ApiResponse{object}"/> dengan data selalu null.
/// </summary>
/// <remarks>
///     Kelas ini menyederhanakan pembuatan respons API untuk operasi yang tidak mengembalikan data.
///     <example>
///         Contoh penggunaan untuk operasi sukses:
///         <code>
///             // Operasi yang berhasil tanpa data
///             return ApiResponse.Success("Data berhasil dihapus");
///         </code>
///
///         Contoh penggunaan untuk operasi gagal:
///         <code>
///             try
///             {
///                 // Operasi yang mungkin gagal
///             }
///             catch (Exception ex)
///             {
///                 return ApiResponse.Failure("Gagal memproses permintaan", new List&lt;ApiError&gt;
///                 {
///                     new ApiError("", ex.Message)
///                 });
///             }
///         </code>
///     </example>
/// </remarks>
[method: JsonConstructor]
public class ApiResponse(
    bool isSuccess,
    string message,
    List<ApiErrorDetails>? errors = null,
    string? traceId = null
)
    : ApiResponse<object>(
        isSuccess: isSuccess,
        message: message,
        data: null,
        errors: errors,
        traceId: traceId
    )
{
    #region Static Helper Methods

    /// <summary>
    ///     Membuat instance ApiResponse yang menandakan operasi berhasil tanpa data.
    /// </summary>
    /// <param name="message">Pesan sukses opsional (default: "Success").</param>
    /// <returns>Instance ApiResponse dengan status sukses.</returns>
    public static ApiResponse Success(string message = "Success") =>
        new(isSuccess: true, message: message);

    /// <summary>
    ///     Membuat instance ApiResponse yang menandakan operasi gagal.
    /// </summary>
    /// <param name="message">Pesan error yang menjelaskan kegagalan.</param>
    /// <param name="errors">Daftar error tambahan (opsional).</param>
    /// <returns>Instance ApiResponse dengan status gagal.</returns>
    public static new ApiResponse Failure(string message, List<ApiErrorDetails>? errors = null) =>
        new(isSuccess: false, message: message, errors: errors, traceId: GetTraceId());

    /// <summary>
    ///     Membuat instance ApiResponse dari exception.
    /// </summary>
    /// <param name="ex">Exception yang terjadi.</param>
    /// <param name="includeDetails">
    ///     Menentukan apakah akan menyertakan detail stack trace dan inner exception.
    /// </param>
    /// <returns>Instance ApiResponse dengan informasi error dari exception.</returns>
    public static new ApiResponse Failure(Exception ex, bool includeDetails = false)
    {
        var errors = new List<ApiErrorDetails>();
        PopulateErrors(ex: ex, errors: errors, includeDetails: includeDetails);
        return Failure(message: ex.Message, errors: errors);
    }

    #endregion
}
