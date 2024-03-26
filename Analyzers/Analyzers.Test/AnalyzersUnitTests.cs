using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Skyline.DataMiner.Utils.SecureCoding.Analyzers.SecureIO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
                    static void Main(string arg1, string arg2, int arg3, string argtest, Holder holder)
                    {

                        var windowsPath = ""C:\\Windows"";

                        holder._path = SecurePath.ConstructSecurePath();
                        File.WriteAllText(holder.Path, """");

                        string securePath = holder.GetPath(windowsPath);                        
                        securePath = ""C:\\Windows"";                        
                        securePath = ""C:\\Windows"";
                        securePath = ""C:\\Windows"";
                        securePath = ""C:\\Windows"";

                        argtest = ""TESTING"";
                        File.WriteAllText(argtest, """");

                        var securePath2 = SecurePath.ConstructSecurePath(windowsPath);
                        File.WriteAllText(securePath2, """");


                        var testPath = ""C:\\Skyline DataMiner\\Test"";
                        File.WriteAllText(testPath, """");

                        var randomPath = ""C:\\Skyline DataMiner\\Test"";
                        File.WriteAllText(randomPath, """");                        

                        File.WriteAllText(arg2, arg3);


            


                        Path.Combine(""1"", ""2\\"");

                        var securePat2 = SecurePath.ConstructSecurePath(windowsPath);
                    }



                    public class Holder
                    {
                        public string _path;

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

            var tree = CSharpSyntaxTree.ParseText(code);

            var root = tree.GetRoot().DescendantNodes(descendIntoChildren: node => node.ChildNodes().Any()).OfType<ArgumentSyntax>();

            var compilation = CSharpCompilation.Create("MyCompilation", syntaxTrees: new[] { tree });

            var model = compilation.GetSemanticModel(tree);

            var control = model.AnalyzeControlFlow(tree.GetRoot().DescendantNodesAndSelf().OfType<BlockSyntax>().First());


            var analysis = model.AnalyzeDataFlow(tree.GetRoot().DescendantNodesAndSelf().OfType<BlockSyntax>().First());


        }
    }
}
