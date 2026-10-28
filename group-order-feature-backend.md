# Group Order Feature — Backend

This document summarizes the backend changes made for group pre-order management. The API lets an owner share a group cart, monitor participant orders, remove items or participants, and submit one consolidated order.

## Step 1: View participants and group-cart totals

### Endpoint

`GET /guest/groups/{groupReservationId}/sessions`

### Behavior

- Requires an authenticated owner. The caller's Cognito `sub` must match the customer who owns the reservation associated with the group reservation.
- Returns the owner's session and guest sessions belonging to the requested group reservation.
- Includes each participant's session ID, type (`Owner` or `Guest`), name, dishes and quantities, participant total, and submission status.
- Calculates each participant total from dish price multiplied by quantity, then calculates the group total from participant totals.
- Orders participants deterministically: owner first, then by name, then by session ID.
- Returns `401` when the caller is unauthenticated, `403` for a non-owner, and `404` when the group reservation is missing.

### Example response

```json
{
  "participants": [
    {
      "id": "session1",
      "type": "Guest",
      "name": "Andrii",
      "items": [
        {
          "dish": {
            "id": "dish1",
            "name": "Caesar Salad",
            "price": 180
          },
          "quantity": 2
        }
      ],
      "total": 360,
      "isSubmitted": true
    }
  ],
  "total": 360
}
```

## Step 2: Owner session and consolidated submission

### 2.1 Retrieve the owner's guest session

`GET /reservations/{reservationId}/guest-session-id`

The endpoint verifies that the caller owns the reservation and returns:

```json
{
  "guestSessionId": "owner-session-123",
  "sessionToken": "opaque-session-token"
}
```

The owner session is created lazily if it does not exist. Its ID is deterministic for the group, and creation uses a conditional write so concurrent first calls converge on the existing session. Existing owner sessions without a token receive one when this endpoint is called.

The response retains the original `guestSessionId` field and adds `sessionToken`. The token is generated for the owner to use with the existing guest-session cart endpoints for their own session. The backend stores only a hash of the token. Each call issues a new token and invalidates the previously issued token for that owner session.

Errors: `401` unauthenticated, `403` not the owner, `404` reservation or associated group reservation not found.

### 2.2 Submit the consolidated group order

`POST /group-reservations/{groupReservationId}/submit`

Request:

```json
{
  "reservationId": "reservation-123"
}
```

The endpoint:

1. Verifies the group reservation, reservation, and owner.
2. Selects only guest sessions whose submission status is `Submitted`.
3. Combines their dishes by `dishId` and sums quantities.
4. Adds the consolidated quantities to the reservation's order.
5. Sets the order status to `SubmittedByCustomer` and the group reservation status to `Submitted`.
6. Persists the order and group status in one DynamoDB transaction.

The response is the consolidated list:

```json
[
  {
    "dishId": "dish1",
    "quantity": 3
  },
  {
    "dishId": "dish2",
    "quantity": 2
  }
]
```

The group-status transaction is conditional on the group not already being submitted. If a concurrent request wins that condition, the losing request reloads the group and order and returns the consolidated result without adding the quantities a second time. This also makes a repeated submission safe after the group is submitted.

Errors: `401` unauthenticated, `403` not the owner, `404` reservation or group reservation not found, `400` when there are no submitted sessions, no dishes in submitted sessions, no associated order, or the group does not belong to the supplied reservation.

## Step 3: Owner management of group carts

### 3.1 Remove a guest from the group

`DELETE /guest/groups/{groupReservationId}/sessions/{guestSessionId}`

The owner can remove a guest session. The response is the updated participant snapshot with recalculated totals, matching the response shape of Step 1. The owner session itself cannot be removed through this endpoint.

Errors: `401` unauthenticated, `403` not the owner, `404` group reservation or session not found, `400` when the session belongs to another group or is the owner session.

The response explicitly excludes the removed session even if a DynamoDB secondary-index query temporarily returns stale data after deletion.

### 3.2 Remove an item from a participant's cart

`DELETE /guest/groups/{groupReservationId}/sessions/{guestSessionId}/items/{dishId}`

The owner can remove the entire line item for a dish from a guest's or the owner's session. Cart lines do not have separate item IDs, so `dishId` identifies the line. The endpoint returns the updated group snapshot.

Errors: `401` unauthenticated, `403` not the owner, `404` group reservation, session, or item not found, `400` when the session belongs to another group or the group has already been submitted.

## Sharing and live updates

The invite-link flow is:

1. The owner gets an invite token from `GET /reservations/{reservationId}/invite-token`.
2. Guests resolve the invite with `GET /guest/invites/{token}`.
3. Guests join by creating a guest session with `POST /guest/sessions`.

The owner can monitor the cart by polling `GET /guest/groups/{groupReservationId}/sessions`. There is no websocket or push-notification endpoint. Participant data is read through an eventually consistent DynamoDB index, so a recent change may not appear until a subsequent poll. `GET /guest/groups/{groupReservationId}/status` remains available for lightweight public status and participant-count polling.

## Implementation and verification

- Route registration, API Gateway deployment resources, `docs/ApiRoutes.md`, and the Swagger endpoint definition were updated for the added API behavior.
- The group pre-order service tests cover participant snapshots and totals, owner authorization, session creation and token rotation, item/guest removal, submission aggregation and status changes, stale-index deletion results, and idempotent submission behavior.
- Reported validation after the implementation: `dotnet build` succeeded and `dotnet test` for `Tests/APIHandler.Tests.csproj` passed (176 tests). The DynamoDB transaction and conditional-write behavior was not verified against a deployed DynamoDB instance.
