// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using NuGet;

namespace Microsoft.Framework.PackageManager.Restore.NuGet
{
    public class KpmPackageFolder : IPackageFeed
    {
        private readonly IReport _report;
        private readonly PackageRepository _repository;
        private readonly IFileSystem _fileSystem;
        private readonly IPackagePathResolver _pathResolver;

        public string Source { get; }

        public KpmPackageFolder(
            string physicalPath,
            IReport report)
        {
            // We need to help "kpm restore" to ensure case-sensitivity here
            // Turn on the flag to get package ids in accurate casing
            _repository = new PackageRepository(physicalPath, checkPackageIdCase: true);
            _fileSystem = new PhysicalFileSystem(physicalPath);
            _pathResolver = new DefaultPackagePathResolver(_fileSystem);
            _report = report;
            Source = physicalPath;
        }

        public Task<IEnumerable<PackageInfo>> FindPackagesByIdAsync(string id)
        {
            return Task.FromResult(_repository.FindPackagesById(id).Select(p => new PackageInfo
            {
                Id = p.Id,
                Version = p.Version
            }));
        }

        public Task<Stream> OpenNuspecStreamAsync(PackageInfo package)
        {
            var nuspecPath = _pathResolver.GetManifestFilePath(package.Id, package.Version);
            _report.WriteLine(string.Format("  OPEN {0}", _fileSystem.GetFullPath(nuspecPath)));
            return Task.FromResult<Stream>(File.OpenRead(nuspecPath));
        }

        public Task<Stream> OpenNupkgStreamAsync(PackageInfo package)
        {
            var nuspecPath = _pathResolver.GetManifestFilePath(package.Id, package.Version);
            var unzippedPackage = new UnzippedPackage(_fileSystem, nuspecPath);

            var nupkgPath = _pathResolver.GetPackageFilePath(package.Id, package.Version);
            _report.WriteLine(string.Format("  OPEN {0}", _fileSystem.GetFullPath(nupkgPath)));

            return Task.FromResult(unzippedPackage.GetStream());
        }
    }
}

