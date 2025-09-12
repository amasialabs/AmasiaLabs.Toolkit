using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AmasiaLabs.Toolkit.Podman.Tests.Unit;

public class PodmanSecretsConfigurationExtensionsTests
{
    [Fact]
    public void AddPodmanSecrets_Maps_Files_With_Trimming_And_Hierarchy()
    {
        // Arrange
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "db__password"), "secret\n");
        File.WriteAllText(Path.Combine(dir.Path, "apiKey"), "XYZ\r\n");

        // Act
        var cfg = new ConfigurationBuilder()
            .AddPodmanSecrets(dir.Path, requireDirectory: true, throwOnError: true)
            .Build();

        // Assert
        cfg["db:password"].Should().Be("secret");
        cfg["apiKey"].Should().Be("XYZ");
    }

    [Fact]
    public void AddPodmanSecrets_Ignores_Hidden_Files()
    {
        // Arrange
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, ".ignored"), "should-not-be-read");

        // Act
        var cfg = new ConfigurationBuilder()
            .AddPodmanSecrets(dir.Path)
            .Build();

        // Assert
        cfg.AsEnumerable().Should().NotContain(kv => kv.Key == ".ignored");
    }

    [Fact]
    public void AddPodmanSecrets_NoThrow_When_Dir_Not_Required()
    {
        // Arrange
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        // Act
        var cfg = new ConfigurationBuilder()
            .AddPodmanSecrets(missing, requireDirectory: false)
            .Build();

        // Assert
        cfg.AsEnumerable().Should().NotBeNull();
    }

    [Fact]
    public void AddPodmanSecrets_Throws_When_Dir_Required()
    {
        // Arrange
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        // Act
        var act = () => new ConfigurationBuilder().AddPodmanSecrets(missing, requireDirectory: true).Build();

        // Assert
        act.Should().Throw<DirectoryNotFoundException>();
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; }

        public TempDir()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); } catch { /* ignore */ }
        }
    }
}
