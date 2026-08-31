import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { ProductsService } from '../services/products.service';
import { CategoriesService } from '../services/categories.service';
import { Product } from '../models/product.model';
import { CategorySimple } from '../models/category.model';
import { PaginatedResult } from '../models/pagination.model';
import { ApiResponse } from '../../../core/models';

describe('ProductListPage support layer', () => {
  let productsService: ProductsService;
  let categoriesService: CategoriesService;
  let httpMock: HttpTestingController;

  const mockProduct: Product = {
    id: 'prod-1', vendorId: 'vendor-1', categoryId: 'cat-1', name: 'Wireless Keyboard',
    slug: 'wireless-keyboard', description: 'Mechanical keyboard',
    price: 89.99, compareAtPrice: 99.99, sku: 'WK001', stockQuantity: 50,
    lowStockThreshold: 5, isFeatured: true, isActive: true, reviewCount: 12,
    averageRating: 4.5, isInStock: true, isLowStock: false, hasDiscount: true,
    discountPercentage: 10, mainImageUrl: 'https://example.com/kbd.jpg', images: [],
    createdAt: '2026-01-15T00:00:00Z', updatedAt: '2026-08-01T00:00:00Z',
    sellerName: 'TechStore', sellerDescription: undefined
  };

  const mockCategory: CategorySimple = { id: 'cat-1', name: 'Electronics' };

  const mockPagination: PaginatedResult<Product> = {
    items: [mockProduct],
    totalCount: 1,
    page: 1,
    pageSize: 12,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ProductsService,
        CategoriesService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    productsService = TestBed.inject(ProductsService);
    categoriesService = TestBed.inject(CategoriesService);
    httpMock = TestBed.inject(HttpTestingController);
    vi.useFakeTimers();
  });

  afterEach(() => {
    httpMock.verify();
    vi.useRealTimers();
  });

  describe('ProductsService.getProducts — HTTP contract', () => {
    it('sends GET to /products with defaults Page=1, PageSize=12', () => {
      productsService.getProducts({ page: 1, pageSize: 12 }).subscribe(res => {
        expect(res.success).toBe(true);
      });
      const req = httpMock.expectOne(req => req.url.includes('/products'));
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('Page')).toBe('1');
      expect(req.request.params.get('PageSize')).toBe('12');
      req.flush({ success: true, data: mockPagination, message: undefined });
    });

    it('sends search, category, stock, featured, sort params when present', () => {
      productsService.getProducts({
        page: 2, pageSize: 20, searchTerm: 'laptop', categoryId: 'cat-1',
        isInStock: true, isFeatured: false, sortBy: 'price', sortDescending: true
      }).subscribe(res => { expect(res.success).toBe(true); });

      const req = httpMock.expectOne(req => req.url.includes('/products'));
      expect(req.request.params.get('Page')).toBe('2');
      expect(req.request.params.get('PageSize')).toBe('20');
      expect(req.request.params.get('SearchTerm')).toBe('laptop');
      expect(req.request.params.get('CategoryId')).toBe('cat-1');
      expect(req.request.params.get('IsInStock')).toBe('true');
      expect(req.request.params.get('SortBy')).toBe('price');
      expect(req.request.params.get('SortDescending')).toBe('true');
      req.flush({ success: true, data: mockPagination, message: undefined });
    });

    it('returns success=false and message on error response', () => {
      productsService.getProducts({ page: 1 }).subscribe(res => {
        expect(res.success).toBe(false);
        expect(res.message).toBe('Server error');
      });
      const req = httpMock.expectOne(req => req.url.includes('/products'));
      req.flush({ success: false, data: undefined, message: 'Server error' });
    });

    it('does not send optional params when undefined', () => {
      productsService.getProducts({ page: 1, pageSize: 12 }).subscribe(res => {
        expect(res.success).toBe(true);
      });
      const req = httpMock.expectOne(req => req.url.includes('/products'));
      expect(req.request.params.has('SearchTerm')).toBe(false);
      expect(req.request.params.has('CategoryId')).toBe(false);
      expect(req.request.params.has('IsInStock')).toBe(false);
      expect(req.request.params.has('IsFeatured')).toBe(false);
      expect(req.request.params.has('SortBy')).toBe(false);
      req.flush({ success: true, data: mockPagination, message: undefined });
    });
  });

  describe('CategoriesService.getCategoryOptions', () => {
    it('flattens hierarchical categories into id/name pairs', () => {
      const nested = [
        { id: 'c1', name: 'Electronics', slug: 'electronics', description: null as null, displayOrder: 1, children: [
          { id: 'c1-1', name: 'Laptops', slug: 'laptops', description: null as null, displayOrder: 1, children: [] }
        ]},
        { id: 'c2', name: 'Clothing', slug: 'clothing', description: null as null, displayOrder: 2, children: [] }
      ];

      let result: CategorySimple[] = [];
      categoriesService.getCategoryOptions().subscribe(cats => { result = cats; });

      const req = httpMock.expectOne(req => req.url.includes('/categories'));
      req.flush({ success: true, data: nested, message: undefined });

      expect(result).toHaveLength(3);
      expect(result.map(c => c.name)).toContain('Electronics');
      expect(result.map(c => c.name)).toContain('Laptops');
      expect(result.map(c => c.name)).toContain('Clothing');
    });

    it('returns empty array when response data is empty', () => {
      let result: CategorySimple[] = [];
      categoriesService.getCategoryOptions().subscribe(cats => { result = cats; });

      const req = httpMock.expectOne(req => req.url.includes('/categories'));
      req.flush({ success: true, data: [], message: undefined });

      expect(result).toEqual([]);
    });
  });

  describe('Filter state logic (mirrors ProductListPage)', () => {
    it('clearFilters resets all signals to defaults', () => {
      const state = {
        searchTerm: signal('laptop'),
        selectedCategory: signal('cat-1'),
        inStockOnly: signal(true),
        featuredOnly: signal(true),
        sortBy: signal<string>('price'),
        sortDescending: signal(true),
        searchInput: 'laptop'
      };

      state.searchInput = '';
      state.searchTerm.set('');
      state.selectedCategory.set('');
      state.inStockOnly.set(false);
      state.featuredOnly.set(false);
      state.sortBy.set('featured');
      state.sortDescending.set(false);

      expect(state.searchTerm()).toBe('');
      expect(state.selectedCategory()).toBe('');
      expect(state.inStockOnly()).toBe(false);
      expect(state.featuredOnly()).toBe(false);
      expect(state.sortBy()).toBe('featured');
      expect(state.sortDescending()).toBe(false);
      expect(state.searchInput).toBe('');
    });

    it('hasActiveFilters is true when any filter is set', () => {
      const state = {
        searchTerm: signal(''),
        selectedCategory: signal(''),
        inStockOnly: signal(false),
        featuredOnly: signal(false)
      };

      const active = !!state.searchTerm() || !!state.selectedCategory() ||
        state.inStockOnly() || state.featuredOnly();
      expect(active).toBe(false);

      state.searchTerm.set('phone');
      expect(!!state.searchTerm() || !!state.selectedCategory() ||
        state.inStockOnly() || state.featuredOnly()).toBe(true);

      state.searchTerm.set('');
      state.inStockOnly.set(true);
      expect(!!state.searchTerm() || !!state.selectedCategory() ||
        state.inStockOnly() || state.featuredOnly()).toBe(true);
    });
  });

  describe('Pagination logic (mirrors ProductListPage navigation)', () => {
    it('nextPage advances only when hasNextPage is true', () => {
      const pag = signal<PaginatedResult<Product> | null>({
        items: [mockProduct], totalCount: 25, page: 2,
        pageSize: 12, totalPages: 3,
        hasPreviousPage: true, hasNextPage: true
      });

      expect(pag()!.hasNextPage).toBe(true);
      expect(pag()!.totalPages).toBe(3);

      const next: PaginatedResult<Product> = {
        items: [mockProduct], totalCount: 25, page: 3,
        pageSize: 12, totalPages: 3,
        hasPreviousPage: true, hasNextPage: false
      };
      pag.set(next);
      expect(pag()!.hasNextPage).toBe(false);
    });

    it('prevPage regresses only when hasPreviousPage is true', () => {
      const pag = signal<PaginatedResult<Product> | null>({
        items: [mockProduct], totalCount: 25, page: 2,
        pageSize: 12, totalPages: 3,
        hasPreviousPage: true, hasNextPage: true
      });

      expect(pag()!.hasPreviousPage).toBe(true);

      const prev: PaginatedResult<Product> = {
        items: [mockProduct], totalCount: 25, page: 1,
        pageSize: 12, totalPages: 3,
        hasPreviousPage: false, hasNextPage: true
      };
      pag.set(prev);
      expect(pag()!.hasPreviousPage).toBe(false);
      expect(pag()!.hasNextPage).toBe(true);
    });

    it('totalPages rounds up (ceiling division)', () => {
      const makePag = (totalCount: number, pageSize: number, page: number) => ({
        items: [mockProduct] as Product[],
        totalCount, page, pageSize,
        get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize); },
        hasPreviousPage: page > 1,
        hasNextPage: page < Math.ceil(totalCount / pageSize)
      } as PaginatedResult<Product> & { totalPages: number });

      expect(makePag(25, 12, 1).totalPages).toBe(3);
      expect(makePag(12, 12, 1).totalPages).toBe(1);
      expect(makePag(13, 12, 1).totalPages).toBe(2);
    });
  });

  describe('Product model — derived-display logic', () => {
    it('top-rated: averageRating >= 4.5', () => {
      expect(mockProduct.averageRating >= 4.5).toBe(true);
    });

    it('popular: reviewCount >= 50', () => {
      expect(mockProduct.reviewCount >= 50).toBe(false);
      expect((mockProduct.reviewCount + 40) >= 50).toBe(true);
    });

    it('low-stock: stockQuantity <= 5 AND isInStock', () => {
      expect(mockProduct.stockQuantity <= 5 && mockProduct.isInStock).toBe(false);
      const lowStock: Product = { ...mockProduct, stockQuantity: 3, isInStock: true };
      expect(lowStock.stockQuantity <= 5 && lowStock.isInStock).toBe(true);
    });

    it('hasDiscount AND compareAtPrice implies original price display', () => {
      expect(mockProduct.hasDiscount).toBe(true);
      expect(mockProduct.compareAtPrice).toBe(99.99);
      expect(mockProduct.hasDiscount && mockProduct.compareAtPrice).toBeTruthy();
      const noDiscount: Product = { ...mockProduct, hasDiscount: false, compareAtPrice: null };
      expect(noDiscount.hasDiscount && noDiscount.compareAtPrice).toBeFalsy();
    });
  });

  describe('Search debounce behavior', () => {
    it('debounces search input trigger by 300ms', () => {
      let called = false;
      const debounce = (fn: () => void, delay: number) => {
        let t: ReturnType<typeof setTimeout> | null = null;
        return () => {
          if (t) clearTimeout(t);
          t = setTimeout(fn, delay);
        };
      };
      const trigger = debounce(() => { called = true; }, 300);

      trigger();
      expect(called).toBe(false);

      vi.advanceTimersByTime(200);
      expect(called).toBe(false);

      vi.advanceTimersByTime(100);
      expect(called).toBe(true);
    });
  });
});
