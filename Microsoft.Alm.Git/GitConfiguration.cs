using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.Alm.Git
{
    public sealed class GitConfiguration
    {
        public static readonly string[] LegalConfigNames = { "local", "global", "xdg", "system" };

        private const char HostSplitCharacter = '.';

        public GitConfiguration(string directory)
        {
            if (String.IsNullOrWhiteSpace(directory))
                throw new ArgumentNullException("directory");
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException(directory);

            LoadGitConfiguation(directory);
        }

        public GitConfiguration()
            : this(Environment.CurrentDirectory)
        { }

        internal GitConfiguration(TextReader configReader)
        {
            Debug.Assert(configReader != null, "The `configReader` parameter is null.");

            ParseGitConfig(configReader, _values);
        }

        /// <summary>
        /// Get the path, if any, to '.git/config' captured after 
        /// <see cref="LoadGitConfiguation(string)"/> is called.
        /// </summary>
        public string LocalPath { get; private set; }
        /// <summary>
        /// Get the path, if any, to '~/.gitconfig' captured after 
        /// <see cref="LoadGitConfiguation(string)"/> is called.
        /// </summary>
        public string GlobalPath { get; private set; }
        /// <summary>
        /// /// <summary>
        /// Get the path, if any, to 'etc/gitconfig' captured after 
        /// <see cref="LoadGitConfiguation(string)"/> is called.
        /// </summary>
        /// </summary>
        public string SystemPath { get; private set; }
        /// <summary>
        /// Get the path, if any, to XDG config captured after 
        /// <see cref="LoadGitConfiguation(string)"/> is called.
        /// </summary>
        public string XdgPath { get; private set; }

        private readonly Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string this[string key]
        {
            get { return _values[key]; }
        }

        public bool ContainsKey(string key)
        {
            return _values.ContainsKey(key);
        }

        /// <summary>
        /// Attempts to read an entry from a <see cref="GitConfiguration"/> using a multi-part naming scheme.
        /// </summary>
        /// <param name="prefix">
        /// <para>The first part of the config key.</para>
        /// <para>Example: "branch" in "branch.master.remote".</para>
        /// </param>
        /// <param name="name">
        /// <para>The middle part of the config key.</para>
        /// <para>Example: "master" in "branch.master.remote"./para>
        /// </param>
        /// <param name="suffix">
        /// <para>The last part of the config key.</para>
        /// <para>Example: "remote" in "branch.master.remote"./para>
        /// </param>
        /// <param name="entry">The output entry value if successful.</param>
        /// <returns><see langword="True"/> if successful, <see langword="false"/> otherwise.</returns>
        public bool TryGetEntry(string prefix, string name, string suffix, out Entry entry)
        {
            if (prefix == null)
                throw new ArgumentNullException("prefix", "The `prefix` parameter cannot be null.");
            if (prefix == null)
                throw new ArgumentNullException("suffix", "The `suffix` parameter cannot be null.");

            Trace.WriteLine("Configuration::TryGetEntry");

            string key = String.IsNullOrEmpty(name)
                ? String.Format("{0}.{1}", prefix, suffix)
                : String.Format("{0}.{1}.{2}", prefix, name, suffix);

            return TryGetEntry(key, out entry);
        }

        /// <summary>
        /// Attempts to read an entry from a <see cref="GitConfiguration"/> using a Uri regression pattern.
        /// </summary>
        /// <param name="prefix">
        /// <para>The first part of the config key.</para>
        /// <para>Example: "credential" in "credential.https://github.com.authority".</para>
        /// </param>
        /// <param name="targetUri">
        /// <para>The first part of the config key.</para>
        /// <para>Example: "https://github.com" in "credential.https://github.com.authority".</para>
        /// </param>
        /// <param name="suffix">
        /// <para>The first part of the config key.</para>
        /// <para>Example: "authority" in "credential.https://github.com.authority".</para>
        /// </param>
        /// <param name="entry">The output entry value if successful.</param>
        /// <returns><see langword="True"/> if successful, <see langword="false"/> otherwise.</returns>
        public bool TryGetEntry(string prefix, Uri targetUri, string suffix, out Entry entry)
        {
            if (prefix == null)
                throw new ArgumentNullException("prefix", "The `prefix` parameter cannot be null.");
            if (targetUri == null)
                throw new ArgumentNullException("targetUri", "The `targetUri` parameter cannot be null.");
            if (suffix == null)
                throw new ArgumentNullException("suffix", "The `suffix` parameter cannot be null.");

            Trace.WriteLine("Configuration::TryGetEntry");

            if (targetUri != null)
            {
                // return match seeking from most specific (<prefix>.<scheme>://<host>.<key>) to least specific (credential.<key>)
                if (TryGetEntry(prefix, String.Format("{0}://{1}", targetUri.Scheme, targetUri.Host), suffix, out entry)
                    || TryGetEntry(prefix, targetUri.Host, suffix, out entry))
                    return true;

                if (!String.IsNullOrWhiteSpace(targetUri.Host))
                {
                    string[] fragments = targetUri.Host.Split(HostSplitCharacter);
                    string host = null;

                    // look for host matches stripping a single sub-domain at a time off
                    // don't match against a top-level domain (aka ".com")
                    for (int i = 1; i < fragments.Length - 1; i++)
                    {
                        host = String.Join(".", fragments, i, fragments.Length - i);
                        if (TryGetEntry(prefix, host, suffix, out entry))
                            return true;
                    }
                }
            }

            // try to find an unadorned match as a complete fallback
            if (TryGetEntry(prefix, String.Empty, suffix, out entry))
                return true;

            // nothing found
            entry = default(Entry);
            return false;
        }

        /// <summary>
        /// Attempts to read an entry from a <see cref="GitConfiguration"/>.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <param name="entry">The output entry value if successful.</param>
        /// <returns><see langword="True"/> if successful, <see langword="false"/> otherwise.</returns>
        public bool TryGetEntry(string key, out Entry entry)
        {
            if (key == null)
                throw new ArgumentNullException("key", "The `key` parameter cannot be null.");

            Trace.WriteLine("Configuration::TryGetEntry");

            if (_values.ContainsKey(key))
            {
                entry = new Entry(key, _values[key]);
                return true;
            }

            entry = default(Entry);
            return false;
        }

        /// <summary>
        /// Attempts to read a typed entry from a <see cref="GitConfiguration"/>.
        /// </summary>
        /// <typeparam name="T">The type of the expected value.</typeparam>
        /// <param name="key">The configuration key.</param>
        /// <param name="converter">
        /// Converts the string serialzed version of the vaule to a typed version of the value.
        /// </param>
        /// <param name="entry">The output entry value if successful.</param>
        /// <returns><see langword="True"/> if successful, <see langword="false"/> otherwise.</returns>
        public bool TryGetEntry<T>(string key, Func<string, T> converter, out Entry<T> entry)
        {
            if (key == null)
                throw new ArgumentNullException("key", "The `key` parameter cannot be null.");
            if (converter == null)
                throw new ArgumentNullException("converter", "The `converter` parameter cannot be null.");

            Trace.WriteLine("Configuration::TryGetEntry<T>");

            if (_values.ContainsKey(key))
            {
                T value = converter(_values[key]);
                entry = new Entry<T>(key, value);
                return true;
            }

            entry = default(Entry<T>);
            return false;
        }

        public void LoadGitConfiguation(string directory)
        {
            string path = null;

            Trace.WriteLine("Configuration::LoadGitConfiguation");

            // read Git's three configs from lowest priority to highest, overwriting values as
            // higher priority configurations are parsed, storing them in a handy lookup table

            // find and parse Git's system config
            if (Where.GitSystemConfig(out path))
            {
                this.SystemPath = path;
                ParseGitConfig(path);
            }

            // find and parse Git's xdg config
            if (Where.GitXdgConfig(out path))
            {
                this.XdgPath = path;
                ParseGitConfig(path);
            }

            // find and parse Git's global config
            if (Where.GitGlobalConfig(out path))
            {
                this.GlobalPath = path;
                ParseGitConfig(path);
            }

            // find and parse Git's local config
            if (Where.GitLocalConfig(directory, out path))
            {
                this.LocalPath = path;
                ParseGitConfig(path);
            }

            foreach (var pair in _values)
            {
                Trace.WriteLine(pair.Key + " = " + pair.Value);
            }
        }

        private void ParseGitConfig(string configPath)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(configPath), "The `configPath` parameter is null or invalid.");
            Debug.Assert(File.Exists(configPath), "The `configPath` parameter references a non-existent file.");

            Trace.WriteLine("Configuration::ParseGitConfig");

            if (String.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                return;

            using (var stream = File.OpenRead(configPath))
            using (var reader = new StreamReader(stream))
            {
                ParseGitConfig(reader, _values);
            }
        }

        internal static void ParseGitConfig(TextReader reader, IDictionary<string, string> destination)
        {
            Debug.Assert(reader != null, "The `reader` parameter is null.");
            Debug.Assert(destination != null, "The `destination` parameter is null.");

            Match match = null;
            string section = null;

            // parse each line in the config independently - Git's configs do not accept multi-line values
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                // skip empty and commented lines
                if (String.IsNullOrWhiteSpace(line))
                    continue;
                if (Regex.IsMatch(line, @"^\s*[#;]", RegexOptions.Compiled | RegexOptions.CultureInvariant))
                    continue;

                // sections begin with values like [section] or [section "section name"]. All subsequent lines,
                // until a new section is encountered, are children of the section
                if ((match = Regex.Match(line, @"^\s*\[\s*(\w+)\s*(\""[^\]]+){0,1}\]", RegexOptions.Compiled | RegexOptions.CultureInvariant)).Success)
                {
                    if (match.Groups.Count >= 2 && !String.IsNullOrWhiteSpace(match.Groups[1].Value))
                    {
                        section = match.Groups[1].Value.Trim();

                        // check if the section is named, if so: process the name
                        if (match.Groups.Count >= 3 && !String.IsNullOrWhiteSpace(match.Groups[2].Value))
                        {
                            string val = match.Groups[2].Value.Trim();

                            // triming off enclosing quotes makes usage easier, only trim in pairs
                            if (val.Length > 0 && val[0] == '"')
                            {
                                if (val[val.Length - 1] == '"' && val.Length > 1)
                                {
                                    val = val.Substring(1, val.Length - 2);
                                }
                                else
                                {
                                    val = val.Substring(1, val.Length - 1);
                                }
                            }

                            section += HostSplitCharacter + val;
                        }
                    }
                }
                // section children should be in the format of name = value pairs
                else if ((match = Regex.Match(line, @"^\s*(\w+)\s*=\s*(.+)", RegexOptions.Compiled | RegexOptions.CultureInvariant)).Success)
                {
                    if (match.Groups.Count >= 3
                        && !String.IsNullOrEmpty(match.Groups[1].Value)
                        && !String.IsNullOrEmpty(match.Groups[2].Value))
                    {
                        string key = section + HostSplitCharacter + match.Groups[1].Value.Trim();
                        string val = match.Groups[2].Value.Trim();

                        // triming off enclosing quotes makes usage easier, only trim in pairs
                        if (val.Length > 0 && val[0] == '"')
                        {
                            if (val[val.Length - 1] == '"' && val.Length > 1)
                            {
                                val = val.Substring(1, val.Length - 2);
                            }
                            else
                            {
                                val = val.Substring(1, val.Length - 1);
                            }
                        }

                        // add or update the (key, value)
                        if (destination.ContainsKey(key))
                        {
                            destination[key] = val;
                        }
                        else
                        {
                            destination.Add(key, val);
                        }
                    }
                }
            }
        }

        public struct Entry
        {
            public Entry(string key, string value)
            {
                Key = key;
                Value = value;
            }

            public readonly string Key;
            public readonly string Value;
        }

        public struct Entry<T>
        {
            public Entry(string key, T value)
            {
                Key = key;
                Value = value;
            }

            public readonly string Key;
            public readonly T Value;
        }

        [Flags]
        public enum Type
        {
            None = 0,
            Local = 1 << 0,
            Global = 1 << 1,
            Xdg = 1 << 2,
            System = 1 << 3,
        }
    }
}
