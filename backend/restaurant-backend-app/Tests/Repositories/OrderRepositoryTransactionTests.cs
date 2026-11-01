using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Repositories.Orders;
using Moq;

namespace APIHandler.Tests.Repositories;

public class OrderRepositoryTransactionTests
{
    private static DescribeTableResponse Describe(string table, params (string name, KeyType type)[] keys) => new()
    {
        Table = new TableDescription
        {
            TableName = table,
            TableStatus = TableStatus.ACTIVE,
            KeySchema = keys.Select(k => new KeySchemaElement(k.name, k.type)).ToList(),
            AttributeDefinitions = keys.Select(k => new AttributeDefinition(k.name, ScalarAttributeType.S)).ToList(),
            GlobalSecondaryIndexes = new List<GlobalSecondaryIndexDescription>(),
            LocalSecondaryIndexes = new List<LocalSecondaryIndexDescription>()
        }
    };

    [Fact]
    public async Task SaveOrderAndGroupReservationAsync_BuildsTransactionWithoutThrowing()
    {
        Environment.SetEnvironmentVariable("ordersTable", "orders-test");
        Environment.SetEnvironmentVariable("groupReservationsTable", "group-reservations-test");
        var client = new Mock<IAmazonDynamoDB>();
        client.Setup(c => c.Config).Returns(new AmazonDynamoDBConfig());
        client.Setup(c => c.DescribeTableAsync(ResourceNames.OrdersTableName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Describe(ResourceNames.OrdersTableName, ("orderId", KeyType.HASH), ("customerId", KeyType.RANGE)));
        client.Setup(c => c.DescribeTableAsync(ResourceNames.GroupReservationsTableName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Describe(ResourceNames.GroupReservationsTableName, ("groupReservationId", KeyType.HASH)));
        TransactWriteItemsRequest? captured = null;
        client.Setup(c => c.TransactWriteItemsAsync(It.IsAny<TransactWriteItemsRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TransactWriteItemsRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(new TransactWriteItemsResponse());

        var repo = new OrderRepository(new DynamoDBContextBuilder().WithDynamoDBClient(() => client.Object).Build());
        var order = new Order
        {
            OrderId = "o1", CustomerId = "c1", ReservationId = "r1", ReservationAddress = "a", ReservationTime = "t",
            Dishes = new List<OrderItem>(), Status = OrderStatus.SubmittedByCustomer
        };
        var group = new GroupReservation
        {
            GroupReservationId = "g1", ReservationId = "r1", ReservationToken = "tok", Status = GroupReservationStatus.Submitted
        };

        bool saved = await repo.SaveOrderAndGroupReservationAsync(order, group);

        Assert.True(saved);
        Assert.NotNull(captured);
        Assert.Equal(2, captured!.TransactItems.Count);
        var update = captured.TransactItems.Single(i => i.Update?.UpdateExpression == "SET #s = :submitted").Update;
        Assert.Equal("SET #s = :submitted", update.UpdateExpression);
        Assert.Contains("#cs", update.ExpressionAttributeNames.Keys);
        Assert.Contains(":cs", update.ExpressionAttributeValues.Keys);
        Assert.Contains(":submitted", update.ExpressionAttributeValues.Keys);
    }
}
