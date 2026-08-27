import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { CartService } from '../../../../core/services/cart.service';
import { WishlistService } from '../../../../core/services/wishlist.service';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { Product } from '../../models/product.model';
import { ProductCardComponent } from './product-card.component';

describe('ProductCardComponent', () => {
  let cartService: { addToCart: ReturnType<typeof vi.fn> };
  let wishlistService: { isInWishlist: ReturnType<typeof vi.fn>; toggleWishlist: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    cartService = { addToCart: vi.fn(() => of({ success: true })) };
    wishlistService = { isInWishlist: vi.fn(() => false), toggleWishlist: vi.fn() };

    TestBed.configureTestingModule({
      imports: [ProductCardComponent],
      providers: [
        { provide: CartService, useValue: cartService },
        { provide: WishlistService, useValue: wishlistService },
        { provide: ToastService, useValue: { success: vi.fn(), error: vi.fn() } }
      ]
    }).overrideComponent(ProductCardComponent, { set: { template: '' } });
  });

  it('prevents link navigation and emits one add-to-cart request', () => {
    const component = TestBed.createComponent(ProductCardComponent).componentInstance;
    component.product = product();
    const event = { preventDefault: vi.fn(), stopPropagation: vi.fn() } as unknown as Event;

    component.onAddToCart(event);

    expect(event.preventDefault).toHaveBeenCalled();
    expect(event.stopPropagation).toHaveBeenCalled();
    expect(cartService.addToCart).toHaveBeenCalledWith({ productId: 'product-1', quantity: 1 });
  });

  it('prevents link navigation and delegates wishlist toggling', () => {
    const component = TestBed.createComponent(ProductCardComponent).componentInstance;
    component.product = product();
    const event = { preventDefault: vi.fn(), stopPropagation: vi.fn() } as unknown as Event;

    component.onToggleWishlist(event);

    expect(event.preventDefault).toHaveBeenCalled();
    expect(event.stopPropagation).toHaveBeenCalled();
    expect(wishlistService.toggleWishlist).toHaveBeenCalledWith(component.product);
  });

  function product(): Product {
    return {
      id: 'product-1', vendorId: 'vendor-1', categoryId: 'category-1', name: 'Keyboard', slug: 'keyboard',
      description: null, price: 25, compareAtPrice: null, sku: 'KEY-1', stockQuantity: 10,
      lowStockThreshold: 2, isFeatured: false, isActive: true, reviewCount: 0, averageRating: 0,
      isInStock: true, isLowStock: false, hasDiscount: false, discountPercentage: 0,
      mainImageUrl: null, images: [], createdAt: new Date().toISOString(), updatedAt: new Date().toISOString()
    };
  }
});
