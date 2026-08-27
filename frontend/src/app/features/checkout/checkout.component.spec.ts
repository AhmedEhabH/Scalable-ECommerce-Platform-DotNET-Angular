import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { BehaviorSubject, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { OrderResponse } from '../../core/models/order.model';
import { CartService, CartState } from '../../core/services/cart.service';
import { OrderService } from '../../core/services/order.service';
import { CheckoutComponent } from './checkout.component';

describe('CheckoutComponent', () => {
  const cartState$ = new BehaviorSubject<CartState>({ items: [], totalItems: 0, subTotal: 0 });
  let orderService: { placeOrder: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    orderService = { placeOrder: vi.fn() };
    router = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      imports: [CheckoutComponent],
      providers: [
        { provide: CartService, useValue: { cartState$ } },
        { provide: OrderService, useValue: orderService },
        { provide: Router, useValue: router }
      ]
    }).overrideComponent(CheckoutComponent, { set: { template: '' } });
  });

  it('blocks submission and marks an invalid form as touched', () => {
    const component = TestBed.createComponent(CheckoutComponent).componentInstance;
    component.cartItems = [cartItem()];

    component.placeOrder();

    expect(orderService.placeOrder).not.toHaveBeenCalled();
    expect(component.checkoutForm.get('fullName')?.touched).toBe(true);
  });

  it('navigates to order success after a valid submission', () => {
    const response$ = new Subject<OrderResponse>();
    orderService.placeOrder.mockReturnValue(response$);
    const component = TestBed.createComponent(CheckoutComponent).componentInstance;
    component.cartItems = [cartItem()];
    component.checkoutForm.setValue(validFormValue());

    component.placeOrder();
    expect(component.submitting).toBe(true);
    response$.next({ success: true, data: { id: 'order-1' } } as OrderResponse);

    expect(router.navigate).toHaveBeenCalledWith(['/order-success'], { queryParams: { orderId: 'order-1' } });
  });

  it('surfaces an API error and allows retry', () => {
    const response$ = new Subject<OrderResponse>();
    orderService.placeOrder.mockReturnValue(response$);
    const component = TestBed.createComponent(CheckoutComponent).componentInstance;
    component.cartItems = [cartItem()];
    component.checkoutForm.setValue(validFormValue());

    component.placeOrder();
    response$.error({ error: { message: 'Insufficient stock' } });

    expect(component.error).toBe('Insufficient stock');
    expect(component.submitting).toBe(false);
  });

  function cartItem() {
    return {
      id: 'item-1', productId: 'product-1', productName: 'Keyboard', productSlug: 'keyboard',
      quantity: 1, unitPrice: 25, total: 25, isInStock: true
    };
  }

  function validFormValue() {
    return {
      fullName: 'Test User', email: 'test@example.com', phone: '123', street: '1 Main Street',
      city: 'Cairo', state: '', postalCode: '', country: 'Egypt', notes: ''
    };
  }
});
