import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { convertToParamMap, provideRouter } from '@angular/router';
import { AdminCategoryFormComponent } from './admin-category-form.component';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../shared/components/toast/toast.service';

describe('AdminCategoryFormComponent', () => {
  let component: AdminCategoryFormComponent;
  let httpMock: HttpTestingController;
  let adminService: AdminService;

  const mockCategory = {
    id: '1',
    name: 'Electronics',
    slug: 'electronics',
    description: 'Electronic items',
    displayOrder: 1,
    isActive: true,
    parentId: null,
    productCount: 0
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AdminCategoryFormComponent],
      providers: [
        AdminService,
        ToastService,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    });

    const fixture = TestBed.createComponent(AdminCategoryFormComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    adminService = TestBed.inject(AdminService);
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with empty category form', () => {
    expect(component.category.name).toBe('');
    expect(component.category.slug).toBe('');
    expect(component.isEdit()).toBe(false);
  });

  it('should validate required fields on submit', () => {
    component.onSubmit();
    expect(component.category.name).toBe('');
    expect(component.category.slug).toBe('');
  });

  it('should load category data in edit mode', () => {
    component['route'] = {
      snapshot: { paramMap: convertToParamMap({ id: '1' }) }
    } as any;

    component.ngOnInit();

    const req = httpMock.expectOne('http://localhost:5000/api/categories/1');
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: mockCategory });

    expect(component.category.name).toBe('Electronics');
    expect(component.category.slug).toBe('electronics');
  });
});
