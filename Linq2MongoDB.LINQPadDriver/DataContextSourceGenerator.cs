using System;
using System.Collections.Generic;
using System.Linq;

namespace Linq2MongoDB.LINQPadDriver
{
    public static class DataContextSourceGenerator
    {
        /// <summary>
        /// {0} - namespace
        /// {1} - class name
        /// {2} - collection initialization code
        /// {3} - collection properties
        /// </summary>
        private const string SourceCodeTemplate = @"
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using MongoDB.Bson;
        using MongoDB.Driver;
        using Linq2MongoDB.LINQPadDriver;

        namespace {0} {{
	        public class {1} : IDisposable
	        {{
                public IMongoDatabase Database => _db;
                private IMongoDatabase _db;

                private Lazy<QueryableMongoCollection> InitCollection(string collectionName)
                    => new Lazy<QueryableMongoCollection>(()=> new QueryableMongoCollection(_db.GetCollection<BsonDocument>(collectionName)));

                public {1}()
                {{
                    {2}
                }}

                internal void Initial(IMongoDatabase db)
                {{
                    _db = db;
                }}

                {3}

                public void Dispose()
                {{
                    if (_db != null)
                    {{
                        _db.Client.Cluster.Dispose();
                        _db.Client.Dispose();
                        _db = null;
                    }}
                }}
            }}
        }}";

        /// <summary>
        /// {0} - property name
        /// {1} - collection name
        /// </summary>
        private const string SourceCodeInitCollectionTemplate = "_{0} = InitCollection(\"{1}\");";

        /// <summary>
        /// {0} - property name
        /// </summary>
        private const string SourceCodeCollectionPropertyTemplate = @"
                private readonly Lazy<QueryableMongoCollection> _{0};
                public QueryableMongoCollection {0} => _{0}.Value;";


        public static string GenerateSource(string nameSpace, string typeName, ICollection<CollectionItem> collectionNames)
        {
            var collectionInitSource = string.Join("\n", collectionNames.Select(c => string.Format(SourceCodeInitCollectionTemplate, c.CollectionPropertyName, c.MongoCollectionName)));
            var collectionPropertiesSource =  string.Join("\n", collectionNames.Select(c => string.Format(SourceCodeCollectionPropertyTemplate, c.CollectionPropertyName)));
            var source = string.Format(SourceCodeTemplate, nameSpace, typeName, collectionInitSource, collectionPropertiesSource);

            return source;
        }
    }
}
