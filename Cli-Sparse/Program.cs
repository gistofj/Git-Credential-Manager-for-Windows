using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Alm.Git;

/**
     git sparse
        Displays the current state of sparse

     git sparse init <init-options>
        Initializes a repo to use git-sparse and/or limited-refspec.

     git sparse clone <init-options> <clone-options>
        Clones a repository with git-sparse settings:
            git init, git remote add, git sparse init <init-options>, git clone <clone-options>.

    git sparse add <path>
        Addes an included path to the git-sparse map.

    git sparse rm <path>
        Removes a path from or adds an excluded path to the git-sparse map based on existing values.
**/

namespace Microsoft.TeamFoundation.SparseCheckout
{
    internal class Program
    {
        public const string FeatureName = "sparse-checkout";
        public const string ConfigFileName = "sparse-checkout";
        public const string ConfigPrefix = "core";
        public const string ConfigKey = "sparsecheckout";
        public const string ConfigValueName = ConfigPrefix + "." + ConfigKey;

        public Program(TextReader stdin, TextWriter stdout, TextWriter stderr)
        {
            Err = stderr ?? Console.Error;
            Out = stdout ?? Console.Out;
            Input = stdin ?? Console.In;
        }
        public Program()
            : this(null, null, null)
        { }

        internal readonly TextWriter Err;
        internal readonly TextReader Input;
        internal readonly TextWriter Out;

        internal static string ExecutablePath
        {
            get
            {
                if (_exeutablePath == null)
                {
                    LoadAssemblyInformation();
                }
                return _exeutablePath;
            }
        }
        private static string _exeutablePath;
        internal static string GitCmd
        {
            get
            {
                if (_gitCmd == null)
                {
                    List<GitInstallation> installs;
                    if (Where.FindGitInstallations(out installs))
                    {
                        _gitCmd = installs[0].Cmd;
                    }
                    else
                    {
                        // non-null empty means no Git installations detected
                        _gitCmd = String.Empty;
                    }
                }

                return _gitCmd;
            }
        }
        private static string _gitCmd;
        internal static string Location
        {
            get
            {
                if (_location == null)
                {
                    LoadAssemblyInformation();
                }
                return _location;
            }
        }
        private static string _location;
        internal static string Name
        {
            get
            {
                if (_name == null)
                {
                    LoadAssemblyInformation();
                }
                return _name;
            }
        }
        private static string _name;
        internal static Version Version
        {
            get
            {
                if (_version == null)
                {
                    LoadAssemblyInformation();
                }
                return _version;
            }
        }
        private static Version _version;

        static int Main(string[] args)
        {
            try
            {
                EnableDebugTrace();

                Program program = new Program();
                GitConfiguration config = new GitConfiguration();

                if (args.Length == 0)
                {
                    program.Default(args, config);
                }
                else
                {
                    Dictionary<string, SparseAction> actions = new Dictionary<string, SparseAction>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "add", program.Add },
                        { "clone", program.Clone },
                        { "init", program.Init },
                        { "remove", program.Remove },
                    };

                    if (actions.ContainsKey(args[0]))
                    {
                        var subargs = new string[args.Length - 1];
                        Array.Copy(args, 1, subargs, 0, subargs.Length);

                        return actions[args[0]](subargs, config)
                            ? 0
                            : -1;
                    }
                    else if (String.Equals(args[0], "--help", StringComparison.OrdinalIgnoreCase)
                             || String.Equals(args[0], "-h", StringComparison.OrdinalIgnoreCase)
                             || args[0].Contains('?'))
                    {
                        program.Help();
                    }
                    else
                    {
                        program.Default(args, config);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Fatal: " + exception.GetType().Name + " encountered.");
                Trace.WriteLine("Fatal: " + exception.ToString());
                LogEvent(exception.Message, EventLogEntryType.Error);
            }

            return 0;
        }

        internal bool Add(string[] args, GitConfiguration config)
        {
            Debug.Assert(args != null, "The `args` parameter is null.");
            Debug.Assert(config != null, "The `config` parameter is null.");

            Trace.WriteLine("Program::Add");

            return true;
        }

        internal bool Clone(string[] args, GitConfiguration config)
        {
            Debug.Assert(args != null, "The `args` parameter is null.");
            Debug.Assert(config != null, "The `config` parameter is null.");

            Trace.WriteLine("Program::Clone");

            return true;
        }

        internal void Default(string[] args, GitConfiguration config)
        {
            Debug.Assert(args != null, "The `args` parameter is null.");
            Debug.Assert(config != null, "The `config` parameter is null.");

            Trace.WriteLine("Program::Default");

            if (IsEnabled(config))
            {
                Out.WriteLine("sparse enabled.");

                List<string> values;
                if (ReadSparseConfig(config, out values))
                {
                    foreach (string value in values)
                    {
                        if (String.IsNullOrEmpty(value))
                            continue;

                        if (value[0] != '!')
                        {
                            Out.Write(" ");
                        }
                        Out.WriteLine(value);
                    }
                }
            }
            else
            {
                Out.WriteLine("sparse is not enabled.");
            }
        }

        internal void Help()
        {
            Trace.WriteLine("Program::PrintHelpMessage");
        }

        internal bool Init(string[] args, GitConfiguration config)
        {
            const string ArgsNotName = "--not";
            const string ArgsMapName = "--map";
            const string ArgsSpecName = "--spec";

            Debug.Assert(args != null, "The `args` parameter is null.");
            Debug.Assert(config != null, "The `config` parameter is null.");

            Trace.WriteLine("Program::Init");

            string specPath = null;
            HashSet<string> maps = null;
            HashSet<string> nots = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (String.Equals(args[i], ArgsMapName, StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length < i + 1)
                    {
                        if (maps == null)
                        {
                            maps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        }

                        string path = args[i];
                        maps.Add(path);
                    }
                    else
                    {
                        Err.WriteLine("Fatal: no path specified for " + ArgsMapName + ".");
                        return false;
                    }
                }
                else if (String.Equals(args[i], ArgsNotName, StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length < i + 1)
                    {
                        if (nots == null)
                        {
                            nots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        }

                        string path = args[i];
                        nots.Add(path);
                    }
                    else
                    {
                        Err.WriteLine("Fatal: no path specified for " + ArgsNotName + ".");
                        return false;
                    }
                }
                else if (String.Equals(args[i], ArgsSpecName, StringComparison.OrdinalIgnoreCase))
                {
                    if (specPath == null)
                    {
                        if (args.Length < i + 1)
                        {
                            i += 1;
                            specPath = args[i];
                        }
                        else
                        {
                            Err.WriteLine("Fatal: no path specified for " + ArgsSpecName + ".");
                            return false;
                        }
                    }
                    else
                    {
                        Err.WriteLine("Fatal: " + ArgsSpecName + " cannot be specified more than once.");
                        return false;
                    }
                }
                else
                {
                    Err.WriteLine("Fatal: Unknown argument " + args[i]);
                    return false;
                }
            }

            if (String.IsNullOrEmpty(specPath))
            {
                maps = maps ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                nots = nots ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (maps.Count == 0)
                {
                    Out.WriteLine("warning: no paths were added to the sparse-checkout configuration.");
                    Out.WriteLine("         defaulting to mapping everything.");

                    maps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    maps.Add("/*");
                }

                {
                    List<string> values;
                    if (!ReadSparseConfig(config, out values))
                    {
                        values = new List<string>();
                    }

                    foreach (string value in values)
                    {
                        if (String.IsNullOrEmpty(value))
                            continue;

                        if (value[0] == '!')
                        {
                            nots.Add(value);
                        }
                        else
                        {
                            maps.Add(value);
                        }
                    }
                }
            }
            else
            {
                FileInfo specFile = new FileInfo(specPath);

                if (!specFile.Exists)
                {
                    Err.WriteLine("Fatal: {0} '{1}' cannot be found.", ArgsSpecName, specFile.FullName);
                    return false;
                }

                List<string> values;
                if (!ReadSparseConfig(config, out values))
                {
                    values = new List<string>();

                    foreach (string item in values)
                    {
                        if (String.IsNullOrWhiteSpace(item))
                            continue;

                        if (item[0] == '!')
                        {
                            nots.Add(item);
                        }
                        else
                        {
                            nots.Add(item);
                        }
                    }
                }

                List<string> refspecs;
                if (ReadSpecFile(specFile, out values, out refspecs))
                {
                    foreach (string item in values)
                    {
                        if (String.IsNullOrWhiteSpace(item))
                            continue;

                        if (item[0] == '!')
                        {
                            nots.Add(item);
                        }
                        else
                        {
                            nots.Add(item);
                        }
                    }
                }
            }

            if (!WriteSparseConfig(config, maps, nots))
            {
                Err.WriteLine("Fatal: Error writing to the sparse-checkout file.");
            }

            if (GitProcess.Run("config --local " + ConfigValueName + " true", Environment.CurrentDirectory, 0))
            {
                Out.WriteLine("git {0} enabled.", FeatureName);
            }

            return true;
        }

        internal bool Remove(string[] args, GitConfiguration config)
        {
            Debug.Assert(args != null, "The `args` parameter is null.");
            Debug.Assert(config != null, "The `config` parameter is null.");

            Trace.WriteLine("Program::Remove");

            return true;
        }

        internal static bool ReadSparseConfig(FileInfo sparseFile, out List<string> values)
        {
            Debug.Assert(sparseFile != null, "The `sparseFile` parameter is null.");
            Debug.Assert(sparseFile.Exists, "The `sparseFile` parameter does not exist.");

            Trace.WriteLine("Program::ReadSparseConfig");

            if (sparseFile.Exists)
            {
                values = new List<string>();

                Trace.WriteLine("   " + ConfigFileName + " found.");

                using (var stream = sparseFile.OpenRead())
                using (var reader = new StreamReader(stream))
                {
                    string line = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        values.Add(line);
                    }
                }

                Trace.WriteLine("   " + values.Count + " entries found.");

                return true;
            }

            Trace.WriteLine("   " + ConfigFileName + " not found.");

            values = null;
            return false;
        }

        internal static bool ReadSparseConfig(GitConfiguration config, out List<string> values)
        {
            const string DotGitInfoFolderName = "info";

            Debug.Assert(config != null, "The `config` parameter is null.");

            Trace.WriteLine("Program::ReadSparseConfig");

            if (config.LocalPath != null)
            {
                Trace.WriteLine("   local config at '" + config.LocalPath + "'.");

                if (config.LocalPath != null && File.Exists(config.LocalPath))
                {
                    string dotgitPath = Path.GetDirectoryName(config.LocalPath);
                    string configPath = Path.Combine(dotgitPath, DotGitInfoFolderName, ConfigFileName);

                    FileInfo sparseFile = new FileInfo(configPath);

                    if (sparseFile.Exists)
                        return ReadSparseConfig(sparseFile, out values);
                }

                Trace.WriteLine("   " + ConfigFileName + " not found.");
            }
            else
            {
                Trace.WriteLine("   no local config detected, not a git repo.");
            }

            values = null;
            return false;
        }

        internal static bool ReadSpecFile(FileInfo specFile, out List<string> values, out List<string> refspec)
        {
            Debug.Assert(specFile != null, "The `specFile` parameter is null.");
            Debug.Assert(specFile.Exists, "The `specFile` parameter do not exist.");

            Trace.WriteLine("Program::ReadSpecFile");

            if (specFile.Exists)
            {
                values = new List<string>();
                refspec = new List<string>();

                Trace.WriteLine("   '" + specFile.FullName + "' found.");

                using (var stream = specFile.OpenRead())
                using (var reader = new StreamReader(stream))
                {
                    string line = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (String.IsNullOrWhiteSpace(line))
                            continue;

                        line = line.Trim();

                        if (line[0] == '#' || line[0] == ';')
                            continue;

                        Match match;
                        if ((match = Regex.Match(line, @"^fetch=+(.+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success)
                        {
                            string value = match.Groups[1].Value;
                            refspec.Add(value);

                            Trace.WriteLine(" ref+=" + value);
                        }
                        else
                        {
                            values.Add(line);

                            Trace.WriteLine("   " + line);
                        }
                    }
                }

                Trace.WriteLine("   " + values.Count + " entries found.");
                Trace.WriteLine("   " + refspec.Count + " refspec found.");
            }

            values = null;
            refspec = null;
            return false;
        }

        internal static bool IsEnabled(GitConfiguration config)
        {
            Debug.Assert(config != null, "The `config` parameter is null.");

            return config.ContainsKey(ConfigValueName)
                && String.Equals(config[ConfigValueName], "true", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool WriteSparseConfig(FileInfo sparseFile, IEnumerable<string> maps, IEnumerable<string> nots)
        {
            Debug.Assert(sparseFile != null, "The `sparseFile` parameter is null.");
            Debug.Assert(maps != null, "The `maps` parameter is null.");
            Debug.Assert(nots != null, "The `nots` parameter is null.");

            Trace.WriteLine("Program::WriteSparseConfig");

            if (sparseFile.Exists)
            {
                sparseFile.Delete();
            }

            using (var stream = sparseFile.Create())
            {
                byte[] buffer = new byte[16 * 1024];
                byte[] eol = Encoding.UTF8.GetBytes("\n");

                foreach (string value in maps)
                {
                    int length = Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, 0);
                    stream.Write(buffer, 0, length);
                    stream.Write(eol, 0, eol.Length);
                }

                foreach (string value in nots)
                {
                    int length = Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, 0);
                    stream.Write(buffer, 0, length);
                    stream.Write(eol, 0, eol.Length);
                }
            }

            return true;
        }

        internal static bool WriteSparseConfig(GitConfiguration config, IEnumerable<string> maps, IEnumerable<string> nots)
        {
            const string DotGitInfoFolderName = "info";

            Debug.Assert(config != null, "The `config` parameter is null.");
            Debug.Assert(maps != null, "The `maps` parameter is null.");
            Debug.Assert(nots != null, "The `nots` parameter is null.");

            Trace.WriteLine("Program::WriteSparseConfig");

            if (config.LocalPath != null)
            {
                Trace.WriteLine("   local config at '" + config.LocalPath + "'.");

                if (config.LocalPath != null && File.Exists(config.LocalPath))
                {
                    string dotgitPath = Path.GetDirectoryName(config.LocalPath);
                    string configPath = Path.Combine(dotgitPath, DotGitInfoFolderName, ConfigFileName);

                    FileInfo sparseFile = new FileInfo(configPath);

                    return WriteSparseConfig(sparseFile, maps, nots);
                }

                Trace.WriteLine("   " + ConfigFileName + " not found.");
            }
            else
            {
                Trace.WriteLine("   no local config detected, not a git repo.");
            }

            return false;
        }

        [Conditional("DEBUG")]
        private static void EnableDebugTrace()
        {
            // use the stderr stream for the trace as stdout is used in the cross-process communications protocol
            Trace.Listeners.Add(new ConsoleTraceListener(useErrorStream: true));
        }

        private static void LoadAssemblyInformation()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            var asseName = assembly.GetName();

            _exeutablePath = assembly.Location;
            _location = Path.GetDirectoryName(_exeutablePath);
            _name = asseName.Name;
            _version = asseName.Version;
        }

        private static void LogEvent(string message, EventLogEntryType eventType)
        {
            //const string EventSource = "Git Credential Manager";

            /*** commented out due to UAC issues which require a proper installer to work around ***/

            //Trace.WriteLine("Program::LogEvent");

            //if (!EventLog.SourceExists(EventSource))
            //{
            //    EventLog.CreateEventSource(EventSource, "Application");

            //    Trace.WriteLine("   event source created");
            //}

            //EventLog.WriteEntry(EventSource, message, eventType);

            //Trace.WriteLine("   " + eventType + "event written");
        }

        delegate bool SparseAction(string[] args, GitConfiguration gitConfiguration);
    }
}
