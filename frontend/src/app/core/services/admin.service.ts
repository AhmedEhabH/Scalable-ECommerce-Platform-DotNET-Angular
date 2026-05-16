import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/auth.model';

export interface DashboardSummary {
  totalOrders: number;
  totalSales: number;
  totalProducts: number;
  totalUsers: number;
  recentOrders: any[];
  topProducts: any[];
  lowStockProducts: any[];
}

export interface ProductListItem {
  id: string;
  name: string;
  price: number;
  stockQuantity: number;
  isActive: boolean;
  categoryId: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiBaseUrl + '/admin';

  getDashboard(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.baseUrl}/dashboard?_t=${Date.now()}`);
  }

  getProducts(): Observable<any[]> {
    return this.http.get<any[]>(`${environment.apiBaseUrl}/products?page=1&pageSize=100&sortBy=created&sortDescending=true&_t=${Date.now()}`);
  }

  getProduct(id: string): Observable<any> {
    return this.http.get<any>(`${environment.apiBaseUrl}/products/${id}`);
  }

  createProduct(product: any): Observable<any> {
    return this.http.post<any>(`${environment.apiBaseUrl}/products`, product);
  }

  updateProduct(id: string, product: any): Observable<any> {
    return this.http.put<any>(`${environment.apiBaseUrl}/products/${id}`, product);
  }

  deleteProduct(id: string): Observable<any> {
    return this.http.delete<any>(`${environment.apiBaseUrl}/products/${id}`);
  }

  uploadImage(productId: string, file: File): Observable<ApiResponse<any>> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ApiResponse<any>>(`${environment.apiBaseUrl}/products/${productId}/images`, formData);
  }

  getCategories(): Observable<any[]> {
    return this.http.get<any[]>(`${environment.apiBaseUrl}/categories`);
  }

  getCategory(id: string): Observable<any> {
    return this.http.get<any>(`${environment.apiBaseUrl}/categories/${id}`);
  }

  createCategory(category: any): Observable<any> {
    return this.http.post<any>(`${environment.apiBaseUrl}/categories`, category);
  }

  updateCategory(id: string, category: any): Observable<any> {
    return this.http.put<any>(`${environment.apiBaseUrl}/categories/${id}`, category);
  }

  deleteCategory(id: string): Observable<any> {
    return this.http.delete<any>(`${environment.apiBaseUrl}/categories/${id}`);
  }

  deactivateCategory(id: string): Observable<any> {
    return this.http.patch<any>(`${environment.apiBaseUrl}/categories/${id}/deactivate`, {});
  }
}