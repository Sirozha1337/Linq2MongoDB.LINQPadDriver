using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Linq2MongoDB.LINQPadDriver
{
    public class QueryableMongoCollection : IQueryable<BsonDocument>
    {
        private readonly IMongoCollection<BsonDocument> _mongoCollection;
        private readonly Expression _expression;
        private readonly IQueryProvider _provider;

        public QueryableMongoCollection(IMongoCollection<BsonDocument> mongoCollection)
        {
            _mongoCollection = mongoCollection;
            var queryable = mongoCollection.AsQueryable();
            _provider = queryable.Provider;
            _expression = queryable.Expression;
        }

        public Type ElementType => typeof(BsonDocument);

        public Expression Expression => _expression;

        public IQueryProvider Provider => _provider;

        public IEnumerator<BsonDocument> GetEnumerator() => ((IEnumerable<BsonDocument>)_provider.Execute(_expression))!.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_provider.Execute(_expression))!.GetEnumerator();

        public IMongoCollection<BsonDocument> Collection => _mongoCollection;

        public ReplaceOneResult ReplaceOne(BsonDocument value)
        {
            if (value["_id"] == null)
            {
                throw new Exception("ReplaceOne requires the document to have an _id field. Use .Collection to run queries directly on MongoCollection.");
            }

            return _mongoCollection.ReplaceOne(Builders<BsonDocument>.Filter.Eq(o => o["_id"], value["_id"]), value);
        }

        public ReplaceOneResult ReplaceOne(Expression<Func<BsonDocument, bool>> filter, BsonDocument value)
            => _mongoCollection.ReplaceOne(Builders<BsonDocument>.Filter.Where(filter), value);

        public DeleteResult DeleteOne(Expression<Func<BsonDocument, bool>> filter)
            => _mongoCollection.DeleteOne(Builders<BsonDocument>.Filter.Where(filter));

        public DeleteResult DeleteMany(Expression<Func<BsonDocument, bool>> filter)
            => _mongoCollection.DeleteMany(Builders<BsonDocument>.Filter.Where(filter));

        public void InsertOne(BsonDocument value)
            => _mongoCollection.InsertOne(value);

        public void InsertOne(object value)
            => _mongoCollection.InsertOne(value.ToBsonDocument());

        public void InsertMany(IEnumerable<BsonDocument> value)
            => _mongoCollection.InsertMany(value);

        public void InsertMany(IEnumerable<object> value)
            => _mongoCollection.InsertMany(value.Select(v => v.ToBsonDocument()));

        public long Count(Expression<Func<BsonDocument, bool>> filter)
            => _mongoCollection.CountDocuments(filter);

        public UpdateResult UpdateMany(Expression<Func<BsonDocument, bool>> filter, Func<BsonDocument, BsonDocument> updateDefinition)
        {
            var matchedCount = 0L;
            var modifiedCount = 0L;
            var upsertedId = new List<BsonValue>();

            var objectsToUpdate = this.Where(filter).ToList();
            if (objectsToUpdate.Any(o => o["_id"] == null))
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
                upsertedId.Add(objectToUpdate["_id"]);
            }

            return new UpdateManyResult(matchedCount, modifiedCount, upsertedId);
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
