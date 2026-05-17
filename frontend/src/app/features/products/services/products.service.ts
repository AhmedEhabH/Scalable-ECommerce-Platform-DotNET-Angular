import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Product } from '../models/product.model';
import { PaginatedResult } from '../models/pagination.model';
import { ProductListQuery } from '../models/product-list-query.model';
import { ApiResponse } from '../../../core/models';

@Injectable({
  providedIn: 'root'
})
export class ProductsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getProducts(query: ProductListQuery = {}): Observable<ApiResponse<PaginatedResult<Product>>> {
    let params = new HttpParams();

    params = params.set('Page', (query.page || 1).toString());
    if (query.pageSize) params = params.set('PageSize', query.pageSize.toString());
    if (query.searchTerm) params = params.set('SearchTerm', query.searchTerm);
    if (query.categoryId) params = params.set('CategoryId', query.categoryId);
    if (query.sortBy) params = params.set('SortBy', query.sortBy);
    if (query.sortDescending !== undefined) params = params.set('SortDescending', query.sortDescending.toString());
    if (query.isFeatured !== undefined) params = params.set('IsFeatured', query.isFeatured.toString());
    if (query.isInStock !== undefined) params = params.set('IsInStock', query.isInStock.toString());

    return this.http.get<ApiResponse<PaginatedResult<Product>>>(`${this.baseUrl}/products`, { params });
  }

  getProductById(id: string): Observable<ApiResponse<Product>> {
    return this.http.get<ApiResponse<Product>>(`${this.baseUrl}/products/${id}`);
  }
}
