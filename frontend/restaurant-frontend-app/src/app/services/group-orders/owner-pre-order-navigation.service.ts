import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { EMPTY, catchError, forkJoin } from 'rxjs';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';

const DEFAULT_OWNER_NAME = 'Owner';

export function guestPreorderStorageKey(
  inviteToken: string,
  part: 'guestName' | 'guestSessionId' | 'guestSessionToken' | 'ownerReservationId'
): string {
  return `guestPreorder:${inviteToken}:${part}`;
}

/**
 * Single place that opens the group owner's own guest pre-order cart (/join/:token).
 * Used by "Edit my Pre-Order" (group order page) and "Make Pre-Order" (reservation confirmed).
 * It also remembers (per invite token) which reservation the owner came from, so that
 * submitting the owner's cart can redirect back to the group pre-order preview.
 */
@Injectable({ providedIn: 'root' })
export class OwnerPreOrderNavigationService {
  private readonly router = inject(Router);
  private readonly groupOrdersService = inject(GroupOrdersService);

  openOwnerPreOrder(
    reservationId: string,
    resolveOwnerName?: (guestSessionId: string) => string | undefined
  ): void {
    forkJoin({
      inviteToken: this.groupOrdersService.getGroupInviteToken$(reservationId),
      ownerSession: this.groupOrdersService.getOwnerSession$(reservationId),
    })
      .pipe(catchError(() => EMPTY))
      .subscribe(({ inviteToken, ownerSession }) => {
        if (!inviteToken || !ownerSession.guestSessionId || !ownerSession.sessionToken) {
          return;
        }

        const ownerName = resolveOwnerName?.(ownerSession.guestSessionId) ?? DEFAULT_OWNER_NAME;

        localStorage.setItem(
          guestPreorderStorageKey(inviteToken, 'guestSessionId'),
          ownerSession.guestSessionId
        );
        localStorage.setItem(
          guestPreorderStorageKey(inviteToken, 'guestSessionToken'),
          ownerSession.sessionToken
        );
        localStorage.setItem(guestPreorderStorageKey(inviteToken, 'guestName'), ownerName);
        localStorage.setItem(guestPreorderStorageKey(inviteToken, 'ownerReservationId'), reservationId);

        void this.router.navigate(['/join', inviteToken]);
      });
  }
}
