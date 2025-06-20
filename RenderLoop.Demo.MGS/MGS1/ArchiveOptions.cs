// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.MGS1
{
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.IO;
    using System.IO.Abstractions;
    using DiscUtils.Complete;
    using DiscUtils.Iso9660;
    using DiscUtils.Streams;
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
            command.AddOption(KeyOption);
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

            void RegisterCD(string cdPath)
            {
                services.AddKeyedSingleton((path: file, cdPath), (s, key) => s.GetRequiredKeyedService<MArchiveV1VirtualFileSystem>(key.path)!.File.OpenRead(key.cdPath));
                services.AddKeyedSingleton((path: file, cdPath), (s, key) => new CDSectorStream(s.GetRequiredKeyedService<FileSystemStream>(key)!, CDSectorStream.XAForm1));
                services.AddKeyedSingleton((path: file, cdPath), (s, key) => new CDReader(s.GetRequiredKeyedService<CDSectorStream>(key), joliet: false));
                services.AddKeyedSingleton((path: file, cdPath, gamePath: WellKnownPaths.FaceDatPath), (s, key) => s.GetRequiredKeyedService<CDReader>((key.path, key.cdPath))!.OpenFile(key.gamePath, FileMode.Open));
                services.AddKeyedSingleton((path: file, cdPath, gamePath: WellKnownPaths.StageDirPath), (s, key) => s.GetRequiredKeyedService<CDReader>((key.path, key.cdPath))!.OpenFile(key.gamePath, FileMode.Open));
                services.AddKeyedSingleton((path: file, cdPath, gamePath: WellKnownPaths.StageDirPath), (s, key) => new StageDirVirtualFileSystem(s.GetRequiredKeyedService<SparseStream>(key)!));
            }

            RegisterCD(WellKnownPaths.CD1Path);
            RegisterCD(WellKnownPaths.CD2Path);
        }
    }
}
