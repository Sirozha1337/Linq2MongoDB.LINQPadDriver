using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using LINQPad;
using LINQPad.Extensibility.DataContext;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Linq2MongoDB.LINQPadDriver
{
    public sealed class MongoDriver : DynamicDataContextDriver
    {
        static MongoDriver()
        {
            // Uncomment the following code to attach to Visual Studio's debugger when an exception is thrown.
#if DEBUG
            AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
            {
                if (args.Exception!.StackTrace!.Contains(typeof(MongoDriver)!.Namespace!))
                {
                    Debugger.Launch();
                }
            };
#endif
        }

        public override string Name => "MongoDB Driver " + Version;
        public override string Author => "Sirozha1337";
        public override Version Version => typeof(MongoDriver).Assembly.GetName().Version;

        public override bool AreRepositoriesEquivalent(IConnectionInfo c1, IConnectionInfo c2)
            => c1.DatabaseInfo.CustomCxString == c2.DatabaseInfo.CustomCxString
            && c1.DatabaseInfo.Database == c2.DatabaseInfo.Database;

        public override IEnumerable<string> GetAssembliesToAdd(IConnectionInfo cxInfo)
        {
            return new[] { "*" };
        }

        public override IEnumerable<string> GetNamespacesToAdd(IConnectionInfo cxInfo)
        {
            return new[]
            {
                "MongoDB.Driver",
                "MongoDB.Bson",
            };
        }

        private static readonly HashSet<string> ExcludedCommand = new HashSet<string>
        {
            "isMaster",
            "buildInfo",
            "saslStart",
            "saslContinue",
            "getLastError",
        };

        private static readonly HashSet<string> SchemaRefreshCommands = new HashSet<string>
        {
            "create",
            "drop",
        };

        private static MongoClientSettings GetMongoClientSettings(IConnectionInfo cxInfo)
            => MongoClientSettings.FromUrl(new MongoUrl(cxInfo.DatabaseInfo.CustomCxString));

        private IMongoDatabase GetDatabase(IConnectionInfo cxInfo) => GetDatabase(cxInfo, GetMongoClientSettings(cxInfo));

        private IMongoDatabase GetDatabase(IConnectionInfo cxInfo, MongoClientSettings mongoClientSettings)
        {
            var client = new MongoClient(mongoClientSettings);
            return client.GetDatabase(cxInfo.DatabaseInfo.Database);
        }

        public override void InitializeContext(IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager)
        {
            #if DEBUG
            Debugger.Launch();
            #endif

            var mongoClientSettings = GetMongoClientSettings(cxInfo);
            mongoClientSettings.ClusterConfigurator = cb => cb
                .Subscribe<CommandStartedEvent>(e =>
                {
                    if (!ExcludedCommand.Contains(e.CommandName))
                    {
                        executionManager.SqlTranslationWriter.WriteLine(e.Command.ToJson(new JsonWriterSettings { Indent = true }));
                    }
                })
                .Subscribe<CommandSucceededEvent>(e =>
                {
                    if (!ExcludedCommand.Contains(e.CommandName))
                    {
                        executionManager.SqlTranslationWriter.WriteLine($"\t Duration = {e.Duration} \n");
                    }

                    if (SchemaRefreshCommands.Contains(e.CommandName))
                    {
                        cxInfo.ForceRefresh();
                    }
                });
            var mongoDatabase = GetDatabase(cxInfo, mongoClientSettings);

            context.GetType().GetMethod("Initial", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(context, new object[] { mongoDatabase });
        }

        public override string GetConnectionDescription(IConnectionInfo cxInfo)
            => !string.IsNullOrWhiteSpace(cxInfo.DisplayName) ? cxInfo.DisplayName : GetDefaultConnectionName(cxInfo);

        private static string GetDefaultConnectionName(IConnectionInfo cxInfo)
            => $"MongoDB - {GetMongoClientSettings(cxInfo).Server.Host} - {cxInfo.DatabaseInfo.Database}";

        public override bool ShowConnectionDialog(IConnectionInfo cxInfo, ConnectionDialogOptions dialogOptions)
            => new ConnectionDialog(cxInfo).ShowDialog() == true;

        public override List<ExplorerItem> GetSchemaAndBuildAssembly(
            IConnectionInfo cxInfo, AssemblyName assemblyToBuild, ref string nameSpace, ref string typeName)
        {
            #if DEBUG
            Debugger.Launch();
            #endif

            var mongoDatabase = GetDatabase(cxInfo);
            var collections = new List<CollectionItem>();
            int invalidCounter = 0;
            int duplicateCounter = 0;
            foreach (var collectionName in mongoDatabase.ListCollectionNames().ToList())
            {
                var collectionPropertyName = StringSanitizer.SanitizeCollectionName(collectionName) ?? $"InvalidName{++invalidCounter}";

                if (collections.Any(c => c.CollectionPropertyName == collectionPropertyName))
                {
                    collectionPropertyName = $"DuplicateName{++duplicateCounter}";
                }

                collections.Add(new CollectionItem(collectionName, collectionPropertyName));
            }
            mongoDatabase.Client.Cluster.Dispose();
            mongoDatabase.Client.Dispose();

            var source = DataContextSourceGenerator.GenerateSource(nameSpace, typeName, collections);
            Compile(cxInfo, source, assemblyToBuild.CodeBase, new[]{
                    typeof(IMongoDatabase).Assembly.Location,
                    typeof(BsonDocument).Assembly.Location,
                    typeof(QueryableMongoCollection).Assembly.Location
            });

            // We need to tell LINQPad what to display in the TreeView on the left (Schema Explorer):
            var schemas = collections.Select(a =>
                new ExplorerItem(a.DisplayName(), ExplorerItemKind.QueryableObject, ExplorerIcon.Table)
                {
                    IsEnumerable = true,
                    DragText = a.CollectionPropertyName,
                });

            return schemas.ToList();
        }

        private static void Compile(IConnectionInfo cxInfo, string cSharpSourceCode, string outputFile, IEnumerable<string> customTypeAssemblyPath)
        {
            // GetCoreFxReferenceAssemblies is helper method that returns the full set of .NET Core reference assemblies.
            // (There are more than 100 of them.)
            var assembliesToReference = GetCoreFxReferenceAssemblies(cxInfo).Concat(customTypeAssemblyPath).ToArray();

            // CompileSource is a static helper method to compile C# source code using LINQPad's built-in Roslyn libraries.
            // If you prefer, you can add a NuGet reference to the Roslyn libraries and use them directly.
            var compileResult = CompileSource(new CompilationInput
            {
                FilePathsToReference = assembliesToReference,
                OutputPath = outputFile,
                SourceCode = new[] { cSharpSourceCode }
            });

            if (compileResult.Errors.Length > 0)
            {
                throw new Exception("Cannot compile typed context: " + compileResult.Errors[0]);
            }
        }

        public override void PreprocessObjectToWrite(ref object objectToWrite, ObjectGraphInfo info)
        {
            if (objectToWrite is BsonDocument document)
            {
                objectToWrite = BsonTypeMapper.MapToDotNetValue(document);
            }

            if (objectToWrite is BsonArray array)
            {
                objectToWrite = BsonTypeMapper.MapToDotNetValue(array);
            }
        }
    }
}
