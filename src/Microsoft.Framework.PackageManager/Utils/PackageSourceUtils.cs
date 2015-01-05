// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet;
using Microsoft.Framework.PackageManager.Restore.NuGet;

namespace Microsoft.Framework.PackageManager
{
    public static class PackageSourceUtils
    {
        public static List<PackageSource> GetEffectivePackageSources(IPackageSourceProvider sourceProvider,
            IEnumerable<string> sources, IEnumerable<string> fallbackSources)
        {
            var allSources = sourceProvider.LoadPackageSources();
            var enabledSources = sources.Any() ?
                Enumerable.Empty<PackageSource>() :
                allSources.Where(s => s.IsEnabled);

            var addedSources = sources.Concat(fallbackSources).Select(
                value => allSources.FirstOrDefault(source => CorrectName(value, source)) ?? new PackageSource(value));

            return enabledSources.Concat(addedSources).Distinct().ToList();
        }

        public static IPackageFeed CreatePackageFeed(PackageSource source, bool noCache, bool ignoreFailedSources,
            Reports reports)
        {
            if (new Uri(source.Source).IsFile)
            {
                if (!Directory.Exists(source.Source))
                {
                    reports.Information.WriteLine("Package source {0} doesn't exist",
                        source.Source.Yellow().Bold());
                    return null;
                }
                return PackageFolderFactory.CreatePackageFolderFromPath(source.Source, reports.Quiet);
            }
            else
            {
                return new NuGetv2Feed(
                    source.Source,
                    source.UserName,
                    source.Password,
                    noCache,
                    reports,
                    ignoreFailedSources);
            }
        }

        private static bool CorrectName(string value, PackageSource source)
        {
            return source.Name.Equals(value, StringComparison.CurrentCultureIgnoreCase) ||
                source.Source.Equals(value, StringComparison.OrdinalIgnoreCase);
        }
    }
}