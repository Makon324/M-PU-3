using Python.Runtime;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Emulator
{
    internal static class ProgramLoader
    {
        /// <summary>
        /// Takes a path to an assembly program file, reads its content, transforms it using parts of Python Assembler, resolves labels, 
        /// and generates a List of instructions in the form of <see cref="Program">.
        /// </summary>
        /// <param name="path">The file path to the assembly program.</param>
        /// <returns>A <see cref="Program"/> containing the compiled List of <see cref="Instruction"/></returns>
        /// <exception cref="ArgumentException">Thrown when the path is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
        /// <exception cref="ProgramLoadException">Thrown when file reading, parsing, validation, or compilation fails.
        /// </exception>
        public static Program LoadProgram(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            if (!File.Exists(path))
                throw new FileNotFoundException($"Assembly file not found: {Path.GetFileName(path)}", path);

            try
            {
                string assemblyCode = File.ReadAllText(path);
                List<ProgramStatement> statements;

                using (var pythonTransformer = new ProgramPythonTransformer())
                {
                    statements = pythonTransformer.TransformProgram(assemblyCode, path);
                }

                var (labels, instructionStatements) = ProgramCompiler.ResolveLabels(statements, path);
                var program = ProgramCompiler.CompileProgram(labels, instructionStatements);
                return new Program(program);
            }
            catch (ProgramLoadException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ProgramLoadException(path, $"Unexpected error loading program: {ex.Message}", ex);
            }
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

    internal readonly record struct Token(string Type, string Value, int Line, int StartColumn);
    internal enum StatementType
    {
        LABEL,
        INSTRUCTION
    }

    /// <summary>
    /// Abstract base class representing a statement in an assembly program.    
    /// </summary>
    /// <remarks>Used only during program loading and transformation.</remarks>
    internal abstract class ProgramStatement(int line, int column)
    {
        public abstract StatementType Type { get; }

        public int Line { get; } = line;
        public int Column { get; } = column;
    }

    internal sealed class LabelStatement(string label, int line, int column)
        : ProgramStatement(line, column)
    {
        public override StatementType Type => StatementType.LABEL;

        public string Label { get; } = label ?? throw new ArgumentNullException(nameof(label));
    }

    internal sealed class InstructionStatement(string mnemonic, IEnumerable<Token> arguments, int line, int column)
        : ProgramStatement(line, column)
    {
        public override StatementType Type => StatementType.INSTRUCTION;

        public string Mnemonic { get; } = mnemonic ?? throw new ArgumentNullException(nameof(mnemonic));
        public IReadOnlyList<Token> Arguments { get; } = new List<Token>(arguments);
    }

    /// <summary>
    /// Custom exception for errors encountered during program loading and transformation.
    /// </summary>
    internal class ProgramLoadException : Exception
    {
        public string FilePath { get; }

        public ProgramLoadException(string filePath, string message) : base(message)
        {
            FilePath = filePath;
        }

        public ProgramLoadException(string filePath, string message, Exception innerException) : base(message, innerException)
        {
            FilePath = filePath;
        }
    }

    /// <summary>
    /// Transforms assembly code into structured program statements using Python-based tokenizer, parser, and validator.
    /// Manages Python runtime initialization and cleanup.
    /// </summary>
    internal sealed class ProgramPythonTransformer : IDisposable
    {        
        private bool _isInitializedHere = false;

        /// <summary>
        /// Transforms the provided assembly code into a list of strongly-typed program statements using parts of Python Assembler.
        /// </summary>
        /// <param name="assemblyCode">The raw assembly code to be transformed.</param>
        /// <param name="filePath">The file path associated with the assembly code, used for error reporting.</param>
        /// <returns>A list of <see cref="ProgramStatement"/> objects representing the parsed and validated program.</returns>
        /// <exception cref="ProgramLoadException">Thrown when transformation fails due to parsing, validation, or other errors.</exception>
        public List<ProgramStatement> TransformProgram(string assemblyCode, string filePath /*just for exceptions*/)
        {
            try
            {
                string rootPath = ProjectPathResolver.FindSolutionRoot();
                string assemblerPath = Path.Combine(rootPath, Paths.PYTHON_ASSEMBLER);
                
                string tokenizerModule = "tokenizer";
                string parserModule = "parser";
                string validatorModule = "validator";

                InitializePython();
                AddToPythonPath(assemblerPath);

                using (Py.GIL())
                {
                    using (var tokenizer = Py.Import(tokenizerModule))
                    using (var parser = Py.Import(parserModule))
                    using (var validator = Py.Import(validatorModule))
                    using (var tokenizerInstance = tokenizer.InvokeMethod("AssemblerTokenizer"))
                    using (var tokens = tokenizerInstance.InvokeMethod("tokenize", assemblyCode.ToPython()))
                    using (var parserInstance = parser.InvokeMethod("AssemblerParser", tokens))
                    using (var parsedProgram = parserInstance.InvokeMethod("parse"))
                    using (var validatorInstance = validator.InvokeMethod("AssemblerValidator"))
                    {
                        validatorInstance.InvokeMethod("validate", parsedProgram);
                        return ConvertPythonListOfDicts(parsedProgram);
                    }
                }
            }
            catch (PythonException ex)
            {
                throw new ProgramLoadException(filePath, $"Python processing error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new ProgramLoadException(filePath, $"Error transforming program: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Converts a dynamic Python list of dictionaries into a strongly-typed list of <see cref="ProgramStatement"/>.
        /// </summary>
        /// <remarks>This method processes each dictionary in the input list based on its "type" property.
        /// If the type is "label", creates a <see cref="LabelStatement"/>. 
        /// If "instruction", creates an <see cref="InstructionStatement"/> with converted <see cref="Token"/> arguments.</remarks>
        /// <remarks>This method runs after code is already validated by Python validator, so it omits some checks.</remarks>
        /// <param name="pythonList">A dynamic list of dictionaries, where each dictionary represents a program statement with properties such as
        /// type, label, mnemonic, and arguments.</param>
        /// <returns>A list of <see cref="ProgramStatement"/> objects representing the converted program statements.</returns>
        private static List<ProgramStatement> ConvertPythonListOfDicts(dynamic pythonList)
        {
            var result = new List<ProgramStatement>();

            foreach (dynamic item in pythonList)
            {
                string type = item["type"].ToString();

                if (type == "label")
                {
                    var labelStmt = new LabelStatement(
                        item["label"].ToString(),
                        int.Parse(item["line"].ToString()),
                        int.Parse(item["column"].ToString())
                    );
                    result.Add(labelStmt);
                }
                else if (type == "instruction")
                {
                    // Convert arguments (list of tokens)
                    List<Token> arguments = new();
                    foreach (dynamic arg in item["arguments"])
                    {
                        var token = new Token(
                            arg.type.ToString(),
                            arg.value.ToString(),
                            int.Parse(arg.line.ToString()),
                            int.Parse(arg.start_column.ToString())
                        );
                        arguments.Add(token);
                    }

                    var instructionStmt = new InstructionStatement(
                        item["mnemonic"].ToString(),
                        arguments,
                        int.Parse(item["line"].ToString()),
                        int.Parse(item["column"].ToString())
                    );

                    result.Add(instructionStmt);
                }
            }

            return result;
        }

        private static void AddToPythonPath(string path)
        {
            using (Py.GIL())
            {
                dynamic sys = Py.Import("sys");

                if (!(bool)sys.path.__contains__(path))
                {
                    sys.path.insert(0, path);
                }
            }
        }

        private void InitializePython()
        {
            if (!PythonEngine.IsInitialized)
            {
                string pythonDllPath = FindPythonDllPath();
                Runtime.PythonDLL = pythonDllPath;
                PythonEngine.Initialize();
                _isInitializedHere = true;
            }
        }

        public void Dispose()
        {
            if (_isInitializedHere && PythonEngine.IsInitialized)
            {
                PythonEngine.Shutdown();
            }
        }

        /// <summary>
        /// Finds the path to the Python shared library.
        /// </summary>
        private static string FindPythonDllPath()
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            bool isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            if (!isWindows && !isLinux && !isOSX)
            {
                throw new PlatformNotSupportedException("Unsupported operating system.");
            }

            // Find the Python executable in PATH
            string[] possibleExeNames = isWindows
                ? new string[] { "python", "python3" }
                : new string[] { "python3", "python" };
            string? pythonExe = null;
            foreach (string possibleName in possibleExeNames)
            {
                if (isWindows)
                {
                    pythonExe = FindInPath(possibleName + ".exe");
                }
                else 
                { 
                    pythonExe = FindInPath(possibleName);
                }

                if (pythonExe != null)
                    break;
            }
            if (pythonExe == null)
                throw new InvalidOperationException("Python executable not found in PATH.");

            // Verify Python version
            string version = GetPythonVersion(pythonExe);
            VerifyPythonVersion(version);

            // Get dll
            string pyCode = @"
import sys
import os
from ctypes import util as ctypes_util
if sys.platform.startswith('win'):
    major = sys.version_info.major
    minor = sys.version_info.minor
    dllname = f'python{major}{minor}.dll'
    print(os.path.join(os.path.dirname(sys.executable), dllname))
else:
    major = sys.version_info.major
    minor = sys.version_info.minor
    libname = f'python{major}.{minor}'
    libpath = ctypes_util.find_library(libname)
    if libpath is None:
        libname += 'm'
        libpath = ctypes_util.find_library(libname)
    if libpath is None:
        raise ValueError('Could not find libpython')
    print(libpath)";
            string dllPath = GetPythonOutput(pythonExe, pyCode);
            if (!File.Exists(dllPath))
                throw new FileNotFoundException($"Python shared library not found at '{dllPath}'.");

            return dllPath;
        }

        /// <summary>
        /// Helper to get output from a Python script.
        /// </summary>
        private static string GetPythonOutput(string pythonExe, string code)
        {
            var psi = new ProcessStartInfo(pythonExe, $"-c \"{code}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi) ?? throw new Exception("Failed to start Python process.");
            string output = process.StandardOutput.ReadToEnd().Trim();
            string error = process.StandardError.ReadToEnd().Trim();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Python script failed: {error}");
            }
            return output;
        }

        /// <summary>
        /// Helper to find file in PATH
        /// </summary>
        private static string? FindInPath(string fileName)
        {
            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathEnv)) return null;

            string[] paths = pathEnv.Split(Path.PathSeparator);
            foreach (string path in paths)
            {
                string fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            return null;
        }

        /// <summary>
        /// Helper to get Python version
        /// </summary>
        /// <remarks>Starts python with -V argument.</remarks>
        private static string GetPythonVersion(string pythonExe)
        {
            var psi = new ProcessStartInfo(pythonExe, "-V")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi) ?? throw new Exception("Failed to start Python process.");
            process.WaitForExit();
            string output = process.StandardOutput.ReadToEnd().Trim();
            
            return output.Split(' ')[1];
        }

        /// <summary>
        /// Verifies that the provided Python version string represents a supported version.
        /// </summary>
        private static void VerifyPythonVersion(string version)
        {
            string[] expectedParts = Paths.MIN_PYTHON_VERSION.Split('.');

            string[] parts = version.Split('.');
            if (parts.Length <= 2)
                throw new ArgumentException($"Cannot parse Python version: \"{version}\". Expected format \"major.minor...\".", nameof(version));

            int major = int.Parse(parts[0]);
            int minor = int.Parse(parts[1]);

            if (major != int.Parse(expectedParts[0]))
                throw new InvalidOperationException(
                    $"Unsupported Python major version: {major}. Only Python 3.x is supported.");

            if (minor < int.Parse(expectedParts[1]))
                throw new InvalidOperationException(
                    $"Unsupported Python version: {version}. Requires Python {Paths.MIN_PYTHON_VERSION} or newer.");
        }
    }

    internal sealed class ProgramCompiler
    {
        /// <summary>
        /// Compiles a list of <see cref="ProgramStatement"/> into a symbol table of labels and a list of instructions.
        /// </summary>
        /// <remarks>This method runs after code is already validated by python validator, so it omits some checks.</remarks>
        /// <param name="statements">The list of parsed program statements to compile.</param>
        /// <param name="filePath">The source file path (used for error reporting).</param>
        /// <returns>
        /// A tuple containing:
        /// - <c>labels</c>: A dictionary mapping label names to resolved memory addresses.
        /// - <c>program</c>: A list of <see cref="InstructionStatement"/>s with resolved label references.
        /// </returns>
        /// <exception cref="ProgramLoadException">
        /// Thrown when duplicate labels are found or when undefined labels are referenced.</exception>
        public static (
            IReadOnlyDictionary<string, ushort> labels,
            IReadOnlyList<InstructionStatement> program
        ) ResolveLabels(
            List<ProgramStatement> statements,
            string filePath // just for exceptions
        )
        {
            var labels = new Dictionary<string, ushort>();
            var instructionStatements = new List<InstructionStatement>();
            ushort currentAddress = 0;

            // Build symbol table and program list
            foreach (var statement in statements)
            {
                if (statement is LabelStatement labelStatement)
                {
                    if(!labels.TryAdd(labelStatement.Label, currentAddress))
                        throw new ProgramLoadException(filePath, $"Duplicate label '{labelStatement.Label}' at line {labelStatement.Line}");
                }
                else if (statement is InstructionStatement instructionStatement)
                {
                    instructionStatements.Add(instructionStatement);
                    currentAddress++;
                }
            }

            if (instructionStatements.Count > Architecture.MAX_PROGRAM_SIZE)
            {
                throw new ProgramLoadException(filePath, $"Program exceeds maximum size of {Architecture.MAX_PROGRAM_SIZE} instructions ({instructionStatements.Count} found).");
            }

            return (labels, instructionStatements);
        }


        /// <summary>
        /// Compiles a list of <see cref="InstructionStatement"/> and labels into a List of <see cref="Instruction"/>.
        /// </summary>
        /// <remarks>This method runs after code is already validated by Python validator, so it omits some checks.</remarks>
        public static IReadOnlyList<Instruction> CompileProgram(
            IReadOnlyDictionary<string, ushort> labels, 
            IReadOnlyList<InstructionStatement> instructionStatements)
        {
            var compiledProgram = new List<Instruction>();

            var argumentSpecifications = LoadArgumentsSpecification();

            foreach (var instructionStatement in instructionStatements)
            {
                string mnemonic = instructionStatement.Mnemonic;
                var argumentsSpec = argumentSpecifications[mnemonic];
                var arguments_ = instructionStatement.Arguments;
                var arguments = new List<Argument>();

                foreach (var (token, spec) in arguments_.Zip(argumentsSpec))
                {
                    arguments.Add(TokenToArgument(token, spec, labels));
                }

                compiledProgram.Add(new Instruction(instructionStatement.Mnemonic, arguments));
            }

            return compiledProgram;
        }

        /// <summary>
        /// Converts a token to a strongly-typed argument based on the specification.        
        /// </summary>
        private static Argument TokenToArgument(Token token, string specification, IReadOnlyDictionary<string, ushort> labels)
        {
            switch (specification)
            {
                case "reg":
                    return new RegisterArgument(byte.Parse(token.Value.AsSpan(1)));
                case "num":
                    return new NumberArgument((byte)NumParse(token.Value));
                case "adr":
                    if(token.Type == "IDENT")
                    {
                        return new AddressArgument(labels[token.Value]);
                    }
                    else
                    {
                        return new AddressArgument((ushort)NumParse(token.Value));
                    }
                default:  // Should not happen, as arguments are already validated by python validator
                    throw new InvalidOperationException($"Unknown argument specification: {specification}");
            }
        }

        /// <summary>
        /// Parses a string representing a small integer (0–65535) in binary, hexadecimal, or decimal format.
        /// </summary>
        private static int NumParse(string input)
        {
            if (input.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
            {
                // Binary
                return Convert.ToInt32(input[2..], 2);
            }
            else if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                // Hex
                return int.Parse(input.AsSpan(2), NumberStyles.HexNumber);
            }
            else
            {
                // Decimal (default)
                return int.Parse(input, NumberStyles.Integer);
            }
        }

        /// <summary>
        /// Loads instruction argument specifications from a JSON file into a dictionary.
        /// </summary>
        public static IReadOnlyDictionary<string, List<string>> LoadArgumentsSpecification()
        {
            string jsonPath = Path.Combine(ProjectPathResolver.FindSolutionRoot(), Paths.INSTRUCTIONS_FILE);

            if(!File.Exists(jsonPath))
                throw new FileNotFoundException($"Instructions specification file not found: {jsonPath}", jsonPath);

            var jsonText = File.ReadAllText(jsonPath, Encoding.UTF8);
            
            var instructions = JsonSerializer.Deserialize<List<Instr>>(jsonText, _serializerOptions)
                ?? throw new InvalidOperationException($"Failed to deserialize instructions from '{jsonPath}'.");

            var dict = instructions
                .ToDictionary(
                    instr => instr.Mnemonic,
                    instr => instr.Operands?.Select(op => op.Type).ToList() ?? new List<string>()
                );
            return dict;
        }

        // Cached options for JSON deserialization
        private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Records just for deserializing instructions.json
        internal readonly record struct Operd(string Type);
        internal readonly record struct Instr(string Mnemonic, List<Operd> Operands);
    }


}
