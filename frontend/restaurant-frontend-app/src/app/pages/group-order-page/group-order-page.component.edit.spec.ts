import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { ToastService } from '@app/services/toast/toast.service';
import { GroupSessionsResponse } from '@app/models/group-orders/group-sessions-response.model';
import { GuestInvite } from '@app/models/reservations/guest-invite.model';
import { GroupOrderPageComponent } from './group-order-page.component';

const caesar = { id: 'dish1', name: 'Caesar Salad', price: 180 };
const tomYum = { id: 'dish2', name: 'Tom Yum', price: 220 };
const SESSIONS: GroupSessionsResponse = {
  participants: [
    {
      id: 'owner',
      type: 'Owner',
      name: 'Yevhenii',
      isSubmitted: true,
      total: 180,
      items: [{ dish: caesar, quantity: 1 }],
    },
    {
      id: 'andrii',
      type: 'Guest',
      name: 'Andrii',
      isSubmitted: true,
      total: 360,
      items: [{ dish: caesar, quantity: 2 }],
    },
    {
      id: 'olena',
      type: 'Guest',
      name: 'Olena',
      isSubmitted: false,
      total: 220,
      items: [{ dish: tomYum, quantity: 1 }],
    },
  ],
  total: 760,
};
const INVITE = {
  GroupReservationId: 'grp-1',
  LocationAddress: 'Rustaveli Avenue 48',
  MaxGuestCount: 5,
  StartTime: '2026-10-21T16:00:00Z',
  EndTime: '2026-10-21T17:30:00Z',
} as GuestInvite;

function buttonByLabel(root: ParentNode, label: string): HTMLButtonElement | undefined {
  return [...root.querySelectorAll('button')].find(
    (button) => button.getAttribute('aria-label') === label
  );
}

describe('Group order item edit preview', () => {
  let service: jasmine.SpyObj<GroupOrdersService>;

  function create() {
    TestBed.configureTestingModule({
      imports: [GroupOrderPageComponent],
      providers: [
        { provide: GroupOrdersService, useValue: service },
        {
          provide: ToastService,
          useValue: jasmine.createSpyObj('ToastService', ['success', 'error', 'info']),
        },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ reservationId: 'res-1' }) } },
        },
        { provide: Router, useValue: jasmine.createSpyObj('Router', ['navigate']) },
      ],
    });
    const fixture = TestBed.createComponent(GroupOrderPageComponent);
    fixture.detectChanges();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  beforeEach(() => {
    service = jasmine.createSpyObj<GroupOrdersService>('GroupOrdersService', [
      'getGroupInvite$',
      'getGroupInviteToken$',
      'getSessions$',
      'getOwnerSession$',
      'getOwnerSessionId$',
      'getStatus$',
      'submit$',
      'setSessionItemQuantity$',
      'removeSessionItem$',
    ]);
    service.getGroupInvite$.and.returnValue(of(INVITE));
    service.getGroupInviteToken$.and.returnValue(of('tok-1'));
    service.getSessions$.and.returnValue(of(structuredClone(SESSIONS)));
    service.getOwnerSession$.and.returnValue(of({ guestSessionId: 'owner', sessionToken: 'owner-token' }));
    service.getOwnerSessionId$.and.returnValue(of('owner'));
    service.getStatus$.and.returnValue(
      of({ currentParticipants: 3, maxCapacity: 5, status: 'Open' as const })
    );
  });

  it('increments the owner dish via PATCH with absolute quantity and re-renders from the snapshot', () => {
    const { fixture, element } = create();
    const updated = structuredClone(SESSIONS);
    updated.participants[0].items[0].quantity = 2;
    updated.participants[0].total = 360;
    service.setSessionItemQuantity$.and.returnValue(of(updated));

    buttonByLabel(element, 'Increase Caesar Salad quantity')?.click();
    fixture.detectChanges();

    expect(service.setSessionItemQuantity$).toHaveBeenCalledOnceWith('grp-1', 'owner', 'dish1', 2);
    const ownerCard = element.querySelector('app-participant-card')!;
    expect(ownerCard.querySelector('.qty')?.textContent?.trim()).toBe('2');
    expect(ownerCard.textContent).toContain('$360.00');
  });

  it('decrements the owner dish with absolute quantity current-1 when above one', () => {
    const base = structuredClone(SESSIONS);
    base.participants[0].items[0].quantity = 3;
    service.getSessions$.and.returnValue(of(base));
    service.setSessionItemQuantity$.and.returnValue(of(base));
    const { fixture, element } = create();

    buttonByLabel(element, 'Decrease Caesar Salad quantity')?.click();
    fixture.detectChanges();

    expect(service.setSessionItemQuantity$).toHaveBeenCalledOnceWith('grp-1', 'owner', 'dish1', 2);
  });

  it('lets the owner change a guest quantity (absolute current+1 and current-1)', () => {
    service.setSessionItemQuantity$.and.returnValue(of(structuredClone(SESSIONS)));
    const { fixture, element } = create();
    const guestCard = element.querySelectorAll('app-participant-card')[1];

    buttonByLabel(guestCard, 'Increase Caesar Salad quantity')?.click();
    fixture.detectChanges();
    expect(service.setSessionItemQuantity$).toHaveBeenCalledWith('grp-1', 'andrii', 'dish1', 3);

    buttonByLabel(guestCard, 'Decrease Caesar Salad quantity')?.click();
    fixture.detectChanges();
    expect(service.setSessionItemQuantity$).toHaveBeenCalledWith('grp-1', 'andrii', 'dish1', 1);
  });

  it('disables decrease at quantity one and never sends quantity below one', () => {
    const { element } = create();

    expect(buttonByLabel(element, 'Decrease Caesar Salad quantity')?.disabled).toBeTrue();
    expect(service.setSessionItemQuantity$).not.toHaveBeenCalled();
  });

  it('keeps the list unchanged and refetches when the quantity API call fails', () => {
    service.setSessionItemQuantity$.and.returnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    const { fixture, element } = create();
    service.getSessions$.calls.reset();

    buttonByLabel(element, 'Increase Caesar Salad quantity')?.click();
    fixture.detectChanges();

    expect(element.querySelector('app-participant-card .qty')?.textContent?.trim()).toBe('1');
    expect(service.getSessions$).toHaveBeenCalled();
  });

  it('deletes a guest dish through the API and uses the returned snapshot', () => {
    const afterDelete = structuredClone(SESSIONS);
    afterDelete.participants[1].items = [];
    afterDelete.participants[1].total = 0;
    service.removeSessionItem$.and.returnValue(of(afterDelete));
    const { fixture, element } = create();
    const guestCard = element.querySelectorAll('app-participant-card')[1];

    buttonByLabel(guestCard, 'Remove Caesar Salad')?.click();
    fixture.detectChanges();

    expect(service.removeSessionItem$).toHaveBeenCalledOnceWith('grp-1', 'andrii', 'dish1');
    expect(guestCard.textContent).toContain('No dishes yet');
    expect(element.querySelectorAll('app-participant-card').length).toBe(3);
  });

  it('shows item deletion before the dish name', () => {
    const { element } = create();

    const ownerCard = element.querySelector('app-participant-card')!;
    const removeButton = buttonByLabel(element, 'Remove Caesar Salad')!;
    expect(removeButton.compareDocumentPosition(ownerCard.querySelector('.name')!)).toBe(
      Node.DOCUMENT_POSITION_FOLLOWING
    );
  });

  it('submits the group order via the API and marks it as sent', () => {
    service.submit$.and.returnValue(of([{ dishId: 'dish1', quantity: 3 }]));
    const { fixture, element } = create();

    [...element.querySelectorAll('button')]
      .find((button) => button.textContent?.trim() === 'Submit Pre-Order')
      ?.click();
    fixture.detectChanges();
    [...element.querySelectorAll('button')]
      .find((button) => button.textContent?.trim() === 'Submit')
      ?.click();
    fixture.detectChanges();

    expect(service.submit$).toHaveBeenCalledOnceWith('grp-1', 'res-1');
    expect(element.textContent).toContain('Sent to Restaurant');
  });

  it('navigates to owner guest session route with invite token', () => {
    localStorage.clear();
    const { element } = create();
    [...element.querySelectorAll('button')]
      .find((button) => button.textContent?.trim() === 'Edit my Pre-Order')
      ?.click();

    expect(TestBed.inject(Router).navigate).toHaveBeenCalledWith(['/join', 'tok-1']);
    expect(localStorage.getItem('guestPreorder:tok-1:guestSessionId')).toBe('owner');
    expect(localStorage.getItem('guestPreorder:tok-1:guestSessionToken')).toBe('owner-token');
  });
});
