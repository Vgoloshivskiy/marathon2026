import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
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
      'getSessions$',
      'getOwnerSessionId$',
      'getStatus$',
      'submit$',
      'setSessionItemQuantity$',
      'removeSessionItem$',
    ]);
    service.getGroupInvite$.and.returnValue(of(INVITE));
    service.getSessions$.and.returnValue(of(structuredClone(SESSIONS)));
    service.getOwnerSessionId$.and.returnValue(of('owner'));
    service.getStatus$.and.returnValue(
      of({ currentParticipants: 3, maxCapacity: 5, status: 'Open' as const })
    );
  });

  it('shows the owner edit link and saves quantity changes through the API', () => {
    const { fixture, element } = create();
    const updated = structuredClone(SESSIONS);
    updated.participants[0].items[0].quantity = 2;
    updated.participants[0].total = 360;
    service.setSessionItemQuantity$.and.returnValue(of(updated));

    expect(
      [...element.querySelectorAll('button')].find(
        (button) => button.textContent?.trim() === 'Edit my Pre-Order'
      )
    ).toBeDefined();
    buttonByLabel(element, 'Increase Caesar Salad quantity')?.click();
    fixture.detectChanges();

    expect(service.setSessionItemQuantity$).toHaveBeenCalledWith('grp-1', 'owner', 'dish1', 2);
    const ownerCard = element.querySelector('app-participant-card')!;
    expect(ownerCard.querySelector('.qty')?.textContent?.trim()).toBe('2');
    expect(ownerCard.textContent).toContain('$360.00');
    expect(element.textContent).not.toContain('preview only');
  });

  it('lets the owner update a guest order while keeping its submitted state', () => {
    const { fixture, element } = create();
    const updated = structuredClone(SESSIONS);
    updated.participants[1].items[0].quantity = 3;
    updated.participants[1].total = 540;
    service.setSessionItemQuantity$.and.returnValue(of(updated));
    const guestCard = element.querySelectorAll('app-participant-card')[1];

    buttonByLabel(guestCard, 'Increase Caesar Salad quantity')?.click();
    fixture.detectChanges();

    expect(service.setSessionItemQuantity$).toHaveBeenCalledWith('grp-1', 'andrii', 'dish1', 3);
    expect(guestCard.querySelector('.qty')?.textContent?.trim()).toBe('3');
    expect(guestCard.textContent).toContain('Submitted');
    expect(element.textContent).toContain('Caesar Salad × 4');
  });

  it('removes an item through the API and shows the returned participants', () => {
    const { fixture, element } = create();
    const updated = structuredClone(SESSIONS);
    updated.participants[0].items = [];
    updated.participants[0].total = 0;
    service.removeSessionItem$.and.returnValue(of(updated));

    buttonByLabel(element, 'Remove Caesar Salad')?.click();
    fixture.detectChanges();

    expect(service.removeSessionItem$).toHaveBeenCalledWith('grp-1', 'owner', 'dish1');
    const ownerCard = element.querySelector('app-participant-card')!;
    expect(ownerCard.textContent).toContain('No dishes yet');
    expect(element.querySelectorAll('app-participant-card').length).toBe(3);
  });
  it('navigates to the existing menu with preorder and group return parameters', () => {
    const { element } = create();
    [...element.querySelectorAll('button')]
      .find((button) => button.textContent?.trim() === 'Edit my Pre-Order')
      ?.click();

    expect(TestBed.inject(Router).navigate).toHaveBeenCalledWith(
      ['/menu'],
      jasmine.objectContaining({
        queryParams: jasmine.objectContaining({
          preOrder: 'true',
          reservationId: 'res-1',
          groupOrderReservationId: 'res-1',
        }),
      })
    );
  });
});
