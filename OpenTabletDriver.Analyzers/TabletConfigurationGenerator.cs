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
        private const string CLASS_NAME = "CompiledTabletConfigurations";
        private const int YIELD_INDENT = 3;
        private static readonly string HEADER =
@"using System;
using System.Collections.Generic;
using OpenTabletDriver.Plugin.Tablet;

namespace OpenTabletDriver
{
    public static class " + CLASS_NAME + @"
    {
        public static IEnumerable<TabletConfiguration> GetConfigurations()
        {
";

        private const string FOOTER =
@"        }
    }
}
";

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var serializer = new JsonSerializer();

            var configs = GetConfigurations(context);

            var configurationSourceCodes = configs.Select(file => GenerateInitializerFromFile(file, serializer));
            var classSourceCode = GenerateCompiledTabletConfigClass(configurationSourceCodes);

            File.WriteAllText($"C:\\OTD\\{CLASS_NAME}.cs", classSourceCode);

            context.AddSource($"{CLASS_NAME}.cs", classSourceCode);
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
            foreach (AdditionalText file in context.AdditionalFiles.Where(f => Path.GetExtension(f.Path) == ".json"))
            {
                if (context.AnalyzerConfigOptions.GetOptions(file).TryGetValue("build_metadata.AdditionalFiles.TabletConfiguration", out var isConfigString)
                    && bool.TryParse(isConfigString, out bool isConfig)
                    && isConfig
                )
                {
                    if (File.Exists(file.Path))
                        yield return new FileInfo(file.Path);
                }
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
