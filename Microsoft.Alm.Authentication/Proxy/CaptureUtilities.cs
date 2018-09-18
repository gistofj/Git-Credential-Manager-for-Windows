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

namespace Microsoft.Alm.Authentication.Test
{
    public class CaptureUtilities : ICaptureService<CapturedUtilitiesData>, Git.IUtilities
    {
        public CaptureUtilities(RuntimeContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            _captured = new Queue<CapturedRemoteHttpDetails>();
            _context = context;
            _utilities = context.Utilities;
            _syncpoint = new object();
        }

        private readonly Queue<CapturedRemoteHttpDetails> _captured;
        private readonly RuntimeContext _context;
        private readonly Git.IUtilities _utilities;
        private readonly object _syncpoint;

        public Type ServiceType
            => typeof(Git.IUtilities);

        public bool TryReadGitRemoteHttpDetails(out string commandLine, out string imagePath)
        {
            var result = _utilities.TryReadGitRemoteHttpDetails(out commandLine, out imagePath);

            Capture(result, commandLine, imagePath);

            return result;
        }

        internal bool GetCapturedData(ICapturedDataFilter filter, out CapturedUtilitiesData capturedData)
        {
            if (filter is null)
                throw new ArgumentNullException(nameof(filter));

            filter = new CapturedDataFilter(filter);

            lock (_syncpoint)
            {
                var details = new List<CapturedRemoteHttpDetails>(_captured.Count);

                foreach (var detail in _captured)
                {
                    details.Add(detail);
                }

                capturedData = new CapturedUtilitiesData
                {
                    Details = details,
                };
                return true;
            }
        }

        private void Capture(bool result, string commandLine, string imagePath)
        {
            var detail = new CapturedRemoteHttpDetails
            {
                CommandLine = commandLine,
                ImagePath = imagePath,
                Result = result,
            };
            _captured.Enqueue(detail);
        }

        string IProxyService.ServiceName
            => "Utilities";

        bool ICaptureService<CapturedUtilitiesData>.GetCapturedData(ICapturedDataFilter filter, out CapturedUtilitiesData capturedData)
            => GetCapturedData(filter, out capturedData);

        bool ICaptureService.GetCapturedData(ICapturedDataFilter filter, out object capturedData)
        {
            if (GetCapturedData(filter, out CapturedUtilitiesData capturedUtilitiesData))
            {
                capturedData = capturedUtilitiesData;
                return true;
            }

            capturedData = null;
            return false;
        }
    }
}
