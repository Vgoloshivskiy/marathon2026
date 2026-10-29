import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import {
  EMPTY,
  Observable,
  Subscription,
  catchError,
  exhaustMap,
  finalize,
  forkJoin,
  interval,
  map,
  switchMap,
  tap,
} from 'rxjs';
import { Participant } from '@app/models/group-orders/participant.model';
import { GroupSessionsResponse } from '@app/models/group-orders/group-sessions-response.model';
import {
  GroupReservationStatus,
  GroupStatusResponse,
} from '@app/models/group-orders/group-status.model';
import { GuestInvite } from '@app/models/reservations/guest-invite.model';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { ToastService } from '@app/services/toast/toast.service';
import { ModalComponent } from '@app/shared/custom-components/modal/modal.component';
import { consolidatePreOrder } from '@app/shared/helpers/consolidate-pre-order';
import { ParticipantCardComponent } from '@app/components/group-orders/participant-card/participant-card.component';

const SYNC_INTERVAL_MS = 15_000;

@Component({
  selector: 'app-group-order-page',
  imports: [CurrencyPipe, DatePipe, ModalComponent, ParticipantCardComponent],
  templateUrl: './group-order-page.component.html',
  styleUrl: './group-order-page.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class GroupOrderPageComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly groupOrdersService = inject(GroupOrdersService);
  private readonly toastService = inject(ToastService);
  private pollingSubscription?: Subscription;
  private pollingStarted = false;

  readonly loading = signal(true);
  readonly loadError = signal('');
  readonly reservationId = signal('');
  readonly invite = signal<GuestInvite | null>(null);
  readonly participants = signal<Participant[]>([]);
  readonly ownerSessionId = signal('');
  readonly groupStatus = signal<GroupStatusResponse | null>(null);
  readonly submitting = signal(false);
  readonly refreshing = signal(false);
  readonly refreshFailed = signal(false);
  readonly updatedAt = signal<Date | null>(null);
  readonly submitDialogOpen = signal(false);
  readonly removeGuestTarget = signal<Participant | null>(null);
  readonly removingGuest = signal(false);

  readonly consolidatedOrder = computed(() => consolidatePreOrder(this.participants()));
  readonly notSubmittedCount = computed(
    () => this.participants().filter((participant) => !participant.isSubmitted).length
  );
  readonly noDishesYet = computed(
    () =>
      this.participants().length > 0 &&
      this.participants().every((participant) => participant.items.length === 0)
  );
  readonly isSubmitted = computed(() => this.groupStatus()?.status === 'Submitted');
  readonly canRemoveGuests = computed(
    () => this.groupStatus()?.status === 'Open' && !this.submitting()
  );
  readonly canSubmit = computed(
    () =>
      this.groupStatus()?.status === 'Open' &&
      this.consolidatedOrder().items.length > 0 &&
      !this.submitting()
  );
  readonly statusLabel = computed(() => {
    const status = this.groupStatus()?.status;
    if (status === 'Open') {
      return 'Collecting pre-orders';
    }
    if (status === 'Submitted') {
      return 'Sent to Restaurant';
    }
    return status ?? '';
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('reservationId');
    if (!id) {
      this.loading.set(false);
      this.loadError.set('Group reservation not found');
      return;
    }
    this.reservationId.set(id);
    this.loadGroup();
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  loadGroup(): void {
    if (this.loading() && this.invite()) {
      return;
    }

    this.loading.set(true);
    this.loadError.set('');
    const reservationId = this.reservationId();

    forkJoin({
      invite: this.groupOrdersService.getGroupInvite$(reservationId),
      ownerSessionId: this.groupOrdersService.getOwnerSessionId$(reservationId),
    })
      .pipe(
        switchMap((initial) =>
          forkJoin({
            sessions: this.groupOrdersService.getSessions$(initial.invite.GroupReservationId),
            status: this.groupOrdersService.getStatus$(initial.invite.GroupReservationId),
          }).pipe(map((group) => ({ ...initial, ...group })))
        ),
        catchError((error: HttpErrorResponse) => this.handleLoadError(error))
      )
      .subscribe((group) => {
        this.invite.set(group.invite);
        this.ownerSessionId.set(group.ownerSessionId);
        this.setGroupData(group.sessions, group.status);
        this.loading.set(false);
        this.startPollingIfOpen(group.status.status);
      });
  }

  refresh(): void {
    this.refreshGroup$().subscribe();
  }

  openSubmitDialog(): void {
    if (this.canSubmit()) {
      this.submitDialogOpen.set(true);
    }
  }

  closeSubmitDialog(): void {
    if (!this.submitting()) {
      this.submitDialogOpen.set(false);
    }
  }

  submitOrder(): void {
    const invite = this.invite();
    if (!invite || !this.canSubmit()) {
      return;
    }

    this.submitting.set(true);
    this.groupOrdersService.submit$(invite.GroupReservationId, this.reservationId()).subscribe({
      next: () => {
        this.groupStatus.update((status) => (status ? { ...status, status: 'Submitted' } : status));
        this.submitDialogOpen.set(false);
        this.submitting.set(false);
        this.stopPolling();
        this.toastService.success('Pre-order sent to the restaurant');
      },
      error: () => {
        this.submitting.set(false);
      },
    });
  }

  openRemoveGuest(participant: Participant): void {
    if (this.canRemoveGuests() && participant.type === 'Guest') {
      this.removeGuestTarget.set(participant);
    }
  }

  closeRemoveGuestDialog(): void {
    if (!this.removingGuest()) {
      this.removeGuestTarget.set(null);
    }
  }

  confirmRemoveGuest(): void {
    const invite = this.invite();
    const guest = this.removeGuestTarget();
    if (!invite || !guest || !this.canRemoveGuests() || this.removingGuest()) {
      return;
    }

    this.removingGuest.set(true);
    this.groupOrdersService.removeSession$(invite.GroupReservationId, guest.id).subscribe({
      next: (response) => {
        this.participants.set(response.participants);
        this.updatedAt.set(new Date());
        this.refreshFailed.set(false);
        this.removeGuestTarget.set(null);
        this.removingGuest.set(false);
      },
      error: () => {
        this.removingGuest.set(false);
      },
    });
  }

  private handleLoadError(error: HttpErrorResponse): Observable<never> {
    this.loading.set(false);
    this.loadError.set(
      error.status === 404 ? 'Group reservation not found' : 'Unable to load group reservation.'
    );
    return EMPTY;
  }

  private setGroupData(sessions: GroupSessionsResponse, status: GroupStatusResponse): void {
    this.participants.set(sessions.participants);
    this.groupStatus.set(status);
    this.updatedAt.set(new Date());
    this.refreshFailed.set(false);
  }

  private startPollingIfOpen(status: GroupReservationStatus): void {
    if (status !== 'Open' || this.pollingStarted) {
      return;
    }

    this.pollingStarted = true;
    this.pollingSubscription = interval(SYNC_INTERVAL_MS)
      .pipe(exhaustMap(() => this.refreshGroup$()))
      .subscribe();
  }

  private stopPolling(): void {
    this.pollingSubscription?.unsubscribe();
    this.pollingSubscription = undefined;
  }

  private refreshGroup$(): Observable<unknown> {
    const invite = this.invite();
    if (!invite || this.refreshing() || this.groupStatus()?.status !== 'Open') {
      return EMPTY;
    }

    this.refreshing.set(true);
    return forkJoin({
      sessions: this.groupOrdersService.getSessions$(invite.GroupReservationId, { silent: true }),
      status: this.groupOrdersService.getStatus$(invite.GroupReservationId, { silent: true }),
    }).pipe(
      tap(({ sessions, status }) => {
        this.setGroupData(sessions, status);
        if (status.status !== 'Open') {
          this.stopPolling();
        }
      }),
      catchError(() => {
        this.refreshFailed.set(true);
        return EMPTY;
      }),
      finalize(() => this.refreshing.set(false))
    );
  }
}
