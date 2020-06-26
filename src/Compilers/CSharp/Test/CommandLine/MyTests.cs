using System.Globalization;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.SharedResourceHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    public class MyTests : CommandLineTestBase
    {
        [Fact]
        public void CompilationTest()
        {
            string source = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"
using Test;

public class C
{
    string _testField;
}
").Path;

            var baseDir = Path.GetDirectoryName(source);
            var fileName = Path.GetFileName(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, baseDir,
                    new[]
                    {
                        "/nologo", "/preferreduilang:en", "/reference:\"c:\\test\\test.dll\"", "/target:library", source.ToString()
                    })
                .Run(outWriter);

            CleanupAllGeneratedFiles(source);
            Assert.Equal(0, exitCode);
        }
    }
}
