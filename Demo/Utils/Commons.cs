namespace Demo.Utils;

public class Commons
{
    public static bool IsDevelopment() =>
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
}
