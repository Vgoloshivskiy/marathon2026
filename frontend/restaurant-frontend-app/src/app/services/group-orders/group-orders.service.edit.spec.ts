import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { GroupOrdersService } from './group-orders.service';

describe('GroupOrdersService item editing', () => {
  let http: HttpTestingController;
  let service: GroupOrdersService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'https://api.test' },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    service = TestBed.inject(GroupOrdersService);
  });

  afterEach(() => http.verify());

  it('removeSessionItem$ sends DELETE /guest/groups/{gid}/sessions/{sid}/items/{dishId}', () => {
    service.removeSessionItem$('grp-1', 'gs-2', 'dish-3').subscribe();

    const req = http.expectOne('https://api.test/guest/groups/grp-1/sessions/gs-2/items/dish-3');
    expect(req.request.method).toBe('DELETE');
    req.flush({ participants: [], total: 0 });
  });

  it('setSessionItemQuantity$ sends PATCH with the absolute quantity and returns the snapshot', () => {
    let result: unknown;
    service.setSessionItemQuantity$('grp-1', 'gs-2', 'dish-3', 4).subscribe((response) => (result = response));

    const req = http.expectOne('https://api.test/guest/groups/grp-1/sessions/gs-2/items/dish-3');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ quantity: 4 });
    req.flush({ participants: [], total: 0 });
    expect(result).toEqual({ participants: [], total: 0 });
  });

  it('setSessionItemQuantity$ url-encodes path segments', () => {
    service.setSessionItemQuantity$('g/1', 's 2', 'd#3', 1).subscribe();

    const req = http.expectOne('https://api.test/guest/groups/g%2F1/sessions/s%202/items/d%233');
    expect(req.request.method).toBe('PATCH');
    req.flush({ participants: [], total: 0 });
  });

  it('setSessionItemQuantity$ propagates a 404 error', () => {
    let status = 0;
    service.setSessionItemQuantity$('grp-1', 'gs-2', 'dish-3', 2).subscribe({
      error: (error: { status: number }) => (status = error.status),
    });

    http
      .expectOne('https://api.test/guest/groups/grp-1/sessions/gs-2/items/dish-3')
      .flush(null, { status: 404, statusText: 'Not Found' });
    expect(status).toBe(404);
  });
});
