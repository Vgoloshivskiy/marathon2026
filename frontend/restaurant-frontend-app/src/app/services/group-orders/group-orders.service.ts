import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, map, switchMap } from 'rxjs';
import { SKIP_ERROR_TOAST } from '@app/auth/skip-error-toast.context';
import { GroupSessionsResponse } from '@app/models/group-orders/group-sessions-response.model';
import { GroupStatusResponse } from '@app/models/group-orders/group-status.model';
import { SubmittedDish } from '@app/models/group-orders/submit-group-pre-order.model';
import { GroupInviteToken, GuestInvite } from '@app/models/reservations/guest-invite.model';
import { GuestInviteService } from '@app/services/reservation/guest-invite.service';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

@Injectable({ providedIn: 'root' })
export class GroupOrdersService {
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly http = inject(HttpClient);
  private readonly guestInviteService = inject(GuestInviteService);

  getGroupInvite$(reservationId: string): Observable<GuestInvite> {
    return this.guestInviteService
      .getInviteToken$(reservationId)
      .pipe(
        switchMap((inviteToken: GroupInviteToken) =>
          this.guestInviteService.getInvite$(inviteToken.token)
        )
      );
  }

  getSessions$(
    groupId: string,
    options: { silent?: boolean } = {}
  ): Observable<GroupSessionsResponse> {
    return this.http.get<GroupSessionsResponse>(
      `${this.baseUrl}/guest/groups/${encodeURIComponent(groupId)}/sessions`,
      { context: new HttpContext().set(SKIP_ERROR_TOAST, options.silent ?? false) }
    );
  }

  getOwnerSessionId$(reservationId: string): Observable<string> {
    return this.http
      .get<{
        guestSessionId: string;
      }>(`${this.baseUrl}/reservations/${encodeURIComponent(reservationId)}/guest-session-id`)
      .pipe(map((response) => response.guestSessionId));
  }

  getStatus$(groupId: string, options: { silent?: boolean } = {}): Observable<GroupStatusResponse> {
    return this.http.get<GroupStatusResponse>(
      `${this.baseUrl}/guest/groups/${encodeURIComponent(groupId)}/status`,
      { context: new HttpContext().set(SKIP_ERROR_TOAST, options.silent ?? false) }
    );
  }

  submit$(groupId: string, reservationId: string): Observable<SubmittedDish[]> {
    return this.http.post<SubmittedDish[]>(
      `${this.baseUrl}/group-reservations/${encodeURIComponent(groupId)}/submit`,
      { reservationId }
    );
  }

  removeSession$(groupId: string, guestSessionId: string): Observable<GroupSessionsResponse> {
    return this.http
      .delete<
        GroupSessionsResponse | PascalCaseSessions
      >(`${this.baseUrl}/guest/groups/${encodeURIComponent(groupId)}/sessions/${encodeURIComponent(guestSessionId)}`)
      .pipe(map((response) => normalizeSessions(response)));
  }
}

// The deployed DELETE returns PascalCase participant fields while GET returns camelCase.
type PascalCaseSessions = {
  Participants: {
    Id: string;
    Type: string;
    Name: string;
    Items: GroupSessionsResponse['participants'][number]['items'];
    Total: number;
    IsSubmitted: boolean;
  }[];
  Total: number;
};

function normalizeSessions(
  response: GroupSessionsResponse | PascalCaseSessions
): GroupSessionsResponse {
  if ('participants' in response) {
    return response;
  }
  return {
    participants: response.Participants.map(
      (p) =>
        ({
          id: p.Id,
          type: p.Type,
          name: p.Name,
          items: p.Items,
          total: p.Total,
          isSubmitted: p.IsSubmitted,
        }) as GroupSessionsResponse['participants'][number]
    ),
    total: response.Total,
  };
}
