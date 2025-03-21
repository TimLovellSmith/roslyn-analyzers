﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers
{
    public readonly struct PublicApiFile
    {
        public PublicApiFile(string path)
        {
            var fileName = Path.GetFileName(path);

            IsShipping = IsFile(fileName, DeclarePublicApiAnalyzer.ShippedFileNamePrefix);
            var isUnshippedFile = IsFile(fileName, DeclarePublicApiAnalyzer.UnshippedFileNamePrefix);

            IsApiFile = IsShipping || isUnshippedFile;
        }

        public bool IsShipping { get; }

        public bool IsApiFile { get; }

        private static bool IsFile(string path, string prefix)
            => path.StartsWith(prefix, StringComparison.Ordinal) && path.EndsWith(DeclarePublicApiAnalyzer.Extension, StringComparison.Ordinal);
    }
}
