using BakinTranslate.CLI.Options;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace BakinTranslate.CLI.Handler
{
    internal class DumpHandler
    {
        private static readonly HashSet<string> _KeySet = new HashSet<string>();
        private static readonly Dictionary<string, HashSet<string>> _KeySetsBySource = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private static readonly Stack<string> _SourcePathContext = new Stack<string>();
        private static string _pendingSourcePath;
        private static string _currentSourcePath;
        private static string _splitSourceRoot;
        private static bool _splitMode;

        private static void ReadStringPostfix(ref string __result, object __instance)
        {
            var keys = Helper.ConvertRawStringToKeys(__result).ToList();
            if (!_splitMode)
            {
                foreach (var key in keys)
                    _KeySet.Add(key);
                return;
            }

            var sourcePath = GetSourceOutputRelativePath(__instance) ?? "unknown.txt";
            if (!_KeySetsBySource.TryGetValue(sourcePath, out var keySet))
            {
                keySet = new HashSet<string>();
                _KeySetsBySource[sourcePath] = keySet;
            }

            foreach (var key in keys)
                keySet.Add(key);
        }

        private static IEnumerable<CodeInstruction> ReadStringTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(BinaryReader), "ReadString");
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Call, target);
            yield return new CodeInstruction(OpCodes.Ret);
        }

        private static void RegisterPendingSourcePathPrefix(string path)
        {
            if (!_splitMode)
                return;

            _pendingSourcePath = NormalizeSourceOutputRelativePath(path);
        }

        private static void RegisterCatalogStreamPrefix(Stream stream)
        {
            if (!_splitMode)
                return;

            _SourcePathContext.Push(_currentSourcePath);
            _currentSourcePath = _pendingSourcePath;
        }

        private static void RestoreCatalogStreamPostfix()
        {
            if (!_splitMode)
                return;

            _currentSourcePath = _SourcePathContext.Count > 0 ? _SourcePathContext.Pop() : null;
        }

        public void Handle(DumpOptions options)
        {
            HandleCore(options.GameDirectory, options.UnpackDirectory, options.OutputPath ?? "dic.txt", splitMode: false);
        }

        public void Handle(DumpSplitOptions options)
        {
            HandleCore(options.GameDirectory, options.UnpackDirectory, options.OutputDirectory ?? "dic-split", splitMode: true);
        }

        private static void HandleCore(string gameDirectory, string unpackDirectory, string outputPath, bool splitMode)
        {
            ResetState(unpackDirectory, splitMode);
            var assembly = Assembly.LoadFrom(Path.Combine(gameDirectory, "data", "common.dll"));
            assembly.GetType("Yukar.Common.Resource.ResourceItem").DeclaredField("sClipboardLoad").SetValue(null, true);
            assembly.GetType("Yukar.Common.Resource.ResourceItem").DeclaredField("sCurrentSourceMode")
                .SetValue(null, Enum.Parse(assembly.GetType("Yukar.Common.Resource.ResourceSource"), "RES_USER"));
            CatalogWrapper.CatalogType = assembly.GetType("Yukar.Common.Catalog");
            ScriptWrapper.ScriptType = assembly.GetType("Yukar.Common.Rom.Script");
            CatalogWrapper.sResourceDir = Path.Combine(unpackDirectory, "unpack.zip\\");
            var harmony = new Harmony("YUKAR.COMMON");
            harmony.Patch(
                Helper.GetMethod(assembly.GetType("Yukar.Common.BinaryReaderWrapper"), "ReadString"),
                transpiler: typeof(DumpHandler).GetDeclaredMethods().First(it => it.Name == "ReadStringTranspiler"),
                postfix: typeof(DumpHandler).GetDeclaredMethods().First(it => it.Name == "ReadStringPostfix"));
            harmony.Patch(
                Helper.GetMethod(assembly.GetType("Yukar.Common.Util"), "getFileStream", "System.String"),
                prefix: typeof(DumpHandler).GetDeclaredMethods().First(it => it.Name == "RegisterPendingSourcePathPrefix"));
            harmony.Patch(
                Helper.GetMethod(assembly.GetType("Yukar.Common.Catalog"), "load", "Yukar.Common.Catalog+FileType", "System.IO.Stream", "Yukar.Common.Catalog+OVERWRITE_RULES", "System.Boolean"),
                prefix: typeof(DumpHandler).GetDeclaredMethods().First(it => it.Name == "RegisterCatalogStreamPrefix"),
                postfix: typeof(DumpHandler).GetDeclaredMethods().First(it => it.Name == "RestoreCatalogStreamPostfix"));
            var catalog = CatalogWrapper.init();
            catalog.load();

            if (splitMode)
                WriteSplitDictionary(outputPath);
            else
                WriteDictionary(outputPath, _KeySet);
        }

        private static void ResetState(string unpackDirectory, bool splitMode)
        {
            _KeySet.Clear();
            _KeySetsBySource.Clear();
            _SourcePathContext.Clear();
            _pendingSourcePath = null;
            _currentSourcePath = null;
            _splitMode = splitMode;
            _splitSourceRoot = Path.GetFullPath(Path.Combine(unpackDirectory, "unpack.zip"));
            if (!_splitSourceRoot.EndsWith(Path.DirectorySeparatorChar.ToString()))
                _splitSourceRoot += Path.DirectorySeparatorChar;
        }

        private static void WriteSplitDictionary(string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            foreach (var entry in _KeySetsBySource.OrderBy(it => it.Key, StringComparer.OrdinalIgnoreCase))
            {
                var filePath = Path.Combine(outputDirectory, entry.Key);
                var directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                WriteDictionary(filePath, entry.Value);
            }
        }

        private static void WriteDictionary(string outputPath, IEnumerable<string> keys)
        {
            using (var sw = new StreamWriter(
                new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read),
                Encoding.UTF8, 4096, leaveOpen: false))
            {
                foreach (var key in keys)
                {
                    var encodedKey = key.Replace("\r\n", "\\n");
                    sw.WriteLine($"{encodedKey}\t{encodedKey}");
                }
            }
        }

        private static string GetSourceOutputRelativePath(object instance)
        {
            if (!string.IsNullOrEmpty(_currentSourcePath))
                return _currentSourcePath;

            if (!(instance is BinaryReader reader))
                return null;

            var filePath = TryGetStreamFilePath(reader.BaseStream, new HashSet<object>(ReferenceEqualityComparer.Instance));
            return NormalizeSourceOutputRelativePath(filePath);
        }

        private static string NormalizeSourceOutputRelativePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            var fullPath = Path.GetFullPath(filePath);
            if (!fullPath.StartsWith(_splitSourceRoot, StringComparison.OrdinalIgnoreCase))
                return null;

            var relativePath = fullPath.Substring(_splitSourceRoot.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.ChangeExtension(relativePath, ".txt");
        }

        private static string TryGetStreamFilePath(object streamObject, HashSet<object> visited)
        {
            if (streamObject == null || !visited.Add(streamObject))
                return null;

            if (streamObject is FileStream fileStream)
                return fileStream.Name;

            var streamType = streamObject.GetType();
            foreach (var propertyName in new[] { "BaseStream", "InnerStream" })
            {
                var property = streamType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property == null || property.GetIndexParameters().Length > 0)
                    continue;

                var innerStream = property.GetValue(streamObject, null);
                var filePath = TryGetStreamFilePath(innerStream, visited);
                if (!string.IsNullOrEmpty(filePath))
                    return filePath;
            }

            foreach (var fieldName in new[] { "_stream", "stream", "_baseStream", "baseStream", "innerStream", "_innerStream" })
            {
                var field = streamType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                    continue;

                var innerStream = field.GetValue(streamObject);
                var filePath = TryGetStreamFilePath(innerStream, visited);
                if (!string.IsNullOrEmpty(filePath))
                    return filePath;
            }

            return null;
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
