/**
 * PATCH /guest/groups/{groupReservationId}/sessions/{guestSessionId}/items/{dishId} — request body.
 * `quantity` is the ABSOLUTE new quantity: > 0 sets it, <= 0 removes the line.
 */
export interface SetItemQuantityRequest {
  quantity: number;
}
