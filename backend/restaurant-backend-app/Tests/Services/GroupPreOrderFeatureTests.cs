using APIHandler.Constants;
using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Models.GuestSessions;
using APIHandler.Repositories.Auth;
using APIHandler.Repositories.Dishes;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Orders;
using APIHandler.Repositories.Reservations;
using APIHandler.Repositories.Tables;
using APIHandler.Services.Auth;
using APIHandler.Services.GuestSessions;
using APIHandler.Services.GroupReservations;
using APIHandler.Services.Locations;
using AutoFixture;
using Moq;

namespace APIHandler.Tests.Services;

public class GroupPreOrderFeatureTests
{
    private const string UserId = "owner-1";
    private const string ReservationId = "reservation-1";
    private const string GroupId = "group-1";

    private readonly Fixture _fixture = new();
    private readonly Mock<IGroupReservationRepository> _groups = new();
    private readonly Mock<IGuestSessionRepository> _sessions = new();
    private readonly Mock<IDishRepository> _dishes = new();
    private readonly Mock<IReservationRepository> _reservations = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ITableRepository> _tables = new();
    private readonly Mock<IOrderRepository> _orders = new();

    private GuestSessionService GuestService() => new(
        _groups.Object, _sessions.Object, _dishes.Object, _reservations.Object, _users.Object, _tables.Object);

    private GroupReservationService GroupService() => new(
        _groups.Object, _tables.Object, _reservations.Object, _sessions.Object, _orders.Object,
        Mock.Of<IUserService>(), Mock.Of<ILocationService>());

    [Fact]
    public async Task GetGroupSessionsAsync_OwnerAndGroupSessions_ReturnsParticipantsAndTotals()
    {
        var group = Group();
        var reservation = Reservation();
        var pasta = Dish("pasta", 12.5m);
        var drink = Dish("drink", 3m);
        var owner = Session("owner-session", GroupId, [Item("pasta", 2)], isOwner: true, isSubmitted: true);
        var guest = Session("guest-session", GroupId, [Item("drink", 1)]);
        SetupOwner(group, reservation);
        _sessions.Setup(r => r.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync([owner, guest]);
        _dishes.Setup(r => r.GetDishesByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.Contains("pasta") && ids.Contains("drink"))))
            .ReturnsAsync([pasta, drink]);

        var result = await GuestService().GetGroupSessionsAsync(UserId, GroupId);

        var participants = result.Participants.ToArray();
        Assert.Equal(2, participants.Length);
        Assert.Equal(ParticipantType.Owner, participants[0].Type);
        Assert.True(participants[0].IsSubmitted);
        Assert.Equal(25m, participants[0].Total);
        Assert.Equal(ParticipantType.Guest, participants[1].Type);
        Assert.False(participants[1].IsSubmitted);
        Assert.Equal(3m, participants[1].Total);
        Assert.Equal(28m, result.Total);
        _sessions.Verify(r => r.GetAllByGroupReservationIdAsync(GroupId), Times.Once);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_GroupMissing_ThrowsNotFound()
    {
        _groups.Setup(r => r.GetGroupReservationByIdAsync(GroupId)).ReturnsAsync((GroupReservation?)null);

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() => GuestService().GetGroupSessionsAsync(UserId, GroupId));
    }

    [Fact]
    public async Task GetGroupSessionsAsync_ReservationMissing_ThrowsNotFound()
    {
        _groups.Setup(r => r.GetGroupReservationByIdAsync(GroupId)).ReturnsAsync(Group());
        _reservations.Setup(r => r.GetReservationByIdAsync(ReservationId)).ReturnsAsync((Reservation?)null);

        await Assert.ThrowsAsync<ReservationNotFoundException>(() => GuestService().GetGroupSessionsAsync(UserId, GroupId));
    }

    [Fact]
    public async Task GetGroupSessionsAsync_NonOwner_ThrowsForbidden()
    {
        SetupOwner(Group(), Reservation(customerId: "someone-else"));

        await Assert.ThrowsAsync<ActionForbiddenException>(() => GuestService().GetGroupSessionsAsync(UserId, GroupId));
    }

    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_ReservationMissing_ThrowsNotFound()
    {
        _reservations.Setup(r => r.GetReservationByIdAsync(ReservationId)).ReturnsAsync((Reservation?)null);

        await Assert.ThrowsAsync<ReservationNotFoundException>(() => GuestService().GetOwnerGuestSessionIdAsync(UserId, ReservationId));
    }

    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_NonOwner_ThrowsForbidden()
    {
        _reservations.Setup(r => r.GetReservationByIdAsync(ReservationId)).ReturnsAsync(Reservation(customerId: "not-owner"));

        await Assert.ThrowsAsync<ActionForbiddenException>(() => GuestService().GetOwnerGuestSessionIdAsync(UserId, ReservationId));
    }

    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_NoOwnerSession_CreatesAndReturnsSessionId()
    {
        _reservations.Setup(r => r.GetReservationByIdAsync(ReservationId)).ReturnsAsync(Reservation());
        _groups.Setup(r => r.GetByReservationIdAsync(ReservationId)).ReturnsAsync(Group());
        _sessions.Setup(r => r.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync([]);
        GuestSession? saved = null;
        _sessions.Setup(r => r.TryCreateAsync(It.IsAny<GuestSession>())).ReturnsAsync(true)
            .Callback<GuestSession>(session => saved = session);

        var result = await GuestService().GetOwnerGuestSessionIdAsync(UserId, ReservationId);

        Assert.NotNull(saved);
        Assert.Equal("owner-" + GroupId, saved.Id);
        Assert.Equal(result.GuestSessionId, saved.Id);
        Assert.Equal(GroupId, saved.GroupReservationId);
        Assert.Equal(UserId, saved.OwnerUserId);
        Assert.True(saved.IsOwnerSession);
        Assert.Empty(saved.Dishes);
        _sessions.Verify(r => r.TryCreateAsync(It.IsAny<GuestSession>()), Times.Once);
    }

    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_OwnerSessionExists_ReturnsExistingWithoutCreating()
    {
        var existing = Session("existing-owner-session", GroupId, [], isOwner: true);
        existing.OwnerUserId = UserId;
        _reservations.Setup(r => r.GetReservationByIdAsync(ReservationId)).ReturnsAsync(Reservation());
        _groups.Setup(r => r.GetByReservationIdAsync(ReservationId)).ReturnsAsync(Group());
        _sessions.Setup(r => r.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync([existing]);

        var result = await GuestService().GetOwnerGuestSessionIdAsync(UserId, ReservationId);

        Assert.Equal(existing.Id, result.GuestSessionId);
        Assert.False(string.IsNullOrEmpty(result.SessionToken));
        _sessions.Verify(r => r.TryCreateAsync(It.IsAny<GuestSession>()), Times.Never);
        // Legacy/existing owner session: token is issued (hash stored) and matches the returned token.
        Assert.Equal(
            Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(Convert.FromBase64String(result.SessionToken!))),
            existing.SessionTokenHash);
        _sessions.Verify(r => r.SaveAsync(existing), Times.Once);
    }

    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_NewSession_StoresOnlyTokenHash()
    {
        _reservations.Setup(r => r.GetReservationByIdAsync(ReservationId)).ReturnsAsync(Reservation());
        _groups.Setup(r => r.GetByReservationIdAsync(ReservationId)).ReturnsAsync(Group());
        _sessions.Setup(r => r.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync([]);
        GuestSession? saved = null;
        _sessions.Setup(r => r.TryCreateAsync(It.IsAny<GuestSession>())).ReturnsAsync(true)
            .Callback<GuestSession>(s => saved = s);

        var result = await GuestService().GetOwnerGuestSessionIdAsync(UserId, ReservationId);

        Assert.NotNull(saved);
        Assert.NotEqual(result.SessionToken, saved.SessionTokenHash);
        Assert.Equal(
            Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(Convert.FromBase64String(result.SessionToken!))),
            saved.SessionTokenHash);
    }

    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_CalledTwice_RotatesToken()
    {
        var existing = Session("owner-session", GroupId, [], isOwner: true);
        existing.OwnerUserId = UserId;
        _reservations.Setup(r => r.GetReservationByIdAsync(ReservationId)).ReturnsAsync(Reservation());
        _groups.Setup(r => r.GetByReservationIdAsync(ReservationId)).ReturnsAsync(Group());
        _sessions.Setup(r => r.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync([existing]);

        var first = await GuestService().GetOwnerGuestSessionIdAsync(UserId, ReservationId);
        var firstHash = existing.SessionTokenHash;
        var second = await GuestService().GetOwnerGuestSessionIdAsync(UserId, ReservationId);

        Assert.NotEqual(first.SessionToken, second.SessionToken);
        Assert.NotEqual(firstHash, existing.SessionTokenHash);
    }

    [Fact]
    public async Task RemoveDishFromGroupSessionAsync_Owner_RemovesItemAndReturnsSnapshot()
    {
        var guest = Session("guest", GroupId, [Item("a", 2), Item("b", 1)]);
        SetupOwner(Group(), Reservation());
        _sessions.Setup(r => r.GetByGuestSessionId("guest")).ReturnsAsync(guest);
        _sessions.Setup(r => r.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync([guest]);
        _dishes.Setup(r => r.GetDishesByIdsAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync([Dish("b", 5m)]);

        var result = await GuestService().RemoveDishFromGroupSessionAsync(UserId, GroupId, "guest", "a");

        _sessions.Verify(r => r.SaveAsync(guest), Times.Once);
        Assert.DoesNotContain(guest.Dishes, d => d.DishId == "a");
        Assert.Equal(5m, result.Total);
    }

    [Fact]
    public async Task RemoveDishFromGroupSessionAsync_NonOwner_ThrowsForbidden()
    {
        SetupOwner(Group(), Reservation(customerId: "other"));

        await Assert.ThrowsAsync<ActionForbiddenException>(() => GuestService().RemoveDishFromGroupSessionAsync(UserId, GroupId, "guest", "a"));
        _sessions.Verify(r => r.SaveAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    [Fact]
    public async Task RemoveDishFromGroupSessionAsync_GroupMissing_ThrowsNotFound()
    {
        _groups.Setup(r => r.GetGroupReservationByIdAsync(GroupId)).ReturnsAsync((GroupReservation?)null);

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() => GuestService().RemoveDishFromGroupSessionAsync(UserId, GroupId, "guest", "a"));
    }

    [Fact]
    public async Task RemoveDishFromGroupSessionAsync_SessionMissing_ThrowsNotFound()
    {
        SetupOwner(Group(), Reservation());
        _sessions.Setup(r => r.GetByGuestSessionId("guest")).ReturnsAsync((GuestSession?)null);

        await Assert.ThrowsAsync<GuestSessionNotFoundException>(() => GuestService().RemoveDishFromGroupSessionAsync(UserId, GroupId, "guest", "a"));
    }

    [Fact]
    public async Task RemoveDishFromGroupSessionAsync_ItemMissing_ThrowsNotFound()
    {
        SetupOwner(Group(), Reservation());
        _sessions.Setup(r => r.GetByGuestSessionId("guest")).ReturnsAsync(Session("guest", GroupId, [Item("b", 1)]));

        await Assert.ThrowsAsync<DishNotFoundException>(() => GuestService().RemoveDishFromGroupSessionAsync(UserId, GroupId, "guest", "a"));
    }

    [Fact]
    public async Task RemoveDishFromGroupSessionAsync_OtherGroup_ThrowsValidation()
    {
        SetupOwner(Group(), Reservation());
        _sessions.Setup(r => r.GetByGuestSessionId("guest")).ReturnsAsync(Session("guest", "another", [Item("a", 1)]));

        await Assert.ThrowsAsync<ValidationException>(() => GuestService().RemoveDishFromGroupSessionAsync(UserId, GroupId, "guest", "a"));
        _sessions.Verify(r => r.SaveAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    [Fact]
    public async Task RemoveDishFromGroupSessionAsync_GroupSubmitted_ThrowsSubmitted()
    {
        var group = Group();
        group.Status = GroupReservationStatus.Submitted;
        SetupOwner(group, Reservation());
        _sessions.Setup(r => r.GetByGuestSessionId("guest")).ReturnsAsync(Session("guest", GroupId, [Item("a", 1)]));

        await Assert.ThrowsAsync<GroupReservationSubmittedException>(() => GuestService().RemoveDishFromGroupSessionAsync(UserId, GroupId, "guest", "a"));
        _sessions.Verify(r => r.SaveAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_OrdersOwnerFirstThenByName()
    {
        SetupOwner(Group(), Reservation());
        var zed = Session("z", GroupId, []);
        var amy = Session("a", GroupId, []);
        var owner = Session("owner", GroupId, [], isOwner: true);
        _sessions.Setup(r => r.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync([zed, amy, owner]);

        var result = await GuestService().GetGroupSessionsAsync(UserId, GroupId);

        Assert.Equal(["owner", "a", "z"], result.Participants.Select(p => p.Id).ToArray());
    }

    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_GroupMissing_ThrowsNotFound()
    {
        _reservations.Setup(r => r.GetReservationByIdAsync(ReservationId)).ReturnsAsync(Reservation());
        _groups.Setup(r => r.GetByReservationIdAsync(ReservationId)).ReturnsAsync((GroupReservation?)null);

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() => GuestService().GetOwnerGuestSessionIdAsync(UserId, ReservationId));
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_NonOwner_ThrowsForbidden()
    {
        SetupOwner(Group(), Reservation(customerId: "other"));

        await Assert.ThrowsAsync<ActionForbiddenException>(() => GuestService().DeleteGuestSessionAsync(UserId, GroupId, "guest"));
        _sessions.Verify(r => r.GetByGuestSessionId(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_SessionMissing_ThrowsNotFound()
    {
        SetupOwner(Group(), Reservation());
        _sessions.Setup(r => r.GetByGuestSessionId("missing")).ReturnsAsync((GuestSession?)null);

        await Assert.ThrowsAsync<GuestSessionNotFoundException>(() => GuestService().DeleteGuestSessionAsync(UserId, GroupId, "missing"));
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_SessionBelongsToOtherGroup_ThrowsBadRequest()
    {
        SetupOwner(Group(), Reservation());
        _sessions.Setup(r => r.GetByGuestSessionId("foreign-session"))
            .ReturnsAsync(Session("foreign-session", "another-group", []));

        await Assert.ThrowsAsync<ValidationException>(() => GuestService().DeleteGuestSessionAsync(UserId, GroupId, "foreign-session"));
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_OwnerSession_ThrowsBadRequest()
    {
        SetupOwner(Group(), Reservation());
        _sessions.Setup(r => r.GetByGuestSessionId("owner-session"))
            .ReturnsAsync(Session("owner-session", GroupId, [], isOwner: true));

        await Assert.ThrowsAsync<ValidationException>(() => GuestService().DeleteGuestSessionAsync(UserId, GroupId, "owner-session"));
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_ValidGuest_DeletesAndReturnsRecalculatedSnapshot()
    {
        var reservation = Reservation();
        var group = Group();
        var removed = Session("to-delete", GroupId, [Item("removed-dish", 1)]);
        var remaining = Session("remaining", GroupId, [Item("kept-dish", 2)]);
        SetupOwner(group, reservation);
        _sessions.Setup(r => r.GetByGuestSessionId(removed.Id)).ReturnsAsync(removed);
        _sessions.Setup(r => r.DeleteAsync(removed)).Callback(() =>
            _sessions.Setup(repo => repo.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync([remaining]));
        _sessions.Setup(r => r.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync([removed, remaining]);
        _dishes.Setup(r => r.GetDishesByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.Contains("kept-dish"))))
            .ReturnsAsync([Dish("kept-dish", 4m)]);

        var result = await GuestService().DeleteGuestSessionAsync(UserId, GroupId, removed.Id);

        _sessions.Verify(r => r.DeleteAsync(removed), Times.Once);
        var participant = Assert.Single(result.Participants);
        Assert.Equal(remaining.Id, participant.Id);
        Assert.Equal(8m, participant.Total);
        Assert.Equal(8m, result.Total);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_ConditionFails_ReloadsAndReturnsMergedWithoutReapplying()
    {
        SetupGroupAndReservation();
        _sessions.Setup(r => r.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync(
            [Session("submitted", GroupId, [Item("dish", 3)], isSubmitted: true)]);
        var order = Order([Item("dish", 3)]);
        _orders.Setup(r => r.GetOrderByReservationIdAsync(ReservationId)).ReturnsAsync(order);
        _orders.Setup(r => r.SaveOrderAndGroupReservationAsync(It.IsAny<Order>(), It.IsAny<GroupReservation>())).ReturnsAsync(false);

        var result = await GroupService().SubmitGroupPreOrderAsync(UserId, GroupId, ReservationId);

        var dish = Assert.Single(result);
        Assert.Equal(3, dish.Quantity);
        _orders.Verify(r => r.SaveOrderAndGroupReservationAsync(It.IsAny<Order>(), It.IsAny<GroupReservation>()), Times.Once);
        _orders.Verify(r => r.GetOrderByReservationIdAsync(ReservationId), Times.Exactly(2));
        _groups.Verify(r => r.GetGroupReservationByIdAsync(GroupId), Times.Exactly(2));
        _orders.Verify(r => r.SaveOrderAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_RepositoryStillReturnsDeletedSession_ExcludesItFromSnapshot()
    {
        var removed = Session("to-delete", GroupId, [Item("removed-dish", 1)]);
        var remaining = Session("remaining", GroupId, [Item("kept-dish", 2)]);
        SetupOwner(Group(), Reservation());
        _sessions.Setup(r => r.GetByGuestSessionId(removed.Id)).ReturnsAsync(removed);
        _sessions.Setup(r => r.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync([removed, remaining]);
        _dishes.Setup(r => r.GetDishesByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.Contains("kept-dish"))))
            .ReturnsAsync([Dish("kept-dish", 4m)]);

        var result = await GuestService().DeleteGuestSessionAsync(UserId, GroupId, removed.Id);

        var participant = Assert.Single(result.Participants);
        Assert.Equal(remaining.Id, participant.Id);
        Assert.Equal(8m, result.Total);
    }

    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_ConcurrentCreateLost_ReturnsExistingSession()
    {
        var existing = Session("owner-" + GroupId, GroupId, [], isOwner: true);
        _reservations.Setup(r => r.GetReservationByIdAsync(ReservationId)).ReturnsAsync(Reservation());
        _groups.Setup(r => r.GetByReservationIdAsync(ReservationId)).ReturnsAsync(Group());
        _sessions.Setup(r => r.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync([]);
        _sessions.Setup(r => r.TryCreateAsync(It.IsAny<GuestSession>())).ReturnsAsync(false);
        _sessions.Setup(r => r.GetByGuestSessionId("owner-" + GroupId)).ReturnsAsync(existing);

        var result = await GuestService().GetOwnerGuestSessionIdAsync(UserId, ReservationId);

        Assert.Equal(existing.Id, result.GuestSessionId);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_GroupMissing_ThrowsNotFound()
    {
        _groups.Setup(r => r.GetGroupReservationByIdAsync(GroupId)).ReturnsAsync((GroupReservation?)null);

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() => GroupService().SubmitGroupPreOrderAsync(UserId, GroupId, ReservationId));
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_ReservationMissing_ThrowsNotFound()
    {
        _groups.Setup(r => r.GetGroupReservationByIdAsync(GroupId)).ReturnsAsync(Group());
        _reservations.Setup(r => r.GetReservationByIdAsync(ReservationId)).ReturnsAsync((Reservation?)null);

        await Assert.ThrowsAsync<ReservationNotFoundException>(() => GroupService().SubmitGroupPreOrderAsync(UserId, GroupId, ReservationId));
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_NonOwner_ThrowsForbidden()
    {
        SetupGroupAndReservation();
        _reservations.Setup(r => r.GetReservationByIdAsync(ReservationId)).ReturnsAsync(Reservation(customerId: "someone-else"));

        await Assert.ThrowsAsync<ActionForbiddenException>(() => GroupService().SubmitGroupPreOrderAsync(UserId, GroupId, ReservationId));
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_NoSubmittedSessions_ThrowsBadRequest()
    {
        SetupGroupAndReservation();
        _sessions.Setup(r => r.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync([Session("guest", GroupId, [Item("dish", 1)])]);

        await Assert.ThrowsAsync<ValidationException>(() => GroupService().SubmitGroupPreOrderAsync(UserId, GroupId, ReservationId));
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_SubmittedSessionsContainNoPositiveQuantities_ThrowsBadRequest()
    {
        SetupGroupAndReservation();
        _sessions.Setup(r => r.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync(
            [Session("submitted", GroupId, [Item("dish", 0)], isSubmitted: true)]);

        await Assert.ThrowsAsync<ValidationException>(() => GroupService().SubmitGroupPreOrderAsync(UserId, GroupId, ReservationId));
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_OrderMissing_ThrowsBadRequest()
    {
        SetupGroupAndReservation();
        _sessions.Setup(r => r.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync(
            [Session("submitted", GroupId, [Item("dish", 1)], isSubmitted: true)]);
        _orders.Setup(r => r.GetOrderByReservationIdAsync(ReservationId)).ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<ValidationException>(() => GroupService().SubmitGroupPreOrderAsync(UserId, GroupId, ReservationId));
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_SubmittedSessions_MergesDishQuantitiesAndPersistsStatusesOnce()
    {
        SetupGroupAndReservation();
        var submittedA = Session("submitted-a", GroupId, [Item("dish-a", 2), Item("dish-b", 1)], isSubmitted: true);
        var submittedB = Session("submitted-b", GroupId, [Item("dish-a", 3)], isSubmitted: true);
        var unsubmitted = Session("not-submitted", GroupId, [Item("dish-a", 100)]);
        _sessions.Setup(r => r.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync([submittedA, unsubmitted, submittedB]);
        var order = Order([Item("dish-a", 4)]);
        _orders.Setup(r => r.GetOrderByReservationIdAsync(ReservationId)).ReturnsAsync(order);
        GroupReservation? savedGroup = null;
        Order? savedOrder = null;
        _orders.Setup(r => r.SaveOrderAndGroupReservationAsync(It.IsAny<Order>(), It.IsAny<GroupReservation>()))
            .ReturnsAsync(true)
            .Callback<Order, GroupReservation>((saved, savedGroupReservation) =>
            {
                savedOrder = saved;
                savedGroup = savedGroupReservation;
            });

        var result = await GroupService().SubmitGroupPreOrderAsync(UserId, GroupId, ReservationId);

        Assert.Equal(new[] { new { DishId = "dish-a", Quantity = 5 }, new { DishId = "dish-b", Quantity = 1 } },
            result.Select(dish => new { dish.DishId, dish.Quantity }));
        Assert.Equal(9, Assert.Single(order.Dishes.Where(item => item.DishId == "dish-a")).Quantity);
        Assert.Equal(1, Assert.Single(order.Dishes.Where(item => item.DishId == "dish-b")).Quantity);
        Assert.Equal(OrderStatus.SubmittedByCustomer, savedOrder!.Status);
        Assert.Equal(GroupReservationStatus.Submitted, savedGroup!.Status);
        _orders.Verify(r => r.SaveOrderAndGroupReservationAsync(order, It.Is<GroupReservation>(g => g.GroupReservationId == GroupId)), Times.Once);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_RetryAfterSubmission_ReturnsMergedDishesWithoutWrites()
    {
        var group = Group();
        group.Status = GroupReservationStatus.Submitted;
        SetupGroupAndReservation(group);
        var session = Session("submitted", GroupId, [Item("dish", 3)], isSubmitted: true);
        _sessions.Setup(r => r.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync([session]);
        var order = Order([Item("dish", 3)]);
        order.Status = OrderStatus.SubmittedByCustomer;
        _orders.Setup(r => r.GetOrderByReservationIdAsync(ReservationId)).ReturnsAsync(order);

        var result = await GroupService().SubmitGroupPreOrderAsync(UserId, GroupId, ReservationId);

        var dish = Assert.Single(result);
        Assert.Equal("dish", dish.DishId);
        Assert.Equal(3, dish.Quantity);
        Assert.Equal(3, Assert.Single(order.Dishes).Quantity);
        _orders.Verify(r => r.SaveOrderAndGroupReservationAsync(It.IsAny<Order>(), It.IsAny<GroupReservation>()), Times.Never);
        _orders.Verify(r => r.SaveOrderAsync(It.IsAny<Order>()), Times.Never);
    }

    private void SetupOwner(GroupReservation group, Reservation reservation)
    {
        _groups.Setup(r => r.GetGroupReservationByIdAsync(group.GroupReservationId)).ReturnsAsync(group);
        _reservations.Setup(r => r.GetReservationByIdAsync(group.ReservationId)).ReturnsAsync(reservation);
    }

    private void SetupGroupAndReservation(GroupReservation? group = null)
    {
        group ??= Group();
        _groups.Setup(r => r.GetGroupReservationByIdAsync(GroupId)).ReturnsAsync(group);
        _reservations.Setup(r => r.GetReservationByIdAsync(ReservationId)).ReturnsAsync(Reservation());
    }

    private static GroupReservation Group() => new()
    {
        GroupReservationId = GroupId,
        ReservationId = ReservationId,
        ReservationToken = "token",
        Status = GroupReservationStatus.Open
    };

    private static Reservation Reservation(string customerId = UserId) => new()
    {
        Id = ReservationId,
        TableId = "table",
        WaiterId = "waiter",
        CustomerId = customerId
    };

    private Dish Dish(string id, decimal price) => new()
    {
        Id = id,
        Name = _fixture.Create<string>(),
        Description = _fixture.Create<string>(),
        Price = price,
        Weight = "100g",
        Calories = "100",
        Proteins = "10",
        Fats = "5",
        Carbohydrates = "10",
        Vitamins = "A",
        State = APIHandler.Constants.Dishes.DishState.Available,
        LocationId = "location"
    };

    private static GuestSession Session(string id, string groupId, List<OrderItem> items, bool isOwner = false, bool isSubmitted = false) => new()
    {
        Id = id,
        GroupReservationId = groupId,
        GuestName = id,
        Dishes = items,
        IsOwnerSession = isOwner,
        IsSubmitted = isSubmitted
    };

    private static OrderItem Item(string id, int quantity) => new()
    {
        DishId = id,
        Quantity = quantity,
        State = OrderItemState.Pending
    };

    private static Order Order(List<OrderItem> items) => new()
    {
        OrderId = "order-1",
        CustomerId = UserId,
        ReservationId = ReservationId,
        ReservationAddress = "address",
        ReservationTime = "time",
        Dishes = items,
        Status = OrderStatus.Created
    };
}
