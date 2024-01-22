namespace RenderLoop.MGS
{
    using System.IO.Abstractions;
    using System.IO;
    using DiscUtils.Iso9660;
    using DiscUtils.Streams;
    using Microsoft.Extensions.DependencyInjection;
    using RenderLoop.Archives;
    using DiscUtils.Complete;

    internal class ServiceRegistration
    {
        internal static void Register(IServiceCollection services, Program.Options options)
        {
            SetupHelper.SetupComplete();

            services.AddTransient<CodecDisplay>();
            services.AddTransient<ModelDisplay>();
            services.AddTransient<VehicleDisplay>();

            services.AddKeyedSingleton(options.File, (s, key) => new MArchiveV1VirtualFileSystem(key, s.GetRequiredService<Program.Options>().Key));

            void RegisterCD(string cdPath)
            {
                services.AddKeyedSingleton((path: options.File, cdPath), (s, key) => s.GetRequiredKeyedService<MArchiveV1VirtualFileSystem>(key.path)!.File.OpenRead(key.cdPath));
                services.AddKeyedSingleton((path: options.File, cdPath), (s, key) => new CDSectorStream(s.GetRequiredKeyedService<FileSystemStream>(key)!, CDSectorStream.XAForm1));
                services.AddKeyedSingleton((path: options.File, cdPath), (s, key) => new CDReader(s.GetRequiredKeyedService<CDSectorStream>(key), joliet: false));
                services.AddKeyedSingleton((path: options.File, cdPath, gamePath: WellKnownPaths.FaceDatPath), (s, key) => s.GetRequiredKeyedService<CDReader>((key.path, key.cdPath))!.OpenFile(key.gamePath, FileMode.Open));
                services.AddKeyedSingleton((path: options.File, cdPath, gamePath: WellKnownPaths.StageDirPath), (s, key) => s.GetRequiredKeyedService<CDReader>((key.path, key.cdPath))!.OpenFile(key.gamePath, FileMode.Open));
                services.AddKeyedSingleton((path: options.File, cdPath, gamePath: WellKnownPaths.StageDirPath), (s, key) => new StageDirVirtualFileSystem(s.GetRequiredKeyedService<SparseStream>(key)!));
            }

            RegisterCD(WellKnownPaths.CD1Path);
            RegisterCD(WellKnownPaths.CD2Path);
        }
    }
}
