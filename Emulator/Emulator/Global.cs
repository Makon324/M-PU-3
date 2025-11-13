using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Emulator
{
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
