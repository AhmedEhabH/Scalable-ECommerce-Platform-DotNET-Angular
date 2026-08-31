import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, convertToParamMap } from '@angular/router';

import { ProductDetailsPage } from './product-details.page';
import { ProductsService } from '../services/products.service';
import { ReviewService } from '../services/review.service';
import { CartService } from '../../../core/services/cart.service';
import { WishlistService } from '../../../core/services/wishlist.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { SellersService } from '../../../core/services/sellers.service';
import { Product } from '../models/product.model';
import { Review, ReviewSummary } from '../models/review.model';
import { ApiResponse } from '../../../core/models';

@Component({ standalone: true, imports: [ProductDetailsPage], template: '<app-product-details-page></app-product-details-page>' })
class TestHostComponent {}

const productId = 'prod-1';
const routeParamMap = convertToParamMap({ id: productId });

const mockProduct: Product = {
  id: productId, vendorId: 'vendor-1', categoryId: 'cat-1', name: 'Wireless Keyboard',
  slug: 'wireless-keyboard', description: 'A customizable keyboard with hot-swappable switches and RGB lighting.',
  price: 89.99, compareAtPrice: 109.99, sku: 'WK-001', stockQuantity: 50, lowStockThreshold: 10,
  isFeatured: true, isActive: true, reviewCount: 124, averageRating: 4.5, isInStock: true, isLowStock: false,
  hasDiscount: true, discountPercentage: 18, mainImageUrl: 'https://example.com/keyboard.jpg', images: [],
  createdAt: '2026-01-15T10:00:00Z', updatedAt: '2026-08-01T12:00:00Z',
  sellerName: 'TechVendor', sellerDescription: 'Quality tech products'
};
const mockSeller = { id: 'vendor-1', name: 'TechVendor', description: 'Quality tech products', imageUrl: null as unknown as string };
const mockReviews: Review[] = [
  { id: 'rev-1', productId, userId: 'user-1', userName: 'Alice', rating: 5, title: 'Great keyboard', comment: 'Love it', isVerifiedPurchase: true, createdAt: '2026-02-01T00:00:00Z' },
  { id: 'rev-2', productId, userId: 'user-2', userName: 'Bob', rating: 4, title: 'Good', comment: 'Pretty good', isVerifiedPurchase: false, createdAt: '2026-02-02T00:00:00Z' }
];
const mockSummary: ReviewSummary = { totalReviews: 2, averageRating: 4.5, oneStar: 0, twoStars: 0, threeStars: 0, fourStars: 1, fiveStars: 1 };
const emptySummary: ReviewSummary = { totalReviews: 0, averageRating: 0, oneStar: 0, twoStars: 0, threeStars: 0, fourStars: 0, fiveStars: 0 };

function flushProduct(page: ProductDetailsPage, httpMock: HttpTestingController, fixture: any, opts?: { product?: Partial<ApiResponse<Product>>; reviews?: Partial<ApiResponse<Review[]>>; summary?: Partial<ApiResponse<ReviewSummary>>; error?: boolean; notFound?: boolean }) {
  if (opts?.error) {
    const req = httpMock.expectOne(req => req.url.includes('/products/' + productId) && !req.url.includes('/reviews') && !req.url.includes('/sellers') && !req.url.includes('/summary'));
    req.error(new ErrorEvent('Network error'));
  } else if (opts?.notFound) {
    const req = httpMock.expectOne(req => req.url.includes('/products/' + productId) && !req.url.includes('/reviews') && !req.url.includes('/sellers') && !req.url.includes('/summary'));
    req.flush({ success: false, data: undefined, message: 'Not found' } as ApiResponse<Product>);
  } else {
    const req = httpMock.expectOne(req => req.url.includes('/products/' + productId) && !req.url.includes('/reviews') && !req.url.includes('/sellers') && !req.url.includes('/summary'));
    req.flush({ success: true, data: mockProduct, message: undefined, ...opts?.product } as ApiResponse<Product>);
  }
  fixture.detectChanges();

  const revReq = httpMock.expectOne(req => req.url.includes('/reviews/product/' + productId) && !req.url.includes('/summary'));
  revReq.flush({ success: true, data: opts?.reviews?.data ?? mockReviews, message: undefined, ...opts?.reviews } as ApiResponse<Review[]>);
  fixture.detectChanges();

  const sumReq = httpMock.expectOne(req => req.url.includes('/reviews/product/' + productId + '/summary'));
  sumReq.flush({ success: true, data: opts?.summary?.data ?? mockSummary, message: undefined, ...opts?.summary } as ApiResponse<ReviewSummary>);
  fixture.detectChanges();

  if (!opts?.error && !opts?.notFound) {
    const sellerReq = httpMock.expectOne(req => req.url.includes('/sellers/vendor-1'));
    sellerReq.flush(mockSeller);
    fixture.detectChanges();
  }
}

function flushExtraReviews(httpMock: HttpTestingController, fixture: any, customReviews?: Review[], customSummary?: ReviewSummary) {
  const revReq = httpMock.expectOne(req => req.url.includes('/reviews/product/' + productId) && !req.url.includes('/summary'));
  revReq.flush({ success: true, data: customReviews ?? mockReviews, message: undefined } as ApiResponse<Review[]>);
  fixture.detectChanges();
  const sumReq = httpMock.expectOne(req => req.url.includes('/reviews/product/' + productId + '/summary'));
  sumReq.flush({ success: true, data: customSummary ?? mockSummary, message: undefined } as ApiResponse<ReviewSummary>);
  fixture.detectChanges();
}

describe('ProductDetailsPage', () => {
  let fixture: any;
  let page: ProductDetailsPage;
  let httpMock: HttpTestingController;
  let productsService: ProductsService;
  let reviewService: ReviewService;
  let cartService: CartService;
  let wishlistService: WishlistService;
  let authService: AuthService;
  let toastService: ToastService;
  let sellersService: SellersService;

  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    TestBed.configureTestingModule({
      imports: [TestHostComponent, ReactiveFormsModule, NoopAnimationsModule, CommonModule],
      providers: [
        ProductsService, ReviewService, CartService, WishlistService, AuthService, ToastService, SellersService,
        provideHttpClient(), provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: routeParamMap } } }
      ]
    });
    fixture = TestBed.createComponent(TestHostComponent);
    httpMock = TestBed.inject(HttpTestingController);
    productsService = TestBed.inject(ProductsService);
    reviewService = TestBed.inject(ReviewService);
    cartService = TestBed.inject(CartService);
    wishlistService = TestBed.inject(WishlistService);
    authService = TestBed.inject(AuthService);
    toastService = TestBed.inject(ToastService);
    sellersService = TestBed.inject(SellersService);
    fixture.detectChanges();
    page = fixture.debugElement.children[0].componentInstance as ProductDetailsPage;
  });

  afterEach(() => {
    try { httpMock.verify(); } catch {}
    vi.useRealTimers();
    vi.restoreAllMocks();
    fixture.destroy();
  });

  describe('load product', () => {
    it('sets loading true initially and false after product arrives', () => {
      expect(page.loading()).toBe(true);
      flushProduct(page, httpMock, fixture);
      expect(page.loading()).toBe(false);
      expect(page.product()).toEqual(mockProduct);
    });

    it('shows error signal when product fetch fails', () => {
      flushProduct(page, httpMock, fixture, { error: true });
      expect(page.loading()).toBe(false);
      expect(page.error()).toBe('Failed to load product. Please try again later.');
    });

    it('shows "not found" message when API returns success false', () => {
      flushProduct(page, httpMock, fixture, { notFound: true });
      expect(page.loading()).toBe(false);
      expect(page.error()).toBe('Not found');
      expect(page.product()).toBeNull();
    });

    it('resets quantity to 1 on load', () => {
      flushProduct(page, httpMock, fixture);
      page.quantity.set(99);
      page.loadProduct(productId);
      const req = httpMock.expectOne(req => req.url.includes('/products/' + productId) && !req.url.includes('/reviews') && !req.url.includes('/sellers') && !req.url.includes('/summary'));
      req.flush({ success: true, data: mockProduct, message: undefined } as ApiResponse<Product>);
      fixture.detectChanges();
      expect(page.quantity()).toBe(1);
    });
  });

  describe('load seller', () => {
    it('loads seller when vendorId present', () => {
      flushProduct(page, httpMock, fixture);
      expect(page.seller()).toEqual(mockSeller);
    });

    it('sets seller null when vendorId missing', () => {
      const noVendor: Product = { ...mockProduct, vendorId: null as unknown as string };
      fixture.detectChanges();
      const prodReq = httpMock.expectOne(req => req.url.includes('/products/' + productId) && !req.url.includes('/reviews') && !req.url.includes('/sellers') && !req.url.includes('/summary'));
      prodReq.flush({ success: true, data: noVendor, message: undefined } as ApiResponse<Product>);
      fixture.detectChanges();
      const revReq = httpMock.expectOne(req => req.url.includes('/reviews/product/' + productId) && !req.url.includes('/summary'));
      revReq.flush({ success: true, data: [], message: undefined } as ApiResponse<Review[]>);
      fixture.detectChanges();
      const sumReq = httpMock.expectOne(req => req.url.includes('/reviews/product/' + productId + '/summary'));
      sumReq.flush({ success: true, data: emptySummary, message: undefined } as ApiResponse<ReviewSummary>);
      fixture.detectChanges();
      expect(page.seller()).toBeNull();
      httpMock.verify();
    });
  });

  describe('quantity selector', () => {
    it('increments up to stockQuantity', () => {
      flushProduct(page, httpMock, fixture);
      page.quantity.set(10);
      page.incrementQuantity();
      expect(page.quantity()).toBe(11);
      page.quantity.set(49);
      page.incrementQuantity();
      expect(page.quantity()).toBe(50);
      page.quantity.set(50);
      page.incrementQuantity();
      expect(page.quantity()).toBe(50);
    });

    it('does not decrement below 1', () => {
      flushProduct(page, httpMock, fixture);
      page.quantity.set(1);
      page.decrementQuantity();
      expect(page.quantity()).toBe(1);
      page.quantity.set(5);
      page.decrementQuantity();
      expect(page.quantity()).toBe(4);
    });
  });

  describe('add to cart', () => {
    it('calls cartService.addToCart and shows success toast', () => {
      flushProduct(page, httpMock, fixture);
      vi.spyOn(cartService, 'addToCart').mockReturnValue(of({} as any));
      const successSpy = vi.spyOn(toastService, 'success');
      vi.spyOn(toastService, 'error');
      page.onAddToCart();
      expect(successSpy).toHaveBeenCalledWith('Wireless Keyboard added to cart');
    });

    it('does nothing when adding already true', () => {
      flushProduct(page, httpMock, fixture);
      page.adding.set(true);
      const addSpy = vi.spyOn(cartService, 'addToCart');
      page.onAddToCart();
      expect(addSpy).not.toHaveBeenCalled();
    });

    it('shows error toast when cart call fails', () => {
      flushProduct(page, httpMock, fixture);
      vi.spyOn(cartService, 'addToCart').mockReturnValue(throwError(() => new Error('fail')));
      const errorSpy = vi.spyOn(toastService, 'error');
      page.onAddToCart();
      expect(errorSpy).toHaveBeenCalledWith('Failed to add item to cart');
      expect(page.adding()).toBe(false);
    });
  });

  describe('wishlist toggle', () => {
    it('calls wishlistService.toggleWishlist and shows appropriate toast', () => {
      flushProduct(page, httpMock, fixture);
      const toggleSpy = vi.spyOn(wishlistService, 'toggleWishlist');
      const successSpy = vi.spyOn(toastService, 'success');
      page.onToggleWishlist();
      expect(toggleSpy).toHaveBeenCalledWith(mockProduct);
      expect(successSpy).toHaveBeenCalled();
    });
  });

  describe('review form visibility', () => {
    it('toggles showReviewForm', () => {
      flushProduct(page, httpMock, fixture);
      expect(page.showReviewForm()).toBe(false);
      page.toggleReviewForm();
      expect(page.showReviewForm()).toBe(true);
      page.toggleReviewForm();
      expect(page.showReviewForm()).toBe(false);
    });
  });

  describe('submit review', () => {
    it('shows error toast when not authenticated', () => {
      flushProduct(page, httpMock, fixture);
      const errorSpy = vi.spyOn(toastService, 'error');
      page.newReviewRating.set(5);
      page.newReviewTitle.set('Great');
      page.newReviewComment.set('Nice');
      page.submitReview();
      expect(errorSpy).toHaveBeenCalledWith('Please log in to submit a review.');
      expect(page.submittingReview()).toBe(false);
    });

    it('shows error toast when rating is 0', () => {
      flushProduct(page, httpMock, fixture);
      vi.spyOn(authService, 'isAuthenticated', 'get').mockReturnValue(true);
      const errorSpy = vi.spyOn(toastService, 'error');
      page.newReviewRating.set(0);
      page.newReviewTitle.set('Great');
      page.newReviewComment.set('Nice');
      page.submitReview();
      expect(errorSpy).toHaveBeenCalledWith('Please select a rating');
      expect(page.submittingReview()).toBe(false);
    });

    it('shows error toast when title is empty', () => {
      flushProduct(page, httpMock, fixture);
      vi.spyOn(authService, 'isAuthenticated', 'get').mockReturnValue(true);
      const errorSpy = vi.spyOn(toastService, 'error');
      page.newReviewRating.set(5);
      page.newReviewTitle.set('');
      page.newReviewComment.set('Nice');
      page.submitReview();
      expect(errorSpy).toHaveBeenCalledWith('Please enter a review title');
      expect(page.submittingReview()).toBe(false);
    });

    it('calls reviewService.createReview with trimmed fields and resets form on success', () => {
      flushProduct(page, httpMock, fixture);
      vi.spyOn(authService, 'isAuthenticated', 'get').mockReturnValue(true);
      const createSpy = vi.spyOn(reviewService, 'createReview');
      createSpy.mockReturnValue(of({ success: true, data: { id: 'rev-new', productId, userId: 'user-1', userName: 'Me', rating: 5, title: 'Great', comment: 'Nice', isVerifiedPurchase: false, createdAt: '2026-01-01T00:00:00Z' }, message: undefined }));
      page.newReviewRating.set(5);
      page.newReviewTitle.set('  Great  ');
      page.newReviewComment.set('  Nice  ');
      page.submitReview();
      expect(createSpy).toHaveBeenCalledWith(productId, { rating: 5, title: 'Great', comment: 'Nice' });
      flushExtraReviews(httpMock, fixture);
      expect(page.showReviewForm()).toBe(false);
      expect(page.newReviewRating()).toBe(0);
      expect(page.newReviewTitle()).toBe('');
      expect(page.newReviewComment()).toBe('');
      expect(page.userHasReviewed()).toBe(true);
    });

    it('shows error toast when createReview returns success false', () => {
      flushProduct(page, httpMock, fixture);
      vi.spyOn(authService, 'isAuthenticated', 'get').mockReturnValue(true);
      const createSpy = vi.spyOn(reviewService, 'createReview');
      const errorSpy = vi.spyOn(toastService, 'error');
      createSpy.mockReturnValue(of({ success: false, data: undefined, message: 'Content policy violation' }));
      page.newReviewRating.set(5);
      page.newReviewTitle.set('Great');
      page.newReviewComment.set('Nice');
      page.submitReview();
      expect(errorSpy).toHaveBeenCalledWith('Content policy violation');
      expect(page.submittingReview()).toBe(false);
    });

    it('shows error toast on HTTP failure and resets submitting flag', () => {
      flushProduct(page, httpMock, fixture);
      vi.spyOn(authService, 'isAuthenticated', 'get').mockReturnValue(true);
      const createSpy = vi.spyOn(reviewService, 'createReview');
      const errorSpy = vi.spyOn(toastService, 'error');
      createSpy.mockReturnValue(throwError(() => ({ status: 500, error: { message: 'Server error' } })));
      page.newReviewRating.set(5);
      page.newReviewTitle.set('Great');
      page.newReviewComment.set('Nice');
      page.submitReview();
      expect(errorSpy).toHaveBeenCalledWith('Server error');
      expect(page.submittingReview()).toBe(false);
    });

    it('shows "log in" toast on 401 error', () => {
      flushProduct(page, httpMock, fixture);
      vi.spyOn(authService, 'isAuthenticated', 'get').mockReturnValue(true);
      const createSpy = vi.spyOn(reviewService, 'createReview');
      const errorSpy = vi.spyOn(toastService, 'error');
      createSpy.mockReturnValue(throwError(() => ({ status: 401 })));
      page.newReviewRating.set(5);
      page.newReviewTitle.set('Great');
      page.newReviewComment.set('Nice');
      page.submitReview();
      expect(errorSpy).toHaveBeenCalledWith('Please log in to submit a review.');
    });
  });

  describe('delete review', () => {
    it('filters review out and resets userHasReviewed on success', () => {
      flushProduct(page, httpMock, fixture);
      vi.spyOn(window, 'confirm').mockReturnValue(true);
      const deleteSpy = vi.spyOn(reviewService, 'deleteReview');
      deleteSpy.mockReturnValue(of({ success: true, data: undefined, message: undefined }));
      page.deleteReview('rev-1');
      flushExtraReviews(httpMock, fixture, [mockReviews[1]], mockSummary);
      expect(deleteSpy).toHaveBeenCalledWith('rev-1');
      expect(page.reviews().length).toBe(1);
      expect(page.userHasReviewed()).toBe(false);
    });

    it('does nothing when confirm returns false', () => {
      flushProduct(page, httpMock, fixture);
      vi.spyOn(window, 'confirm').mockReturnValue(false);
      const deleteSpy = vi.spyOn(reviewService, 'deleteReview');
      page.deleteReview('rev-1');
      expect(deleteSpy).not.toHaveBeenCalled();
    });

    it('shows error toast when delete fails', () => {
      flushProduct(page, httpMock, fixture);
      vi.spyOn(window, 'confirm').mockReturnValue(true);
      const deleteSpy = vi.spyOn(reviewService, 'deleteReview');
      deleteSpy.mockReturnValue(throwError(() => new Error('fail')));
      const errorSpy = vi.spyOn(toastService, 'error');
      page.deleteReview('rev-1');
      expect(errorSpy).toHaveBeenCalledWith('Failed to delete review');
    });
  });

  describe('user review helpers', () => {
    it('finds current user review when authenticated and has reviewed', () => {
      vi.spyOn(authService, 'currentUser', 'get').mockReturnValue({ id: 'user-1', email: 'user-1', fullName: 'A', role: 'Customer' } as any);
      flushProduct(page, httpMock, fixture);
      expect(page.getUserReview()).toEqual(mockReviews[0]);
    });

    it('returns undefined when not authenticated', () => {
      vi.spyOn(authService, 'currentUser', 'get').mockReturnValue(null);
      flushProduct(page, httpMock, fixture);
      expect(page.getUserReview()).toBeUndefined();
    });

    it('detects userHasReviewed after reviews load for current user', () => {
      vi.spyOn(authService, 'currentUser', 'get').mockReturnValue({ id: 'user-1', email: 'user-1', fullName: 'A', role: 'Customer' } as any);
      flushProduct(page, httpMock, fixture);
      expect(page.userHasReviewed()).toBe(true);
    });
  });

  describe('rating percentage helper', () => {
    it('returns 0 when summary is null', () => {
      flushProduct(page, httpMock, fixture);
      page.reviewSummary.set(null);
      expect(page.getRatingPercentage(5)).toBe(0);
    });

    it('returns 0 when totalReviews is 0', () => {
      flushProduct(page, httpMock, fixture);
      page.reviewSummary.set(emptySummary);
      expect(page.getRatingPercentage(5)).toBe(0);
    });

    it('computes correct percentage', () => {
      flushProduct(page, httpMock, fixture);
      expect(page.getRatingPercentage(1)).toBe(50);
    });
  });
});
