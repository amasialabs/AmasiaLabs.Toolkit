using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AmasiaLabs.Toolkit.Podman;

public static class PodmanSecretsConfigurationExtensions
{
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once CognitiveComplexity
    public static IConfigurationBuilder AddPodmanSecrets(
        this IConfigurationBuilder configuration,
        string directory = "/run/secrets",
        bool requireDirectory = false,
        bool throwOnError = true,
        string? prefix = null)
    {
        if (!Directory.Exists(directory))
        {
            if (requireDirectory) throw new DirectoryNotFoundException(directory);
            {
                return configuration;
            }
        }

        var normalizedPrefix = NormalizePrefix(prefix);

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
        {
            var file = Path.GetFileName(path);
            if (string.IsNullOrEmpty(file) || file[0] == '.')
            {
                continue;
            }

            var key = file.Replace("__", ":", StringComparison.Ordinal);
            if (normalizedPrefix is not null)
            {
                key = $"{normalizedPrefix}:{key}";
            }

            try
            {
                var val = File.ReadAllText(path).TrimEnd('\r', '\n');
                data[key] = val;
            }
            catch
            {
                if (throwOnError)
                {
                    throw;
                }
            }
        }

        if (data.Count > 0)
        {
            configuration.AddInMemoryCollection(data);
        }

        return configuration;
    }

    public static IHostApplicationBuilder AddPodmanSecrets(
        this IHostApplicationBuilder builder,
        string directory = "/run/secrets",
        bool requireDirectory = false,
        bool throwOnError = false,
        string? prefix = null)
    {
        builder.Configuration.AddPodmanSecrets(directory, requireDirectory, throwOnError, prefix);
        return builder;
    }

    private static string? NormalizePrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return null;

        var trimmed = prefix.TrimEnd(':');
        return trimmed.Length == 0 ? null : trimmed;
    }
}
