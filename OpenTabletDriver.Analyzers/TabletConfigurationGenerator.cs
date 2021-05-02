using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver.Analyzers
{
    [Generator]
    public class TabletConfigurationGenerator : ISourceGenerator
    {
        private const string CLASS_NAME = "CompiledTabletConfig";
        private const int YIELD_INDENT = 3;
        private const string HEADER = @"using System;
using System.Collections.Generic;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver
{
    public static class CompiledTabletConfig
    {
        public static IEnumerable<TabletConfiguration> GetCompiledConfigs()
        {
";

        private const string FOOTER = @"        }
    }
}
";

        public void Execute(GeneratorExecutionContext context)
        {
            var serializer = new JsonSerializer();

            var configs = GetConfigurations(context);

            var configurationSourceCodes = configs.Select(file => GenerateInitializerFromFile(file, serializer));
            var classSourceCode = GenerateCompiledTabletConfigClass(configurationSourceCodes);

            context.AddSource($"{CLASS_NAME}.cs", classSourceCode);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            return;
        }

        private static string GenerateCompiledTabletConfigClass(IEnumerable<string> tabletConfigurations)
        {
            var sourceCode = new StringBuilder();
            sourceCode.Append(HEADER);

            CodeRepresentation.IndentLevel = YIELD_INDENT;

            foreach (var config in tabletConfigurations)
            {
                sourceCode.AppendLine(CodeRepresentation.Indent($"yield return {config};"));
            }

            sourceCode.Append(FOOTER);
            return sourceCode.ToString();
        }

        private static IEnumerable<FileInfo> GetConfigurations(GeneratorExecutionContext context)
        {
            foreach (AdditionalText file in context.AdditionalFiles)
            {
                var info = new DirectoryInfo(file.Path);
                foreach (var tabletConfig in info.EnumerateFiles("*.json", SearchOption.AllDirectories))
                    yield return tabletConfig;
            }
        }

        private static string GenerateInitializerFromFile(FileInfo file, JsonSerializer serializer)
        {
            using (var fs = file.OpenRead())
            using (var sr = new StreamReader(fs))
            using (var jr = new JsonTextReader(sr))
            {
                return GenerateInitializerFromConfig(serializer.Deserialize<TabletConfiguration>(jr));
            }
        }

        private static string GenerateInitializerFromConfig(TabletConfiguration config)
        {
            CodeRepresentation.IndentLevel = YIELD_INDENT;
            return CodeRepresentation.Create(config);
        }
    }
}
