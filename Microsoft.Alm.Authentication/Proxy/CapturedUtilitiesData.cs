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

using Newtonsoft.Json;
using System.Collections.Generic;
using static System.FormattableString;

namespace Microsoft.Alm.Authentication.Test
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedUtilitiesData
    {
        [JsonProperty(PropertyName = "Details", NullValueHandling = NullValueHandling.Ignore)]
        public List<CapturedRemoteHttpDetails> Details { get; set; }

        internal string DebuggerDisplay
        {
            get { return Invariant($"{nameof(CapturedUtilitiesData)} Queries[{Details?.Count}]"); }
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct CapturedRemoteHttpDetails
    {
        [JsonProperty(PropertyName = "CommandLine", NullValueHandling = NullValueHandling.Ignore)]
        public string CommandLine { get; set; }

        [JsonProperty(PropertyName = "ImagePath", NullValueHandling = NullValueHandling.Ignore)]
        public string ImagePath { get; set; }

        [JsonProperty(PropertyName = "Result", NullValueHandling = NullValueHandling.Ignore)]
        public bool Result { get; set; }

        internal string DebuggerDisplay
        {
            get { return Invariant($"{nameof(CapturedRemoteHttpDetails)}: {(CommandLine ?? ImagePath ?? "<Invalid Data>")}"); }
        }
    }
}
