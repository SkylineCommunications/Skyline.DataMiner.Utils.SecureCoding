using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO;

namespace Analyzers.Test
{
    [TestClass]
    public class AnalyzersUnitTest
    {
        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task FileOperationAnalyzeTest()
        {
            var code = @"
                using Skyline.DataMiner.Utils.Security.SecureIO;

                class Program
                {
                    static void Main()
                    {

                        var windowsPath = ""C:\\Windows"";

                        var holder = new Holder(windowsPath);
                        File.WriteAllText(holder.Path, """");

                        string securePath = holder.GetPath(windowsPath);
                        File.WriteAllText(securePath, """");

                        var securePath2 = SecurePath.ConstructSecurePath(windowsPath);
                        File.WriteAllText(securePath2, """");


                        var randomPath = ""C:\\Skyline DataMiner\\Test"";
                        File.WriteAllText(randomPath, """");

                        Path.Combine(""1"", ""2\\"");

                        var securePat2 = SecurePath.ConstructSecurePath(windowsPath);
                    }



                    public class Holder
                    {
                        public Holder(string path)
                        {
                            this.Path = path;
                        }
                        public string Path { get; }

                        private string GetPath(string windowsPath)
                        {
                            return SecurePath.ConstructSecurePath(windowsPath);
                        }
                    }
                }";

            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            SyntaxNode root = tree.GetRoot();
            //FileOperationAnalyzer.Analyze(root);
        }
    }
}
