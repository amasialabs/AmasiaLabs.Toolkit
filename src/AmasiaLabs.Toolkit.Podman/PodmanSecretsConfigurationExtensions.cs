using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AmasiaLabs.Toolkit.Podman;

public static class PodmanSecretsConfigurationExtensions
{
    // ReSharper disable once MemberCanBePrivate.Global
    public static IConfigurationBuilder AddPodmanSecrets(
        this IConfigurationBuilder configuration,
        string directory = "/run/secrets",
        bool requireDirectory = false,
        bool throwOnError = true)
        => AddPodmanSecretsCore(configuration, directory, requireDirectory, throwOnError, prefix: null);

    /// <summary>
    /// Adds Podman/Docker secrets to configuration, placing every key under the given <paramref name="prefix"/>.
    /// Trailing colons in the prefix are trimmed. Pass null/empty/whitespace-only to behave as the no-prefix overload.
    /// </summary>
    /// <remarks>
    /// All parameters are required to keep this overload binary-distinct from the no-prefix overload above.
    /// </remarks>
    public static IConfigurationBuilder AddPodmanSecrets(
        this IConfigurationBuilder configuration,
        string directory,
        bool requireDirectory,
        bool throwOnError,
        string? prefix)
        => AddPodmanSecretsCore(configuration, directory, requireDirectory, throwOnError, prefix);

    public static IHostApplicationBuilder AddPodmanSecrets(
        this IHostApplicationBuilder builder,
        string directory = "/run/secrets",
        bool requireDirectory = false,
        bool throwOnError = false)
    {
        builder.Configuration.AddPodmanSecrets(directory, requireDirectory, throwOnError);
        return builder;
    }

    /// <summary>
    /// Host-builder overload that accepts a prefix. All parameters are required to keep this overload
    /// binary-distinct from the no-prefix overload above.
    /// </summary>
    public static IHostApplicationBuilder AddPodmanSecrets(
        this IHostApplicationBuilder builder,
        string directory,
        bool requireDirectory,
        bool throwOnError,
        string? prefix)
    {
        builder.Configuration.AddPodmanSecrets(directory, requireDirectory, throwOnError, prefix);
        return builder;
    }

    // ReSharper disable once CognitiveComplexity
    private static IConfigurationBuilder AddPodmanSecretsCore(
        IConfigurationBuilder configuration,
        string directory,
        bool requireDirectory,
        bool throwOnError,
        string? prefix)
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

    private static string? NormalizePrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return null;

        var trimmed = prefix.TrimEnd(':');
        return trimmed.Length == 0 ? null : trimmed;
    }
}
