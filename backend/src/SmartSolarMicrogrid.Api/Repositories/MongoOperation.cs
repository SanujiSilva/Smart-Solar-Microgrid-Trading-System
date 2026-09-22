/*
 * File: src/SmartSolarMicrogrid.Api/Repositories/MongoOperation.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: MongoDB persistence and query operations for Mongo Operation.
 */
using System.Linq.Expressions;
using MongoDB.Driver;

namespace SmartSolarMicrogrid.Api.Repositories;

// One request-scoped session shared by the station, slot and reservation repositories.
public sealed class MongoOperation
{
    public IClientSessionHandle? Session { get; set; }
}

public static class MongoOperationExtensions
{
    // Query for Mongo Operation.
    public static IFindFluent<T, T> Query<T>(this IMongoCollection<T> collection, MongoOperation operation,
        Expression<Func<T, bool>> filter) => collection.Query(operation, new ExpressionFilterDefinition<T>(filter));

    // Query for Mongo Operation.
    public static IFindFluent<T, T> Query<T>(this IMongoCollection<T> collection, MongoOperation operation,
        FilterDefinition<T> filter) => operation.Session is { } session
        ? collection.Find(session, filter) : collection.Find(filter);

    // Count for Mongo Operation.
    public static Task<long> Count<T>(this IMongoCollection<T> collection, MongoOperation operation,
        FilterDefinition<T> filter, CancellationToken cancellationToken) => operation.Session is { } session
        ? collection.CountDocumentsAsync(session, filter, cancellationToken: cancellationToken)
        : collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

    // Insert for Mongo Operation.
    public static Task Insert<T>(this IMongoCollection<T> collection, MongoOperation operation,
        T document, CancellationToken cancellationToken) => operation.Session is { } session
        ? collection.InsertOneAsync(session, document, cancellationToken: cancellationToken)
        : collection.InsertOneAsync(document, cancellationToken: cancellationToken);

    // Change for Mongo Operation.
    public static Task<T> Change<T>(this IMongoCollection<T> collection, MongoOperation operation,
        Expression<Func<T, bool>> filter, UpdateDefinition<T> update, FindOneAndUpdateOptions<T> options,
        CancellationToken cancellationToken) => collection.Change(operation,
            new ExpressionFilterDefinition<T>(filter), update, options, cancellationToken);

    // Change for Mongo Operation.
    public static Task<T> Change<T>(this IMongoCollection<T> collection, MongoOperation operation,
        FilterDefinition<T> filter, UpdateDefinition<T> update, FindOneAndUpdateOptions<T> options,
        CancellationToken cancellationToken) => operation.Session is { } session
        ? collection.FindOneAndUpdateAsync(session, filter, update, options, cancellationToken)
        : collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
}
