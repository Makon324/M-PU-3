using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
