using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Alm.Git
{
    public sealed class GitProcess : IDisposable
    {
        private const string CmdExePath = "cmd";
        private static readonly object @lock = new object();

        /// <summary>
        /// Creates and starts a new Git process.
        /// </summary>
        /// <param name="options"></param>
        public GitProcess(GitProcessOptions options)
        {
            GitProcessOptions.Validate(ref options, "options");

            Trace.WriteLine("GitProcess");

            var startinfo = new ProcessStartInfo()
            {
                Arguments = options.Command,
                CreateNoWindow = options.ShowWindow,
                FileName = GitCmdPath,
                RedirectStandardError = options.ErrorBuffer != null,
                RedirectStandardOutput = options.OutputBuffer != null,
                UseShellExecute = false,
                WorkingDirectory = options.WorkingDirectory,
            };

            if (options.ShowWindow)
            {
                startinfo.Arguments = String.Format("/c pushd \"{0}\" {1}", GitCmdPath, options.Command);
                startinfo.FileName = CmdExePath;
            }

            if (options.RunElevated)
            {
                startinfo.UseShellExecute = true;
                startinfo.Verb = "runas";
            }

            this.Command = options.Command;
            this.WorkingDirectory = options.WorkingDirectory;

            _stderrBuffer = options.ErrorBuffer;
            _stdoutBuffer = options.OutputBuffer;

            Trace.WriteLine("   " + startinfo.FileName + " " + startinfo.Arguments);

            _process = Process.Start(startinfo);

            if (options.CaptureOutput)
            {
                Trace.WriteLine("   capturing git process output.");

                _process.EnableRaisingEvents = true;

                if (_stderrBuffer != null && startinfo.RedirectStandardError)
                {
                    _process.ErrorDataReceived += ProcessStderrWrite;
                }
                if (_stdoutBuffer != null && startinfo.RedirectStandardOutput)
                {
                    _process.OutputDataReceived += ProcessStdoutWrite;
                }
            }
        }
        ~GitProcess()
        {
            this.Dispose();
        }

        /// <summary>
        /// Gets the Git command used to invoke the <see cref="GitProcess"/>.
        /// </summary>
        public readonly string Command;
        /// <summary>
        /// Get the exit code of the process. If the process has not yet exited, zero is returned.
        /// </summary>
        public int ExitCode
        {
            get
            {
                Debug.Assert(_process != null, "The `_process` is null.");

                return (_process.HasExited)
                    ? _process.ExitCode
                    : 0;
            }
        }
        /// <summary>
        /// Gets if the <see cref="GitProcess"/> has exited or not.
        /// </summary>
        public bool Exited
        {
            get
            {
                Debug.Assert(_process != null, "The `_process` is null.");

                return _process.HasExited;
            }
        }
        /// <summary>
        /// Gets the path to "git.exe"
        /// </summary>
        public static string GitCmdPath
        {
            get
            {
                lock (@lock)
                {
                    if (_gitCmdPath == null)
                    {
                        List<GitInstallation> installations;
                        if (Where.FindGitInstallations(out installations))
                        {
                            _gitCmdPath = installations[0].Cmd;
                        }
                    }

                    return _gitCmdPath;
                }
            }
        }
        private static string _gitCmdPath;
        /// <summary>
        /// Gets the working directory of the <see cref="GitProcess"/>.
        /// </summary>
        public readonly string WorkingDirectory;

        private bool _disposed;
        private readonly Process _process;
        private readonly TextWriter _stderrBuffer;
        private readonly TextWriter _stdoutBuffer;

        /// <summary>
        /// Releases all system resources used by <see cref="GitProcess"/>.
        /// </summary>
        public void Dispose()
        {
            lock (@lock)
            {
                if (!_disposed)
                {
                    _process.Dispose();
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// Terminates a running <see cref="GitProcess"/>.
        /// </summary>
        /// <returns></returns>
        public bool Kill()
        {
            Debug.Assert(_process != null, "`_process` is null.");
            Debug.Assert(!_process.HasExited, "The `_process` has exited.");

            if (!_process.HasExited)
            {
                try
                {
                    _process.Kill();
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates and runs a process of git.exe with a specified set of options.
        /// </summary>
        /// <param name="command">The command options to supply to git.exe.</param>
        /// <param name="workingDirectory">The directory git.exe should be started in.</param>
        /// <param name="showWindow">Displays the command window used to execute git.exe.</param>
        /// <param name="wait">
        /// The amount of time (measured in milliseconds) to wait before killing the process.
        /// </param>
        /// <param name="expectedExitCode">The git.exe exit code expected if it exited successfully.</param>
        /// <returns><see langword="True"/> if success; otherwise <see langword="false"/>.</returns>
        public static bool Run(string command, string workingDirectory, bool showWindow, TimeSpan wait, int expectedExitCode)
            => InternalRun(command, workingDirectory, showWindow, null, null, wait, expectedExitCode);

        /// <summary>
        /// Creates and runs a process of git.exe with a specified set of options.
        /// </summary>
        /// <param name="command">The command options to supply to git.exe.</param>
        /// <param name="workingDirectory">The directory git.exe should be started in.</param>
        /// <param name="outputBuffer"></param>
        /// <param name="errorBuffer"></param>
        /// <param name="wait">
        /// The amount of time (measured in milliseconds) to wait before killing the process.
        /// </param>
        /// <param name="expectedExitCode">The git.exe exit code expected if it exited successfully.</param>
        /// <returns><see langword="True"/> if success; otherwise <see langword="false"/>.</returns>
        public static bool Run(string command, string workingDirectory, TextWriter outputBuffer, TextWriter errorBuffer, TimeSpan wait, int expectedExitCode)
            => InternalRun(command, workingDirectory, false, outputBuffer, errorBuffer, wait, expectedExitCode);

        /// <summary>
        /// Creates and runs a process of git.exe with a specified set of options.
        /// </summary>
        /// <param name="command">The command options to supply to git.exe.</param>
        /// <param name="workingDirectory">The directory git.exe should be started in.</param>
        /// <param name="outputBuffer">A buffer to write standard output from git.exe to.</param>
        /// <param name="errorBuffer">A buffer to write standard error from git.exe to.</param>
        /// <param name="expectedExitCode">The git.exe exit code expected if it exited successfully.</param>
        /// <returns><see langword="True"/> if success; otherwise <see langword="false"/>.</returns>
        public static bool Run(string command, string workingDirectory, TextWriter outputBuffer, TextWriter errorBuffer, int expectedExitCode)
            => InternalRun(command, workingDirectory, false, outputBuffer, errorBuffer, TimeSpan.MaxValue, expectedExitCode);

        /// <summary>
        /// Creates and runs a process of git.exe with a specified set of options.
        /// </summary>
        /// <param name="command">The command options to supply to git.exe.</param>
        /// <param name="workingDirectory">The directory git.exe should be started in.</param>
        /// <param name="outputBuffer">A buffer to write standard output from git.exe to.</param>
        /// <param name="errorBuffer">A buffer to write standard error from git.exe to.</param>
        /// <returns><see langword="True"/> if success; otherwise <see langword="false"/>.</returns>
        public static bool Run(string command, string workingDirectory, TextWriter outputBuffer, TextWriter errorBuffer)
            => InternalRun(command, workingDirectory, false, outputBuffer, errorBuffer, TimeSpan.MaxValue, 0);

        /// <summary>
        /// Creates and runs a process of git.exe with a specified set of options.
        /// </summary>
        /// <param name="command">The command options to supply to git.exe.</param>
        /// <param name="workingDirectory">The directory git.exe should be started in.</param>
        /// <param name="wait">
        /// The amount of time (measured in milliseconds) to wait before killing the process.
        /// </param>
        /// <param name="expectedExitCode">The git.exe exit code expected if it exited successfully.</param>
        /// <returns><see langword="True"/> if success; otherwise <see langword="false"/>.</returns>
        public static bool Run(string command, string workingDirectory, TimeSpan wait, int expectedExitCode)
            => InternalRun(command, workingDirectory, false, null, null, wait, expectedExitCode);

        /// <summary>
        /// Creates and runs a process of git.exe with a specified set of options.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="outputBuffer"></param>
        /// <param name="errorBuffer"></param>
        /// <returns><see langword="True"/> if success; otherwise <see langword="false"/>.</returns>
        public static bool Run(string command, TextWriter outputBuffer, TextWriter errorBuffer)
            => InternalRun(command, Environment.CurrentDirectory, false, outputBuffer, errorBuffer, TimeSpan.MaxValue, 0);

        /// <summary>
        /// Creates and runs a process of git.exe with a specified set of options.
        /// </summary>
        /// <param name="command">The command options to supply to git.exe.</param>
        /// <param name="workingDirectory">The directory git.exe should be started in.</param>
        /// <param name="expectedExitCode">The git.exe exit code expected if it exited successfully.</param>
        /// <returns><see langword="True"/> if success; otherwise <see langword="false"/>.</returns>
        public static bool Run(string command, string workingDirectory, int expectedExitCode)
            => InternalRun(command, workingDirectory, false, null, null, TimeSpan.MaxValue, expectedExitCode);

        /// <summary>
        /// Creates and runs a process of git.exe with a specified set of options.
        /// </summary>
        /// <param name="command">The command options to supply to git.exe.</param>
        /// <param name="workingDirectory">The directory git.exe should be started in.</param>
        /// <param name="showWindow">Displays the command window used to execute git.exe.</param>
        /// <returns><see langword="True"/> if success; otherwise <see langword="false"/>.</returns>
        public static bool Run(string command, string workingDirectory, bool showWindow)
            => InternalRun(command, workingDirectory, showWindow, null, null, TimeSpan.MaxValue, 0);

        /// <summary>
        /// Creates and runs a process of git.exe with a specified set of options.
        /// </summary>
        /// <param name="command">The command options to supply to git.exe.</param>
        /// <param name="workingDirectory">The directory git.exe should be started in.</param>
        /// <returns><see langword="True"/> if success; otherwise <see langword="false"/>.</returns>
        public static bool Run(string command, string workingDirectory)
            => InternalRun(command, workingDirectory, false, null, null, TimeSpan.MaxValue, 0);

        /// <summary>
        /// Creates and runs a process of git.exe with a specified set of options.
        /// </summary>
        /// <param name="command">The command options to supply to git.exe.</param>
        /// <param name="showWindow">Displays the command window used to execute git.exe.</param>
        /// <returns><see langword="True"/> if success; otherwise <see langword="false"/>.</returns>
        public static bool Run(string command, bool showWindow)
            => InternalRun(command, Environment.CurrentDirectory, showWindow, null, null, TimeSpan.MaxValue, 0);

        /// <summary>
        /// Creates and runs a process of git.exe with a specified set of options.
        /// </summary>
        /// <param name="command">The command options to supply to git.exe.</param>
        /// <returns><see langword="True"/> if success; otherwise <see langword="false"/>.</returns>
        public static bool Run(string command)
            => InternalRun(command, Environment.CurrentDirectory, false, null, null, TimeSpan.MaxValue, 0);

        /// <summary>
        /// Waits for a specified amount of time for the process to exit.
        /// </summary>
        /// <param name="wait">
        /// <para>The amount of time to wait for the process to exit before giving up.</para>
        /// <para>
        /// Values less than or equal to <see cref="TimeSpan.Zero"/> or equal to 
        /// <see cref="TimeSpan.MaxValue"/> are treated as infinity.
        /// </para>
        /// </param>
        /// <returns><see langword="True"/> if success before timeout; otherwise <see langword="false"/>.</returns>
        public bool WaitForExit(TimeSpan wait)
        {
            Debug.Assert(_process != null, "The `_process` is null.");
            Debug.Assert(!_process.HasExited, "The `_process` has exited.");

            int milliseconds = (wait == TimeSpan.MaxValue || wait <= TimeSpan.Zero)
                ? -1
                : (int)wait.TotalMilliseconds;

            return _process.WaitForExit(milliseconds);
        }

        internal static bool InternalRun(string command, string workingDirectory, bool showWindow, TextWriter outputBuffer, TextWriter errorBuffer, TimeSpan wait, int expectedExitCode)
        {
            Debug.Assert(command != null, "The `command` parameter is null.");
            Debug.Assert(workingDirectory != null, "The `workingDirectory` parameter is null.");

            var options = new GitProcessOptions()
            {
                Command = command,
                ErrorBuffer = errorBuffer,
                OutputBuffer = outputBuffer,
                ShowWindow = showWindow,
                WorkingDirectory = workingDirectory,
            };

            if (!GitProcessOptions.IsValid(ref options, "options"))
                return false;

            GitProcess git = new GitProcess(options);

            return (git.WaitToKill(wait))
                ? git.ExitCode == expectedExitCode
                : false;
        }

        internal bool WaitToKill(TimeSpan wait)
        {
            if (this.WaitForExit(wait))
                return true;

            this.Kill();
            return false;
        }

        private void ProcessStderrWrite(Object sender, DataReceivedEventArgs args)
        {
            Debug.Assert(sender != null, "The `sender` pararmeter is null.");
            Debug.Assert(args != null, "The `args` pararmeter is null.");
            Debug.Assert(_stderrBuffer != null, "`_stderrBuffer` is null.");

            if (args.Data != null)
            {
                _stderrBuffer.Write(args.Data);
            }
        }

        private void ProcessStdoutWrite(Object sender, DataReceivedEventArgs args)
        {
            Debug.Assert(sender != null, "The `sender` pararmeter is null.");
            Debug.Assert(args != null, "The `args` pararmeter is null.");
            Debug.Assert(_stdoutBuffer != null, "`_stderrBuffer` is null.");

            if (args.Data != null)
            {
                _stdoutBuffer.Write(args.Data);
            }
        }
    }
}
