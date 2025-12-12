using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Linq2MongoDB.LINQPadDriver
{
    public class QueryableMongoCollection : QueryableMongoCollection<BsonDocument>
    {
        public QueryableMongoCollection(IMongoCollection<BsonDocument> collection) : base(collection)
        {
        }
    }

    public class QueryableMongoCollection<T> : IQueryable<T> where T : class
    {
        private readonly IMongoCollection<T> _mongoCollection;
        private readonly Expression _expression;
        private readonly IQueryProvider _provider;

        public QueryableMongoCollection(IMongoCollection<T> mongoCollection)
        {
            _mongoCollection = mongoCollection;
            var queryable = mongoCollection.AsQueryable();
            _provider = queryable.Provider;
            _expression = queryable.Expression;
        }

        public Type ElementType => typeof(T);

        public Expression Expression => _expression;

        public IQueryProvider Provider => _provider;

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_provider.Execute(_expression))!.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_provider.Execute(_expression))!.GetEnumerator();

        public IMongoCollection<T> Collection => _mongoCollection;

        public QueryableMongoCollection<TNew> As<TNew>() where TNew : class
        {
            var newCollection = _mongoCollection.Database.GetCollection<TNew>(_mongoCollection.CollectionNamespace.CollectionName);
            return new QueryableMongoCollection<TNew>(newCollection);
        }

        public ReplaceOneResult ReplaceOne(T value)
        {
            var bsonDoc = GetBsonDoc(value);
            if ( bsonDoc["_id"] == null)
            {
                throw new Exception("ReplaceOne requires the document to have an _id field. Use .Collection to run queries directly on MongoCollection.");
            }

            return _mongoCollection.ReplaceOne(Builders<T>.Filter.Eq("_id", bsonDoc["_id"]), value);
        }

        public ReplaceOneResult ReplaceOne(Expression<Func<T, bool>> filter, T value)
            => _mongoCollection.ReplaceOne(Builders<T>.Filter.Where(filter), value);

        public DeleteResult DeleteOne(Expression<Func<T, bool>> filter)
            => _mongoCollection.DeleteOne(Builders<T>.Filter.Where(filter));

        public DeleteResult DeleteMany(Expression<Func<T, bool>> filter)
            => _mongoCollection.DeleteMany(Builders<T>.Filter.Where(filter));

        public void InsertOne(T value)
            => _mongoCollection.InsertOne(value);

        public void InsertOne(object value)
            => _mongoCollection.InsertOne(GetAsTargetType(value));

        public void InsertMany(IEnumerable<T> value)
            => _mongoCollection.InsertMany(value);

        public void InsertMany(IEnumerable<object> value)
            => _mongoCollection.InsertMany(value.Select(GetAsTargetType));

        public long Count(Expression<Func<T, bool>> filter)
            => _mongoCollection.CountDocuments(filter);

        public UpdateResult UpdateMany(Expression<Func<T, bool>> filter, Func<T, T> updateDefinition)
        {
            var matchedCount = 0L;
            var modifiedCount = 0L;
            var upsertedId = new List<BsonValue>();

            var objectsToUpdate = this.Where(filter).ToList();
            if (objectsToUpdate.Any(o => GetBsonDoc(o)["_id"] == null))
            {
                throw new Exception("UpdateMany requires all documents to have an _id field. Use .Collection to run queries directly on MongoCollection.");
            }

            foreach (var objectToUpdate in objectsToUpdate)
            {
                var updated = updateDefinition(objectToUpdate);
                var result = ReplaceOne(updated);

                if (!result.IsAcknowledged)
                {
                    return new UpdateManyResult(matchedCount, modifiedCount, upsertedId, false);
                }

                matchedCount += result.MatchedCount;
                modifiedCount += result.ModifiedCount;
                upsertedId.Add(GetBsonDoc(objectToUpdate)["_id"]);
            }

            return new UpdateManyResult(matchedCount, modifiedCount, upsertedId);
        }

        private BsonDocument GetBsonDoc(object value) =>
            value switch
            {
                BsonDocument bsonDoc => bsonDoc,
                _ => value.ToBsonDocument(),
            };

        private T GetAsTargetType(object value)
        {
            if (typeof(T) == typeof(BsonDocument))
            {
                return GetBsonDoc(value) as T;
            }

            if (value is T vt)
            {
                return vt;
            }

            throw new NotSupportedException(
                $"The type {typeof(T).FullName} is not supported. Use .Collection to run queries directly on MongoCollection.");
        }

    }

    public class UpdateManyResult : UpdateResult
    {
        public UpdateManyResult(long matchedCount, long modifiedCount, IEnumerable<BsonValue> upsertedIds, bool isAcknowledged = true)
        {
            MatchedCount = matchedCount;
            ModifiedCount = modifiedCount;
            UpsertedId = new BsonArray(upsertedIds);
            IsAcknowledged = isAcknowledged;
        }

        public override bool IsAcknowledged { get; }
        public override bool IsModifiedCountAvailable => true;
        public override long MatchedCount { get; }
        public override long ModifiedCount { get; }
        public override BsonValue UpsertedId { get; }
    }
}
