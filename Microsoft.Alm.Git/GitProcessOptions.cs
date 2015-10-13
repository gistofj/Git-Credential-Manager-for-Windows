using System;
using System.IO;

namespace Microsoft.Alm.Git
{
    public struct GitProcessOptions
    {
        /// <summary>
        /// The command Git is expected to execute
        /// </summary>
        /// <example>
        /// "log --oneline --decorate" would execute "git log --oneline --decorate"
        /// </example>
        public string Command;
        /// <summary>
        /// <para>A buffer for captured standard error to be written to.</para>
        /// <para>Not compatible with <seealso cref="ShowWindow"/>.</para>
        /// <para>Not compatible with <seealso cref="RunElevated"/>.</para>
        /// </summary>
        public TextWriter ErrorBuffer;
        /// <summary>
        /// <para>A buffer for captured standard output to be written to.</para>
        /// <para>Not compatible with <seealso cref="ShowWindow"/>.</para>
        /// <para>Not compatible with <seealso cref="RunElevated"/>.</para>
        /// </summary>
        public TextWriter OutputBuffer;
        /// <summary>
        /// <para>Forces the Git process to use ShellExecute and attempt to gain elevated privillages.</para>
        /// <para>Not compatible with <seealso cref="ErrorBuffer"/>.</para>
        /// <para>Not compatible with <seealso cref="OutputBuffer"/>.</para>
        /// </summary>
        public bool RunElevated;
        /// <summary>
        /// <para>Executes Git with in a conshot environement, enabling the user to see the activity.</para>
        /// <para>Not compatible with <seealso cref="ErrorBuffer"/>.</para>
        /// <para>Not compatible with <seealso cref="OutputBuffer"/>.</para>
        /// </summary>
        public bool ShowWindow;
        /// <summary>
        /// The working directory where Git will execute.
        /// </summary>
        public string WorkingDirectory;

        public bool CaptureOutput
        {
            get { return ErrorBuffer != null || OutputBuffer != null; }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Validate(ref GitProcessOptions options, string name)
        {
            if (String.IsNullOrWhiteSpace(options.Command))
                throw new ArgumentNullException(name, "The `Command` cannot be null or empty.");
            if (String.IsNullOrWhiteSpace(options.WorkingDirectory))
                throw new ArgumentNullException(name, "The `WorkingDirectory` cannot be null or empty.");
            if (!Directory.Exists(options.WorkingDirectory))
                throw new ArgumentException("The `WorkingDirectory` is invalid.", name, new DirectoryNotFoundException());
            if (options.ShowWindow && options.CaptureOutput)
                throw new ArgumentException("The `ShowWindow` value cannot true if output buffers are supplied.",
                                            name,
                                            new InvalidOperationException("Cannot capture output from a shown process."));
            if (options.RunElevated && options.CaptureOutput)
                throw new ArgumentException("The `RunElevated` value cannot true if output buffers are supplied.",
                                            name,
                                            new InvalidOperationException("Cannot capture output from an elevated process."));
        }

        public static bool IsValid(ref GitProcessOptions options, string name)
        {
            try
            {
                Validate(ref options, name);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
