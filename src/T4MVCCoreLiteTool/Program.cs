﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace T4MVCCoreLiteTool
{
    class Program
    {
        private static string ProjectPath;
        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
                Console.WriteLine($"Arg {i}: {args[i]}");

            if (args.Length != 1)
            {
                Console.WriteLine("Expecting project path as parameter");
                return;
            }

            ProjectPath = args[0];
            if (!File.Exists(ProjectPath))
            {
                Console.WriteLine("Invalid project path");
                return;
            }

            new Program().Run().Wait();
        }

        public async Task Run()
        {
            var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(ProjectPath);
            foreach (var diag in workspace.Diagnostics)
                Console.WriteLine($"  {diag.Kind}: {diag.Message}");

            var compilation = await project.GetCompilationAsync() as CSharpCompilation;
            foreach (var diag in compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
                Console.WriteLine($"  {diag.Severity}: {diag.GetMessage()}");

            var generator = new T4MVCGenerator(
                new Services.ControllerRewriterService(),
                new Services.ControllerGeneratorService(new Services.ViewLocatorService(new[] { new Locators.DefaultRazorViewLocator() })),
                new Services.StaticFileGeneratorService(new[] { new Locators.DefaultStaticFileLocator() }));

            var node = generator.Generate(compilation, new Services.Settings(""));
            Extensions.SyntaxNodeHelpers.WriteFile(node, Path.Combine(Path.GetDirectoryName(project.FilePath), T4MVCGenerator.R4MvcFileName));
        }
    }
}
