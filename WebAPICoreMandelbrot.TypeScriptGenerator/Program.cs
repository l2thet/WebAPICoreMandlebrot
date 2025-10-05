namespace WebAPICoreMandelbrot.TypeScriptGenerator;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("TypeScript Generator - Converting C# Response Classes to TypeScript Interfaces");
        
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: TypeScriptGenerator <output-file-path>");
            Environment.Exit(1);
        }

        string outputPath = args[0];
        
        try
        {
            var generator = new TypeScriptGenerator();
            string generatedCode = generator.GenerateTypeScriptInterfaces();
            
            await File.WriteAllTextAsync(outputPath, generatedCode);
            Console.WriteLine($"Successfully generated TypeScript interfaces at: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating TypeScript interfaces: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
