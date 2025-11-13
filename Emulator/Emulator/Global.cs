using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using YamlDotNet.Serialization;

namespace Emulator
{
    /// <summary>
    /// Provides access to the application's global service provider and helper methods
    /// for retrieving registered services from anywhere in the application.
    /// </summary>
    internal static class Global
    {
        public static IServiceProvider? Services { get; set; }

        // Helper to get any service from anywhere
        public static T GetService<T>() where T : notnull
        {
            if (Services == null)
                throw new InvalidOperationException($"Service {typeof(T)} not registered.");
            return Services.GetRequiredService<T>();
        }
    }

    internal class YAMLConfig
    {
        public YAMLConfig()
        {
            LoadConfig();  // Runs once on DI resolution
        }

        // Debug mode configurations
        public ConsoleKey stepKey { get; private set; }
        public ConsoleKey stepOverKey { get; private set; }
        public bool doStepOverNOPsAfterRET { get; private set; }

        private void LoadConfig()
        {
            // Set defaults first
            stepKey = DefaultConfig.STEP_KEY;
            stepOverKey = DefaultConfig.STEP_OVER_KEY;
            doStepOverNOPsAfterRET = DefaultConfig.DO_STEP_OVER_NOPS_AFTER_RET;

            try
            {
                string filePath = Path.Combine(
                    ProjectPathResolver.FindSolutionRoot(),
                    Paths.YAML_CONFIG_FILE);

                if (File.Exists(filePath))
                {
                    string yamlContent = File.ReadAllText(filePath);
                    var deserializer = new DeserializerBuilder().Build();
                    var root = deserializer.Deserialize<RootConfig>(yamlContent);
                    var debug = root?.DebugModeConfiguration;

                    if (debug != null)
                    {
                        if (!string.IsNullOrWhiteSpace(debug.stepKey) &&
                            Enum.TryParse<ConsoleKey>(debug.stepKey, true, out var parsedStepKey))
                        {
                            stepKey = parsedStepKey;
                        }

                        if (!string.IsNullOrWhiteSpace(debug.stepOverKey) &&
                            Enum.TryParse<ConsoleKey>(debug.stepOverKey, true, out var parsedStepOverKey))
                        {
                            stepOverKey = parsedStepOverKey;
                        }

                        if (debug.doStepOverNOPsAfterRET.HasValue)
                        {
                            doStepOverNOPsAfterRET = debug.doStepOverNOPsAfterRET.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ConfigurationLoadException($"Config load failed: {ex.Message}.", ex);
            }
        }

        private class RootConfig
        {
            public DebugConfig? DebugModeConfiguration { get; set; }
        }

        private class DebugConfig
        {
            public string? stepKey { get; set; }
            public string? stepOverKey { get; set; }
            public bool? doStepOverNOPsAfterRET { get; set; }
        }

        public class ConfigurationLoadException : Exception
        {
            public ConfigurationLoadException(string message, Exception innerException)
                : base(message, innerException) { }
        }
    }

    internal static class ProjectPathResolver
    {
        /// <summary>
        /// Finds the solution root directory by looking for .git folder
        /// </summary>
        /// <returns>The full path to the solution root directory</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when solution root cannot be found</exception>
        public static string FindSolutionRoot()
        {
            if (_cache != null) return _cache;

            // Start from the current assembly's location
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var startDirectory = Path.GetDirectoryName(assemblyLocation);

            if (string.IsNullOrEmpty(startDirectory))
            {
                throw new DirectoryNotFoundException("Could not determine assembly directory");
            }

            var directory = new DirectoryInfo(startDirectory);

            // Traverse up until we find .git
            while (directory != null)
            {
                if (directory.GetDirectories(".git").Any())
                {
                    _cache = directory.FullName;
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                "Solution root not found looking for .git folder.");
        }

        private static string? _cache = null;
    }
}
