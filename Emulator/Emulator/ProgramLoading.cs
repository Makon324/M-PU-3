using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Emulator
{
    internal record Token(string Type, string Value, int Line, int StartColumn);

    internal enum StatementType
    {
        LABEL,
        INSTRUCTION
    }

    /// <summary>
    /// Abstract base class representing a statement in an assembly program.
    /// </summary>
    internal abstract class ProgramStatement
    {
        public abstract StatementType Type { get; }

        public int Line { get; }
        public int Column { get; }

        protected ProgramStatement(int line, int column)
        {
            Line = line;
            Column = column;
        }
    }

    internal sealed class LabelStatement : ProgramStatement
    {
        public override StatementType Type => StatementType.LABEL;

        public string Label { get; }

        public LabelStatement(string label, int line, int column) : base(line, column)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
        }
    }

    internal sealed class InstructionStatement : ProgramStatement
    {
        public override StatementType Type => StatementType.INSTRUCTION;

        public string Mnemonic { get; }
        
        public IReadOnlyList<Token> Arguments { get; }

        public InstructionStatement(string mnemonic, IEnumerable<Token> arguments, int line, int column)
            : base(line, column)
        {
            Mnemonic = mnemonic ?? throw new ArgumentNullException(nameof(mnemonic));
            Arguments = new List<Token>(arguments);
        }
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
            string? assemblerPath = null;

            try
            {
                assemblerPath = GetAssemblerPath();
                
                InitializePython();
                AddToPythonPath(assemblerPath);

                using (Py.GIL())
                {
                    using (var tokenizer = Py.Import("tokenizer"))
                    using (var parser = Py.Import("parser"))
                    using (var validator = Py.Import("validator"))
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
        /// Resolves the path to the "Assembler" directory.
        /// </summary>
        /// <remarks>This method starts from the application's base directory and navigates up four
        /// directory levels to locate the "Assembler" directory. If the base directory is not resolvable, or if the
        /// "Assembler" directory does not exist at the expected location, an exception is thrown.</remarks>
        /// <returns>The full path to the "Assembler" directory.</returns>
        /// <exception cref="DirectoryNotFoundException"> Thrown when the base directory cannot be resolved, parent 
        /// directories are missing, or the Assembler directory is not found at the expected location.
        /// </exception>
        private string GetAssemblerPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (string.IsNullOrEmpty(baseDir))
            {
                throw new DirectoryNotFoundException("Base directory could not be resolved.");
            }

            // Walk up 4 levels
            DirectoryInfo? dirInfo = new DirectoryInfo(baseDir);
            for (int i = 0; i < 4; i++)
            {
                if (dirInfo?.Parent == null)
                {
                    throw new DirectoryNotFoundException(
                        $"Could not find a parent directory {i + 1} levels above {baseDir}"
                    );
                }
                dirInfo = dirInfo.Parent;
            }

            string assemblerPath = Path.Combine(dirInfo.FullName, "Assembler");
            if (!Directory.Exists(assemblerPath))
            {
                throw new DirectoryNotFoundException(
                    $"Directory not found at: {assemblerPath}"
                );
            }

            return assemblerPath;
        }

        /// <summary>
        /// Converts a dynamic Python list of dictionaries into a strongly-typed list of <see cref="ProgramStatement"/>.
        /// </summary>
        /// <remarks>This method processes each dictionary in the input list based on its "type" property.
        /// If the type is "label", it creates a <see cref="LabelStatement"/>. If the type is "instruction", it creates
        /// an <see cref="InstructionStatement"/> and converts its arguments into <see cref="Token"/> objects.</remarks>
        /// <param name="pythonList">A dynamic list of dictionaries, where each dictionary represents a program statement with properties such as
        /// type, label, mnemonic, and arguments.</param>
        /// <returns>A list of <see cref="ProgramStatement"/> objects representing the converted program statements.</returns>
        private List<ProgramStatement> ConvertPythonListOfDicts(dynamic pythonList)
        {
            var result = new List<ProgramStatement>();

            foreach (dynamic item in pythonList)
            {
                string type = item.type.ToString();

                if (type == "label")
                {
                    var labelStmt = new LabelStatement(
                        item.label.ToString(),
                        int.Parse(item.line.ToString()),
                        int.Parse(item.column.ToString())
                    );
                    result.Add(labelStmt);
                }
                else if (type == "instruction")
                {                    
                    // Convert arguments (list of tokens)
                    List<Token> arguments = new List<Token>();
                    foreach (dynamic arg in item.arguments)
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
                        item.mnemonic.ToString(),
                        arguments,
                        int.Parse(item.line.ToString()),
                        int.Parse(item.column.ToString())
                    );

                    result.Add(instructionStmt);
                }
            }

            return result;
        }

        private void AddToPythonPath(string path)
        {
            using (Py.GIL())
            {
                dynamic sys = Py.Import("sys");

                if (!sys.path.Contains(path))
                {
                    sys.path.insert(0, path);
                }
            }
        }

        private void InitializePython()
        {
            if (!PythonEngine.IsInitialized)
            {
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
    }

    internal static class ProgramLoader
    {
        /// <summary>
        /// Takes a path to an assembly program file, reads its content, transforms it using parts of Python Assembler, resolves labels, 
        /// and generates a list of instructions in the form of <see cref="InstructionStatement">.
        /// </summary>
        /// <param name="path">The file path to the assembly program.</param>
        /// <returns>
        /// A tuple containing:
        /// - labels: Dictionary mapping label names to their memory addresses
        /// - program: List of instruction statements
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when the path is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
        /// <exception cref="ProgramLoadException">Thrown when file reading, parsing, validation, or compilation fails.
        /// </exception>
        public static (IReadOnlyDictionary<string, ushort> labels, IReadOnlyList<InstructionStatement> program) LoadProgram(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            if (!File.Exists(path))
                throw new FileNotFoundException($"Assembly file not found: {Path.GetFileName(path)}", path);

            try
            {
                string assemblyCode = File.ReadAllText(path);
                List<ProgramStatement> statements;

                using (var transformer = new ProgramPythonTransformer())
                {
                    statements = transformer.TransformProgram(assemblyCode, path);
                }

                return CompileProgram(statements, path);
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

        /// <summary>
        /// Compiles a list of <see cref="ProgramStatement"/> into a symbol table of labels and a list of instructions.
        /// </summary>
        /// <param name="statements">The list of parsed program statements to compile.</param>
        /// <param name="filePath">The source file path (used for error reporting).</param>
        /// <returns>
        /// A tuple containing:
        /// - labels: Dictionary mapping label names to their resolved memory addresses
        /// - program: List of instruction statements with resolved label references
        /// </returns>
        /// <exception cref="ProgramLoadException">
        /// Thrown when duplicate labels are found or when undefined labels are referenced.</exception>
        private static (IReadOnlyDictionary<string, ushort> labels, IReadOnlyList<InstructionStatement> program) CompileProgram(List<ProgramStatement> statements, string filePath /*just for exceptions*/)
        {
            var labels = new Dictionary<string, ushort>();
            var program = new List<InstructionStatement>();
            ushort currentAddress = 0;

            // Build symbol table and program list
            foreach (var statement in statements)
            {
                if (statement is LabelStatement labelStatement)
                {
                    if (!labels.ContainsKey(labelStatement.Label))
                    {
                        labels.Add(labelStatement.Label, currentAddress);
                    }
                    else
                    {
                        throw new ProgramLoadException(filePath, $"Duplicate label '{labelStatement.Label}' at line {labelStatement.Line}");
                    }
                }
                else if (statement is InstructionStatement instructionStatement)
                {
                    program.Add(instructionStatement);
                    currentAddress++;
                }
            }

            if(program.Count > 1024)
            {
                throw new ProgramLoadException(filePath, $"Program exceeds 1024 instructions, found: {program.Count} instructions.");
            }

            // Validate that all referenced labels exist
            foreach (var instruction in program)
            {
                foreach (var token in instruction.Arguments)
                {
                    if (token.Type == "LABEL" && !labels.ContainsKey(token.Value))
                    {
                        throw new ProgramLoadException(filePath, 
                            $"Undefined label '{token.Value}' at line {instruction.Line}, column {instruction.Column}");
                    }
                }
            }

            return (labels, program);
        }






    }
}
