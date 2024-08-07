using Mono.Cecil;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Celeste64.Mod;

/// <summary>
/// A mod's assembly context, which handles resolving/loading mod assemblies.
/// Heavily inspired by Everest: https://github.com/EverestAPI/Everest/blob/dev/Celeste.Mod.mm/Mod/Module/EverestModuleAssemblyContext.cs
/// </summary>
internal sealed class ModAssemblyLoadContext : AssemblyLoadContext
{
	/// <summary>
	/// A list of assembly names which must not be loaded by a mod. The list will be initialized upon first access (which is before any mods will have loaded).
	/// </summary>
	private static string[] AssemblyLoadBlackList => assemblyLoadBlackList ??= AssemblyLoadContext.Default.Assemblies.Select(asm => asm.GetName().Name)
		.Append("Mono.Cecil.Pdb").Append("Mono.Cecil.Mdb") // These two aren't picked up by default for some reason
		.ToArray()!;
	private static string[]? assemblyLoadBlackList = null;

	/// <summary>
	/// The folder name where mod unmanaged assemblies will be loaded from.
	/// </summary>
	private static string UnmanagedLibraryFolder => unmanagedLibraryFolder ??= (
			RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "lib-win-x64" :
			RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? RuntimeInformation.OSArchitecture switch
			{
				Architecture.X64 => "lib-linux-x64",
				Architecture.Arm => "lib-linux-arm",
				Architecture.Arm64 => "lib-linux-arm64",
				_ => throw new PlatformNotSupportedException(),
			} :
			RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "lib-osx-x64" :
			throw new PlatformNotSupportedException());
	private static string? unmanagedLibraryFolder = null;

	private static readonly ReaderWriterLockSlim allContextsLock = new();
	private static readonly Dictionary<string, ModAssemblyLoadContext> contextsByModId = new();
	private static readonly Dictionary<Assembly, ModAssemblyLoadContext> contextsByAssembly = new();

	// All modules loaded by the context.
	private readonly Dictionary<string, ModuleDefinition> assemblyModules = new();

	// Cache for this mod including dependencies.
	private readonly ConcurrentDictionary<string, Assembly> assemblyLoadCache = new();
	private readonly ConcurrentDictionary<string, IntPtr> assemblyUnmanagedLoadCache = new();

	// Cache for this mod specifically. Also used when loading from this mod as a dependency.
	private readonly ConcurrentDictionary<string, Assembly> localLoadCache = new();
	private readonly ConcurrentDictionary<string, IntPtr> localUnmanagedLoadCache = new();

	private readonly ModInfo info;
	private readonly IModFilesystem fs;
	private readonly List<ModAssemblyLoadContext> dependencyContexts = [];

	private bool isDisposed = false;

	internal ModAssemblyLoadContext(ModInfo info, IModFilesystem fs) : base(info.Id, isCollectible: true)
	{
		this.info = info;
		this.fs = fs;

		// Resolve dependencies
		foreach (var (modId, _) in info.Dependencies)
		{
			if (contextsByModId.TryGetValue(modId, out var alc))
				dependencyContexts.Add(alc);
		}
		allContextsLock.EnterWriteLock();
		contextsByModId.TryAdd(info.Id, this);
		allContextsLock.ExitWriteLock();

		// Load all assemblies
		foreach (var assemblyPath in fs.FindFilesInDirectory(Assets.LibrariesFolder, Assets.LibrariesExtensionAssembly))
		{
			LoadAssemblyFromModPath(assemblyPath);
		}
	}

	public void Dispose()
	{
		lock (this)
		{
			if (isDisposed)
				return;
			isDisposed = true;

			// Remove from mod ALC list
			allContextsLock.EnterWriteLock();
			contextsByModId.Remove(info.Id);
			foreach (var assembly in localLoadCache.Values)
				contextsByAssembly.Remove(assembly);
			allContextsLock.ExitWriteLock();

			// Unload all assemblies loaded in the context
			foreach (var module in assemblyModules.Values)
				module.Dispose();
			assemblyModules.Clear();

			assemblyLoadCache.Clear();
			localLoadCache.Clear();

			assemblyUnmanagedLoadCache.Clear();
			foreach (var handle in localUnmanagedLoadCache.Values)
				NativeLibrary.Free(handle);
			localUnmanagedLoadCache.Clear();
		}
	}
	
	private static readonly Assembly celeste64Asm = typeof(Game).Assembly;
	internal static ModInfo? GetInfoForCallingAssembly()
	{
		var asm = new StackTrace()
			.GetFrames()
			.Select(frame => frame.GetMethod()?.DeclaringType?.Assembly)
			.FirstOrDefault(asm => asm != celeste64Asm);
		
		return asm == null ? null : GetInfoForAssembly(asm);
	}
	
	internal static ModInfo? GetInfoForAssembly(Assembly asm)
	{
		allContextsLock.EnterWriteLock();
		ModAssemblyLoadContext? alc = null;
		if (contextsByAssembly.TryGetValue(asm, out var ctx))
			alc = ctx;
		allContextsLock.ExitWriteLock();
		
		return alc?.info;
	}

	protected override Assembly? Load(AssemblyName asmName)
	{
		lock (this)
		{
			// Lookup in the cache
			if (assemblyLoadCache.TryGetValue(asmName.Name!, out var cachedAsm))
				return cachedAsm;

			// Try to load the assembly locally (from this or dependency ALCs)
			// // If that fails, try to load the assembly globally (game assemblies)
			var asm = LoadManagedLocal(asmName) ?? LoadManagedGlobal(asmName);
			if (asm != null)
			{
				assemblyLoadCache.TryAdd(asmName.Name!, asm);

				return asm;
			}

			Log.Warning($"Failed to load assembly '{asmName.FullName}' for mod '{info.Id}'");
			return null;
		}
	}

	protected override IntPtr LoadUnmanagedDll(string name)
	{
		// Lookup in the cache
		if (assemblyUnmanagedLoadCache.TryGetValue(name, out var cachedHandle))
			return cachedHandle;

		// Try to load the unmanaged assembly locally (from this or dependency ALCs)
		// If that fails, don't fallback to loading it globally - unmanaged dependencies have to be explicitly specified
		var handle = LoadUnmanaged(name);
		if (handle.HasValue && handle.Value != IntPtr.Zero)
		{
			assemblyUnmanagedLoadCache.TryAdd(name, handle.Value);
			return handle.Value;
		}

		Log.Warning($"Failed to load native library '{name}' for mod '{info.Id}'");
		return IntPtr.Zero;
	}

	private Assembly? LoadManagedLocal(AssemblyName asmName)
	{
		// Try to load the assembly from this mod
		if (LoadManagedFromThisMod(asmName) is { } asm)
			return asm;

		// Try to load the assembly from dependency assembly contexts
		foreach (var depCtx in dependencyContexts)
		{
			if (depCtx.LoadManagedFromThisMod(asmName) is { } depAsm)
				return depAsm;
		}

		return null;
	}

	private Assembly? LoadManagedGlobal(AssemblyName asmName)
	{
		try
		{
			// Try to load the assembly from the default assembly load context
			if (AssemblyLoadContext.Default.LoadFromAssemblyName(asmName) is { } globalAsm)
				return globalAsm;
		}
		catch
		{
			// ignored
		}

		return null;
	}

	private IntPtr? LoadUnmanaged(string name)
	{
		// Try to load the assembly from this mod
		if (LoadUnmanagedFromThisMod(name) is { } handle)
			return handle;

		// Try to load the assembly from dependency assembly contexts
		foreach (var depCtx in dependencyContexts)
		{
			if (depCtx.LoadUnmanagedFromThisMod(name) is { } depHandle)
				return depHandle;
		}

		return null;
	}

	private Assembly? LoadManagedFromThisMod(AssemblyName asmName)
	{
		lock (this)
		{
			// Lookup in the cache
			if (localLoadCache.TryGetValue(asmName.Name!, out var asm))
				return asm;

			// Try to load the assembly from the same library directory
			return LoadAssemblyFromModPath(Path.Combine(Assets.LibrariesFolder, $"{asmName.Name!}.{Assets.LibrariesExtensionAssembly}"));
		}
	}

	private IntPtr? LoadUnmanagedFromThisMod(string name)
	{
		// Lookup in the cache
		if (localUnmanagedLoadCache.TryGetValue(name, out var handle))
			return handle;

		// Determine the OS-specific name of the assembly
		string osName =
			RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{name}.dll" :
			RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? $"lib{name}.so" :
			RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? $"lib{name}.dylib" :
			name;

		// Try multiple paths to load the library from
		foreach (string libName in new[] { name, osName })
		{
			string libraryPath = Path.Combine(Assets.LibrariesFolder, UnmanagedLibraryFolder, libName);

			// Extract the library into a temporary file to be able to read it
			// Even in the case of a directory mod, we can't use it directly, as it would be invalid to reference
			// the library once the file has changed (which is annoying, but ehh)
			// TODO: Store this in a consistent cache file inside the install?
			var tempFilePath = Path.GetTempFileName();
			using (var tempFile = File.OpenWrite(tempFilePath))
			{
				try
				{
					using var libraryStream = fs.OpenFile(libraryPath);
					libraryStream.CopyTo(tempFile);
				}
				catch
				{
					// Not found
					continue;
				}
			}

			// Try to load the native library from the temporary file
			if (NativeLibrary.TryLoad(tempFilePath, out handle))
			{
				localUnmanagedLoadCache.TryAdd(name, handle);
				return handle;
			}
		}

		return null;
	}

	private Assembly? LoadAssemblyFromModPath(string assemblyPath)
	{
		lock (this)
		{
			if (isDisposed)
				throw new ObjectDisposedException(nameof(ModAssemblyLoadContext));

			try
			{
				var symbolPath = Path.ChangeExtension(assemblyPath, $".{Assets.LibrariesExtensionSymbol}");

				using var assemblyStream = fs.OpenFile(assemblyPath);
				using var symbolStream = fs.FileExists(symbolPath) ? fs.OpenFile(symbolPath) : null;

				// If we load from a zipped mod, stream will be deflated, so we have to copy it to a memory stream in that case.
				Stream updatedAssemblyStream = assemblyStream;
				using var memStream = new MemoryStream();
				if (!assemblyStream.CanSeek)
				{
					assemblyStream.CopyTo(memStream);
					memStream.Position = 0;
					updatedAssemblyStream = memStream;
				}

				using var memSymbolStream = new MemoryStream();
				Stream? updatedSymbolStream = symbolStream;
				if (symbolStream != null && !assemblyStream.CanSeek)
				{
					symbolStream.CopyTo(memSymbolStream);
					memSymbolStream.Position = 0;
					updatedSymbolStream = memSymbolStream;
				}


				var module = ModuleDefinition.ReadModule(updatedAssemblyStream);
				if (AssemblyLoadBlackList.Contains(module.Assembly.Name.Name, StringComparer.OrdinalIgnoreCase))
					throw new Exception($"Attempted load of blacklisted assembly {module.Assembly.Name} from mod '{info.Id}'");

				// Reset stream back to beginning
				updatedAssemblyStream.Position = 0;

				var assembly = LoadFromStream(updatedAssemblyStream, updatedSymbolStream);
				var asmName = assembly.GetName().Name!;

				if (assemblyModules.TryAdd(asmName, module))
				{
					assemblyLoadCache.TryAdd(asmName, assembly);
					localLoadCache.TryAdd(asmName, assembly);
					
					allContextsLock.EnterWriteLock();
					contextsByAssembly.Add(assembly, this);
					allContextsLock.ExitWriteLock();
				}
				else
				{
					Log.Warning($"Assembly name conflict for name '{asmName}' in mod '{info.Id}'!");
				}

				return assembly;
			}
			catch
			{
				return null;
			}
		}
	}
}
