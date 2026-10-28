using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using APIHandler.Constants;
using APIHandler.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Repositories.GuestSessions;

public class GuestSessionRepository(IDynamoDBContext context) : IGuestSessionRepository
{
    public async Task<GuestSession?> GetByGuestSessionId(string id) => await context.LoadAsync<GuestSession?>(id, new LoadConfig()
    {
        OverrideTableName = ResourceNames.GuestSessionTableName
    });

    public async Task<GuestSession?> GetByGroupReservationId(string groupReservationId)
    {
        List<GuestSession> result = await context.QueryAsync<GuestSession>(groupReservationId, new QueryConfig
        {
            OverrideTableName = ResourceNames.GuestSessionTableName,
            IndexName = GlobalSecondaryIndexes.GuestSession.GroupReservationId
        }).GetRemainingAsync();

        return result.FirstOrDefault();
    }

    public async Task<GuestSession?> GetByTokenHash(string tokenHash)
    {
        List<GuestSession> result = await context.QueryAsync<GuestSession>(tokenHash, new QueryConfig
        {
            OverrideTableName = ResourceNames.GuestSessionTableName,
            IndexName = GlobalSecondaryIndexes.GuestSession.SessionTokenHash
        }).GetRemainingAsync();

        return result.FirstOrDefault();
    }

    public async Task<List<GuestSession>> GetAllByGroupReservationIdAsync(string groupReservationId)
    {
        return await context.QueryAsync<GuestSession>(groupReservationId, new QueryConfig
        {
            OverrideTableName = ResourceNames.GuestSessionTableName,
            IndexName = GlobalSecondaryIndexes.GuestSession.GroupReservationId
        }).GetRemainingAsync();
    }

    public async Task SaveAsync(GuestSession guestSession) => await context.SaveAsync(guestSession, new SaveConfig
    {
        OverrideTableName = ResourceNames.GuestSessionTableName
    });

    public async Task<bool> TryCreateAsync(GuestSession guestSession)
    {
        // Build "SET attr = :v, ..." from the serialized document so the put is conditional on the item not existing.
        var document = context.ToDocument(guestSession);
        var names = new Dictionary<string, string>();
        var values = new Dictionary<string, DynamoDBEntry>();
        var assignments = new List<string>();
        int i = 0;
        foreach (var attribute in document)
        {
            if (attribute.Key == "guestSessionId" || attribute.Value is null || attribute.Value is DynamoDBNull)
                continue;
            names[$"#a{i}"] = attribute.Key;
            values[$":v{i}"] = attribute.Value;
            assignments.Add($"#a{i} = :v{i}");
            i++;
        }

        var write = context.CreateTransactWrite<GuestSession>(new TransactWriteConfig { OverrideTableName = ResourceNames.GuestSessionTableName });
        write.AddSaveItem(
            guestSession.Id,
            new Expression
            {
                ExpressionStatement = "SET " + string.Join(", ", assignments),
                ExpressionAttributeNames = names,
                ExpressionAttributeValues = values
            },
            new Expression { ExpressionStatement = "attribute_not_exists(guestSessionId)" });

        try
        {
            await write.ExecuteAsync();
            return true;
        }
        catch (TransactionCanceledException ex) when (ex.CancellationReasons?.Any(r => r.Code == "ConditionalCheckFailed") == true)
        {
            return false;
        }
    }

    public async Task<int> CountByGroupReservationId(string groupReservationId)
    {
        List<GuestSession> result = await context.QueryAsync<GuestSession>(groupReservationId, new QueryConfig
        {
            OverrideTableName = ResourceNames.GuestSessionTableName,
            IndexName = GlobalSecondaryIndexes.GuestSession.GroupReservationId
        }).GetRemainingAsync();

        // The owner is accounted for separately by callers (+1), so owner sessions are not counted as joined guests.
        return result.Count(s => !s.IsOwnerSession);
    }

    public async Task DeleteAsync(GuestSession session)
    {
        await context.DeleteAsync(session, DynamoConfig.DeleteConfig(ResourceNames.GuestSessionTableName));
    }
}
