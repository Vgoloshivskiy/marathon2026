using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using APIHandler.Constants;
using APIHandler.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Repositories.Orders;

public class OrderRepository : IOrderRepository
{
    private readonly string OrdersTableName = ResourceNames.OrdersTableName;
    private readonly string CustomerIdGlobalIndex = ResourceNames.OrdersTableCustomerIdGlobalIndex;
    private readonly string ReservationIdGlobalIndex = ResourceNames.OrdersTableReservationIdGlobalIndex;

    private readonly IDynamoDBContext _dynamoDbContext;

    public OrderRepository(IDynamoDBContext dynamoDbContext)
    {
        _dynamoDbContext = dynamoDbContext ?? throw new ArgumentNullException(nameof(dynamoDbContext));
    }

    public async Task<Order?> GetOrderByIdAsync(string orderId, string customerId)
    {
        return await _dynamoDbContext.LoadAsync<Order>(orderId, customerId, DynamoConfig.LoadConfig(OrdersTableName));
    }

    public async Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(string customerId)
    {
        var config = new QueryConfig
        {
            OverrideTableName = OrdersTableName,
            IndexName = CustomerIdGlobalIndex,
        };

        var query = _dynamoDbContext.QueryAsync<Order>(customerId, config);

        return await query.GetRemainingAsync();
    }

    public async Task<Order?> GetOrderByReservationIdAsync(string reservationId)
    {
        var config = new QueryConfig
        {
            OverrideTableName = OrdersTableName,
            IndexName = ReservationIdGlobalIndex,
        };

        var results = await _dynamoDbContext.QueryAsync<Order>(reservationId, config).GetNextSetAsync();

        return results.FirstOrDefault(o => o.Status != OrderStatus.Cancelled);
    }

    public async Task SaveOrderAsync(Order order)
    {
        await _dynamoDbContext.SaveAsync(order, DynamoConfig.SaveConfig(OrdersTableName));
    }

    public async Task<bool> SaveOrderAndGroupReservationAsync(Order order, GroupReservation groupReservation)
    {
        var orderWrite = _dynamoDbContext.CreateTransactWrite<Order>(new TransactWriteConfig { OverrideTableName = OrdersTableName });
        orderWrite.AddSaveItem(order);

        // The group status is updated only if it has not been Submitted yet (guards against concurrent/retried submits).
        var submittedStatus = _dynamoDbContext.ToDocument(groupReservation)["status"];
        var groupWrite = _dynamoDbContext.CreateTransactWrite<GroupReservation>(new TransactWriteConfig { OverrideTableName = ResourceNames.GroupReservationsTableName });
        groupWrite.AddSaveItem(
            groupReservation.GroupReservationId,
            new Expression
            {
                ExpressionStatement = "SET #s = :submitted",
                ExpressionAttributeNames = new Dictionary<string, string> { ["#s"] = "status" },
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> { [":submitted"] = submittedStatus }
            },
            new Expression
            {
                ExpressionStatement = "attribute_exists(groupReservationId) AND (attribute_not_exists(#s) OR #s <> :submitted)",
                ExpressionAttributeNames = new Dictionary<string, string> { ["#s"] = "status" },
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> { [":submitted"] = submittedStatus }
            });

        try
        {
            await _dynamoDbContext.CreateMultiTableTransactWrite(orderWrite, groupWrite).ExecuteAsync();
            return true;
        }
        catch (TransactionCanceledException ex) when (ex.CancellationReasons?.Any(r => r.Code == "ConditionalCheckFailed") == true)
        {
            return false;
        }
    }
}