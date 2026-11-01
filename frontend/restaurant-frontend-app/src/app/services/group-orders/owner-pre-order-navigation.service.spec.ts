import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { OwnerPreOrderNavigationService } from './owner-pre-order-navigation.service';

describe('OwnerPreOrderNavigationService', () => {
  let service: OwnerPreOrderNavigationService;
  let groupOrders: jasmine.SpyObj<GroupOrdersService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    localStorage.clear();
    groupOrders = jasmine.createSpyObj<GroupOrdersService>('GroupOrdersService', [
      'getGroupInviteToken$',
      'getOwnerSession$',
    ]);
    groupOrders.getGroupInviteToken$.and.returnValue(of('tok-1'));
    groupOrders.getOwnerSession$.and.returnValue(of({ guestSessionId: 'owner', sessionToken: 'owner-token' }));
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        { provide: GroupOrdersService, useValue: groupOrders },
        { provide: Router, useValue: router },
      ],
    });
    service = TestBed.inject(OwnerPreOrderNavigationService);
  });

  afterEach(() => localStorage.clear());

  it('stores the owner session and reservation, then navigates to /join/:token', () => {
    service.openOwnerPreOrder('res-1', () => 'Yevhenii');

    expect(localStorage.getItem('guestPreorder:tok-1:guestSessionId')).toBe('owner');
    expect(localStorage.getItem('guestPreorder:tok-1:guestSessionToken')).toBe('owner-token');
    expect(localStorage.getItem('guestPreorder:tok-1:guestName')).toBe('Yevhenii');
    expect(localStorage.getItem('guestPreorder:tok-1:ownerReservationId')).toBe('res-1');
    expect(router.navigate).toHaveBeenCalledOnceWith(['/join', 'tok-1']);
  });

  it('falls back to "Owner" when no name can be resolved', () => {
    service.openOwnerPreOrder('res-1');

    expect(localStorage.getItem('guestPreorder:tok-1:guestName')).toBe('Owner');
  });

  it('does nothing when the owner session has no token', () => {
    groupOrders.getOwnerSession$.and.returnValue(of({ guestSessionId: 'owner' }));

    service.openOwnerPreOrder('res-1');

    expect(router.navigate).not.toHaveBeenCalled();
    expect(localStorage.length).toBe(0);
  });

  it('does nothing when a request fails', () => {
    groupOrders.getOwnerSession$.and.returnValue(throwError(() => new Error('boom')));

    service.openOwnerPreOrder('res-1');

    expect(router.navigate).not.toHaveBeenCalled();
  });
});
