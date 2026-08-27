import { TestBed } from '@angular/core/testing';
import { BehaviorSubject, of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { CartResponse } from '../../core/models/cart.model';
import { CartService, CartState } from '../../core/services/cart.service';
import { ToastService } from '../../shared/components/toast/toast.service';
import { CartComponent } from './cart.component';

describe('CartComponent', () => {
  const cartState$ = new BehaviorSubject<CartState>({ items: [], totalItems: 0, subTotal: 0 });
  let cartService: {
    cartState$: BehaviorSubject<CartState>;
    updateCartItem: ReturnType<typeof vi.fn>;
    removeCartItem: ReturnType<typeof vi.fn>;
    getCart: ReturnType<typeof vi.fn>;
  };
  let toastService: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    cartState$.next({ items: [], totalItems: 0, subTotal: 0 });
    cartService = {
      cartState$,
      updateCartItem: vi.fn(),
      removeCartItem: vi.fn(),
      getCart: vi.fn(() => of({ success: true } as CartResponse))
    };
    toastService = { success: vi.fn(), error: vi.fn() };

    TestBed.configureTestingModule({
      imports: [CartComponent],
      providers: [
        { provide: CartService, useValue: cartService },
        { provide: ToastService, useValue: toastService }
      ]
    }).overrideComponent(CartComponent, { set: { template: '' } });
  });

  it('updates quantity once while an item request is in flight', () => {
    const response$ = new Subject<CartResponse>();
    cartService.updateCartItem.mockReturnValue(response$);
    const component = TestBed.createComponent(CartComponent).componentInstance;

    component.updateQuantity('item-1', 2);
    component.updateQuantity('item-1', 3);

    expect(cartService.updateCartItem).toHaveBeenCalledTimes(1);
    expect(component.isItemUpdating('item-1')).toBe(true);

    response$.next({ success: true });
    expect(component.isItemUpdating('item-1')).toBe(false);
    expect(toastService.success).toHaveBeenCalledWith('Cart updated');
  });

  it('removes an item and reports the product name', () => {
    cartService.removeCartItem.mockReturnValue(of({ success: true } as CartResponse));
    const component = TestBed.createComponent(CartComponent).componentInstance;
    component.cartItems = [{
      id: 'item-1',
      productId: 'product-1',
      productName: 'Keyboard',
      productSlug: 'keyboard',
      quantity: 1,
      unitPrice: 25,
      total: 25,
      isInStock: true
    }];

    component.removeItem('item-1');

    expect(cartService.removeCartItem).toHaveBeenCalledWith('item-1');
    expect(toastService.success).toHaveBeenCalledWith('Keyboard removed from cart');
  });
});
