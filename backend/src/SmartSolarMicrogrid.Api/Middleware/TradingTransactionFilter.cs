/*
 * File: src/SmartSolarMicrogrid.Api/Middleware/TradingTransactionFilter.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Request-pipeline behavior for Trading Transaction Filter.
 */
using Microsoft.AspNetCore.Mvc.Filters;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Controllers;
using SmartSolarMicrogrid.Api.Helpers;
using SmartSolarMicrogrid.Api.Repositories;

namespace SmartSolarMicrogrid.Api.Middleware;

public sealed class TradingTransactionFilter(MongoDbContext database, MongoOperation operation) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Serialize trading mutations in a MongoDB transaction to prevent conflicting writes.
        var request = context.HttpContext.Request;
        var trading = context.Controller is StationsController or SlotsController or ReservationsController or QrController;
        var writes = !HttpMethods.IsGet(request.Method) || request.Path.Value!.EndsWith("/qr", StringComparison.Ordinal);
        if (!trading || !writes) { await next(); return; }

        using var session = await database.Database.Client.StartSessionAsync(cancellationToken: request.HttpContext.RequestAborted);
        operation.Session = session;
        session.StartTransaction(new TransactionOptions(readConcern: ReadConcern.Snapshot, writeConcern: WriteConcern.WMajority));
        try
        {
            // A shared write prevents snapshot write skew between station/slot changes and bookings,
            // including requests served by separate API instances. Contenders receive a retryable 409.
            await database.Database.GetCollection<BsonDocument>("TradingMutationGuard").UpdateOneAsync(session,
                new BsonDocument("_id", "trading"), new BsonDocument("$inc", new BsonDocument("revision", 1)),
                new UpdateOptions { IsUpsert = true }, request.HttpContext.RequestAborted);
            var result = await next();
            if (result.Exception is MongoException mongo && mongo.HasErrorLabel("TransientTransactionError"))
            {
                result.ExceptionHandled = true;
                throw new ApiException(409, "Another trading operation is in progress. Reload and retry.");
            }
            if (result.Exception is not null || result.Canceled) return;
            await session.CommitTransactionAsync(request.HttpContext.RequestAborted);
        }
        catch (MongoException error) when (error.HasErrorLabel("TransientTransactionError"))
        { throw new ApiException(409, "Another trading operation is in progress. Reload and retry."); }
        catch (MongoCommandException error) when (error.Code == 20)
        { throw new ApiException(503, "Trading writes require a MongoDB replica set. Configure a replica set or Atlas."); }
        finally
        {
            operation.Session = null;
            if (session.IsInTransaction) await session.AbortTransactionAsync(CancellationToken.None);
        }
    }
}
