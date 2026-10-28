using APIHandler.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Repositories.Orders;

public interface IOrderRepository
{
    Task<Order?> GetOrderByIdAsync(string orderId, string customerId);
    Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(string customerId);
    Task<Order?> GetOrderByReservationIdAsync(string reservationId);
    Task SaveOrderAsync(Order order);

    /// <summary>Atomically persists the order and the group reservation in a single DynamoDB transaction.</summary>
    /// <summary>Returns false when the transaction was canceled because the group reservation was already Submitted.</summary>
    Task<bool> SaveOrderAndGroupReservationAsync(Order order, GroupReservation groupReservation);
}

