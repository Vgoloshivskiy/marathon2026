import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { GuestInviteService } from '@app/services/reservation/guest-invite.service';
import { GuestInvite } from '@app/models/reservations/guest-invite.model';
import { GuestMenuResponse, GuestSessionsService } from '@app/services/guest-sessions/guest-sessions.service';
import { GuestPreorderStore } from './guest-preorder.store';

describe('GuestPreorderStore submit redirect', () => {
  let store: GuestPreorderStore;
  let router: jasmine.SpyObj<Router>;
  let sessions: jasmine.SpyObj<GuestSessionsService>;

  async function initWithCart(): Promise<void> {
    store.initContext('tok-1');
    await Promise.resolve();
    store.cartItems.set([{ dishId: 'd1', title: 'Dish', price: 10, qty: 1 }]);
  }

  beforeEach(() => {
    localStorage.clear();
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    sessions = jasmine.createSpyObj<GuestSessionsService>('GuestSessionsService', ['submit', 'getMenu']);
    sessions.submit.and.returnValue(of({}));
    sessions.getMenu.and.returnValue(of({ dishes: {} } as GuestMenuResponse));
    const invites = jasmine.createSpyObj<GuestInviteService>('GuestInviteService', ['getInvite$']);
    invites.getInvite$.and.returnValue(of({} as GuestInvite));

    TestBed.configureTestingModule({
      providers: [
        { provide: Router, useValue: router },
        { provide: GuestSessionsService, useValue: sessions },
        { provide: GuestInviteService, useValue: invites },
      ],
    });
    store = TestBed.inject(GuestPreorderStore);
    localStorage.setItem('guestPreorder:tok-1:guestSessionId', 's1');
    localStorage.setItem('guestPreorder:tok-1:guestSessionToken', 't1');
  });

  afterEach(() => localStorage.clear());

  it('keeps the confirmed state and does not navigate for a regular guest', async () => {
    await initWithCart();

    await store.submitPreorder();

    expect(store.status()).toBe('submitted');
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('redirects the group owner to the read-only group order preview', async () => {
    localStorage.setItem('guestPreorder:tok-1:ownerReservationId', 'res-1');
    await initWithCart();

    await store.submitPreorder();

    expect(router.navigate).toHaveBeenCalledOnceWith(['/reservations', 'res-1', 'group-order'], {
      queryParams: { readonly: 'true' },
    });
    expect(store.status()).toBe('active');
    expect(store.ownerReservationId()).toBeNull();
    expect(localStorage.getItem('guestPreorder:tok-1:ownerReservationId')).toBeNull();
  });
});
