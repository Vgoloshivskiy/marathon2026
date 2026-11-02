# Backend Homework: Group Pre-Order

## Project overview

This project extends the restaurant reservation API with group pre-order workflows. Guests can join an owner's group order and submit their own carts. The owner can review the participants, manage the group carts, and submit one consolidated order to the restaurant.

The backend is implemented in `backend/restaurant-backend-app` using .NET 10 and AWS Lambda.

## Implemented functionality

### Guest pre-order

- Guests resolve an owner's invite and join the reservation with a guest session.
- Guests add dishes to their carts and submit their pre-orders.
- The owner can see which participants have joined and the items, totals, and submission status for each session.

### Group-owner workflows

- The owner can retrieve their guest session ID and session token for a reservation.
- The owner can view all participant sessions, including their own, and the total for the group.
- The owner can remove a dish from a participant's cart or remove a guest session while the group is open.
- The owner can submit the consolidated group order. Only submitted guest sessions are included; identical dishes are merged by dish ID and quantities summed.
- Submission updates the order to `SubmittedByCustomer` and the group reservation to `Submitted`. Repeated or concurrent submissions do not add the consolidated quantities twice.
- Owner-only operations validate ownership and return appropriate errors for missing or invalid reservations, sessions, carts, or orders.

See [group-order-feature-backend.md](group-order-feature-backend.md) for endpoint details and response examples.

## Manual verification

I manually tested the guest pre-order and group-owner workflows against the backend deployed to AWS.

## Build and test

Install the .NET 10 SDK. From the backend project directory:

```powershell
cd backend/restaurant-backend-app
dotnet build .\restaurant-backend-app.sln
dotnet test .\Tests\APIHandler.Tests.csproj
```

The tests cover participant snapshots and totals, owner authorization, guest-session creation, cart and participant removal, consolidated submission, and idempotent submission behavior.

## AI assistance

AI assistance included creating agents for coding, orchestration, code review, and test writing. I performed the manual end-to-end testing against the AWS-deployed backend.
