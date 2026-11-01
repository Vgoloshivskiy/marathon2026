# Frontend Homework: Group Pre-Order

## Project overview

This project extends the restaurant reservation app with a group pre-order experience. Guests can join a reservation from an invite link and submit their own pre-order. The reservation owner can review the participants' carts, manage the group order, and submit one consolidated pre-order to the restaurant.

The feature is implemented in the Angular app at [`frontend/restaurant-frontend-app`](frontend/restaurant-frontend-app/README.md) and integrates with the deployed AWS backend.

## Implemented functionality

### Guest pre-order

- Guests join through a reservation invite and provide their name.
- Guests browse dishes, manage their cart, and submit their pre-order.
- Submitted guest carts appear in the owner's group-order view.

### Group-owner pre-order

- The owner can view participants, each participant's cart and subtotal, and their submission status.
- The owner's own cart is identified as **My Pre-Order**.
- The consolidated order includes submitted carts only and combines quantities for matching dishes.
- The owner can edit their own pre-order, adjust or remove items from participant carts, and remove guests while the group is open.
- A confirmation dialog protects submission. On success, the group is marked as sent to the restaurant and the view becomes read-only.
- While pre-orders are open, the page refreshes group data every 15 seconds. The owner can also refresh manually; failed background refreshes preserve the last loaded data and display a warning.
- Loading, error, empty-cart, and refresh states are shown in the interface.

## Manual verification

I manually tested both the guest pre-order flow and the group-owner pre-order flow against the backend deployed to AWS.

## Run locally

From the frontend project directory:

```bash
npm ci
npm start
```

The app is served at `http://localhost:4200`. The backend URL is configured in `src/environments/environment.development.ts`; use the appropriate deployed API Gateway URL for your environment.

## Automated checks

The homework-focused tests can be run with:

```bash
npm run test:hw
```

To build the frontend:

```bash
npm run build
```

## AI assistance

AI assistance was used to prepare this README. The manual end-to-end testing described above was performed by me against the deployed AWS backend.
