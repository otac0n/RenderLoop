// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS1
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.IO;
    using System.IO.Abstractions;
    using DiscUtils.Complete;
    using DiscUtils.Iso9660;
    using Microsoft.Extensions.DependencyInjection;
    using RenderLoop.Demo.MGS.MGS1.Archives;

    internal class ArchiveOptions
    {
        public static readonly Option<string> KeyOption = new(
            name: "--key",
            description: "The key to the MGS1 alldata.bin file.")
        {
            IsRequired = true,
        };

        public required string Key { get; set; }

        public static void Attach(Command command)
        {
            command.AddGlobalOption(KeyOption);
        }

        public static void Bind(InvocationContext context, IServiceCollection services)
        {
            var options = new ArchiveOptions
            {
                Key = context.ParseResult.GetValueForOption(KeyOption)!,
            };

            services.AddSingleton(options);

            SetupHelper.SetupComplete();

            var file = WellKnownPaths.AllDataBin;
            services.AddKeyedSingleton(file, (s, key) => new MArchiveV1VirtualFileSystem(Path.Combine(s.GetRequiredService<Program.Options>().SteamApps, key), s.GetRequiredService<ArchiveOptions>().Key));
            services.AddKeyedSingleton(file, (s, key) => new NestedFileSystemManager(s.GetRequiredKeyedService<MArchiveV1VirtualFileSystem>(key),
                (file, fs, fsPath) =>
                {
                    if (fs is MArchiveV1VirtualFileSystem &&
                        string.Equals(Path.GetExtension(file), ".bin", StringComparison.OrdinalIgnoreCase) &&
                        Path.GetFileName(Path.GetDirectoryName(file)) == "roms")
                    {
                        return static (IFileSystem fs, string subPath) =>
                        {
                            var file = fs.File.OpenRead(subPath);
                            var cdSector = new CDSectorStream(file, CDSectorStream.XAForm1);
                            var cdReader = new CDReader(cdSector, joliet: false);
                            var subFs = new CDReaderVFSAdapter(cdReader);
                            return subFs;
                        };
                    }
                    else if (fs is CDReaderVFSAdapter &&
                        string.Equals(Path.GetFileName(file), "brf.dat", StringComparison.OrdinalIgnoreCase) &&
                        Path.GetFileName(Path.GetDirectoryName(file)) == "MGS")
                    {
                        return static (IFileSystem fs, string subPath) =>
                        {
                            var file = fs.File.OpenRead(subPath);
                            var subFs = new BrfDatVirtualFileSystem(file);
                            return subFs;
                        };
                    }
                    else if (fs is CDReaderVFSAdapter &&
                        string.Equals(Path.GetExtension(file), ".dir", StringComparison.OrdinalIgnoreCase) &&
                        Path.GetFileName(Path.GetDirectoryName(file)) == "MGS")
                    {
                        return static (IFileSystem fs, string subPath) =>
                        {
                            var file = fs.File.OpenRead(subPath);
                            var subFs = new StageDirVirtualFileSystem(file);
                            return subFs;
                        };
                    }

                    return null;
                }));
        }
    }
}
