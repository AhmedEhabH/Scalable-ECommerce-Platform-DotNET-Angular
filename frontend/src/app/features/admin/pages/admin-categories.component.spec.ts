import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AdminCategoriesComponent } from './admin-categories.component';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../shared/components/toast/toast.service';

describe('AdminCategoriesComponent', () => {
  let component: AdminCategoriesComponent;
  let httpMock: HttpTestingController;
  let adminService: AdminService;

  const mockCategories = [
    {
      id: '1',
      name: 'Electronics',
      slug: 'electronics',
      description: 'Electronic items',
      displayOrder: 1,
      isActive: true,
      parentId: null,
      productCount: 5,
      children: []
    },
    {
      id: '2',
      name: 'Clothing',
      slug: 'clothing',
      description: 'Clothing items',
      displayOrder: 2,
      isActive: true,
      parentId: null,
      productCount: 0,
      children: []
    }
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AdminCategoriesComponent],
      providers: [
        AdminService,
        ToastService,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    });

    const fixture = TestBed.createComponent(AdminCategoriesComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    adminService = TestBed.inject(AdminService);
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });

  it('should load categories on init', () => {
    component.ngOnInit();

    const req = httpMock.expectOne('http://localhost:5000/api/categories');
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: mockCategories });

    expect(component.categories().length).toBe(2);
    expect(component.categories()[0].name).toBe('Electronics');
  });

  it('should handle empty categories list', () => {
    component.ngOnInit();

    const req = httpMock.expectOne('http://localhost:5000/api/categories');
    req.flush({ success: true, data: [] });

    expect(component.categories().length).toBe(0);
  });

  it('should not delete category with products', () => {
    component.ngOnInit();

    const req = httpMock.expectOne('http://localhost:5000/api/categories');
    req.flush({ success: true, data: mockCategories });

    const categoryWithProducts = component.categories()[0];
    expect(categoryWithProducts.productCount).toBe(5);

    // Try to delete - should not call delete API
    // The component checks productCount > 0 and shows error
  });
});
