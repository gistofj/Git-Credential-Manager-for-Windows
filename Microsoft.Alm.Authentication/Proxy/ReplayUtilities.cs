/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the """"Software""""), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
**/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.StringComparer;

namespace Microsoft.Alm.Authentication.Test
{
    public class ReplayUtilities : Git.IUtilities, IReplayService<CapturedUtilitiesData>
    {
        internal ReplayUtilities(RuntimeContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            _captures = new Queue<CapturedRemoteHttpDetails>();
            _context = context;
            _replayed = new List<CapturedRemoteHttpDetails>();
            _syncpoint = new object();
        }

        private readonly Queue<CapturedRemoteHttpDetails> _captures;
        private readonly RuntimeContext _context;
        private readonly List<CapturedRemoteHttpDetails> _replayed;
        private readonly object _syncpoint;

        public string SeriveName
            => "Utilities";

        public Type ServiceType
            => typeof(Git.IUtilities);

        public bool TryReadGitRemoteHttpDetails(out string commandLine, out string imagePath)
        {
            if (!TryReadNext(out CapturedRemoteHttpDetails details))
                throw new ReplayNotFoundException($"Unexpected call to {nameof(TryReadGitRemoteHttpDetails)}.");

            commandLine = details.CommandLine;
            imagePath = details.ImagePath;

            return details.Result;
        }

        private bool TryReadNext(out CapturedRemoteHttpDetails details)
        {
            lock(_syncpoint)
            {
                if (_captures.Count > 0)
                {
                    var capture = _captures.Dequeue();

                    details = new CapturedRemoteHttpDetails
                    {
                        CommandLine = capture.CommandLine,
                        ImagePath = capture.ImagePath,
                        Result = capture.Result,
                    };

                    return true;
                }

                details = default(CapturedRemoteHttpDetails);
                return false;
            }
        }

        string IProxyService.ServiceName
            => SeriveName;

        void IReplayService<CapturedUtilitiesData>.SetReplayData(CapturedUtilitiesData replayData)
        {
            if (replayData.Details is null)
                return;

            lock (_syncpoint)
            {
                foreach(var detail in replayData.Details)
                {
                    _captures.Enqueue(detail);
                }
            }
        }

        void IReplayService.SetReplayData(object replayData) => throw new NotImplementedException();
    }
}
