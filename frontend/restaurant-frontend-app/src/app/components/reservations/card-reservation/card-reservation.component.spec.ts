import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { CardReservationComponent } from './card-reservation.component';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { ReservationCustomer, ReservationStatusCustomer } from '@app/models/reservations/reservation.model';

const RESERVATION: ReservationCustomer = {
  id: 'res-1',
  tableId: 'table-1',
  startTime: new Date('2026-10-21T16:00:00Z'),
  endTime: new Date('2026-10-21T17:30:00Z'),
  guestCount: 2,
  status: ReservationStatusCustomer.Reserved,
  restaurantName: 'Green & Tasty',
  locationAddress: 'Rustaveli Avenue 48',
  waiterId: 'waiter-1',
};

describe('CardReservationComponent', () => {
  let component: CardReservationComponent;
  let fixture: ComponentFixture<CardReservationComponent>;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [{ provide: API_BASE_URL, useValue: 'https://test.api' }, provideRouter([])],
      imports: [CardReservationComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CardReservationComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    // r — обов'язковий input: без нього компонент падає з NG0950
    fixture.componentRef.setInput('r', RESERVATION);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('navigates to group pre-order page for group reservation', () => {
    spyOn(router, 'navigate').and.resolveTo(true);

    component.onPreOrder();

    expect(router.navigate).toHaveBeenCalledOnceWith(['/reservations', 'res-1', 'group-order']);
  });

  it('navigates to menu pre-order for non-group reservation', () => {
    spyOn(router, 'navigate').and.resolveTo(true);
    fixture.componentRef.setInput('r', {
      ...RESERVATION,
      guestCount: 1,
      guestInviteLinkAvailable: false,
    });
    fixture.detectChanges();

    component.onPreOrder();

    expect(router.navigate).toHaveBeenCalledOnceWith(['/menu'], {
      queryParams: jasmine.objectContaining({
        preOrder: 'true',
        reservationId: 'res-1',
      }),
    });
  });
});
