import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { ProductImagePipe } from '../../../shared/pipes/product-image.pipe';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { CategoriesService } from '../../products/services/categories.service';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models';

interface ProductListItem {
  id: string;
  name: string;
  price: number;
  stockQuantity: number;
  isActive: boolean;
  mainImageUrl: string | null;
  slug: string;
  categoryId: string;
  createdAt: string;
}

@Component({
  selector: 'app-seller-products',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, RouterLink, ProductImagePipe],
  template: `
    <div class="products-page">
      <div class="page-header">
        <h1>My Products</h1>
        <a routerLink="/seller/products/new" class="btn-primary">Add Product</a>
      </div>

      @if (loading()) {
        <p class="loading">Loading...</p>
      } @else if (products().length === 0) {
        <div class="empty">
          <svg class="empty-icon" xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
            <path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z"></path>
            <line x1="7" y1="7" x2="7.01" y2="7"></line>
          </svg>
          <p>You haven't added any products yet.</p>
          <a routerLink="/seller/products/new" class="btn-primary">Add your first product</a>
        </div>
      } @else {
        <div class="products-table-wrapper">
          <table class="data-table">
            <thead>
              <tr>
                <th>Image</th>
                <th>Name</th>
                <th>Category</th>
                <th>Price</th>
                <th>Stock</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (product of products(); track product.id) {
                <tr>
                  <td>
                    <img [src]="product.mainImageUrl | productImage: product.slug" [alt]="product.name" class="product-thumb" />
                  </td>
                  <td class="product-name">{{ product.name }}</td>
                  <td>{{ categoryName(product.categoryId) }}</td>
                  <td>{{ product.price | currency }}</td>
                  <td [class.warning]="product.stockQuantity < 10">{{ product.stockQuantity }}</td>
                  <td>
                    <span class="status" [class.active]="product.isActive">
                      {{ product.isActive ? 'Active' : 'Inactive' }}
                    </span>
                  </td>
                  <td class="actions">
                    <a [routerLink]="['/seller/products', product.id, 'edit']" class="btn-link">Edit</a>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
  styles: [`
    .products-page { max-width: 1200px; }
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1.5rem;
    }
    .page-header h1 { margin: 0; }

    .btn-primary {
      background: var(--primary);
      color: white;
      padding: 0.5rem 1rem;
      border-radius: 6px;
      text-decoration: none;
    }
    .loading, .empty { text-align: center; padding: 3rem; color: var(--text-secondary); }
    .empty a { margin-top: 1rem; display: inline-block; }
    .empty-icon { margin-bottom: 1rem; color: var(--text-muted); }

    .products-table-wrapper { overflow-x: auto; }
    .data-table {
      width: 100%;
      border-collapse: collapse;
      background: var(--card-bg);
      border-radius: 8px;
    }
    .data-table th, .data-table td {
      padding: 0.75rem 1rem;
      text-align: left;
      border-bottom: 1px solid var(--border-color);
      vertical-align: middle;
    }
    .data-table th { font-size: 0.75rem; color: var(--text-secondary); text-transform: uppercase; }
    .data-table tbody tr:hover { background: var(--hover-bg); }
    .product-thumb {
      width: 48px;
      height: 48px;
      object-fit: cover;
      border-radius: 4px;
      display: block;
      background: var(--color-bg-secondary);
    }
    .product-name { font-weight: 500; }
    .warning { color: var(--warning); font-weight: 600; }
    .status {
      padding: 0.25rem 0.5rem;
      border-radius: 4px;
      font-size: 0.75rem;
      background: var(--text-muted);
      color: white;
    }
    .status.active { background: var(--success); }
    .actions { display: flex; gap: 1rem; }
    .btn-link { color: var(--primary); text-decoration: none; background: none; border: none; cursor: pointer; font: inherit; }
    .btn-link:hover { text-decoration: underline; }
  `]
})
export class SellerProductsComponent implements OnInit {
  private authService = inject(AuthService);
  private toastService = inject(ToastService);
  private http = inject(HttpClient);
  private categoriesService = inject(CategoriesService);
  private baseUrl = environment.apiBaseUrl;

  products = signal<ProductListItem[]>([]);
  loading = signal(true);
  private categoryMap: Record<string, string> = {};

  ngOnInit(): void {
    this.loadCategories();
    this.loadProducts();
  }

  private loadCategories(): void {
    this.categoriesService.getCategories().subscribe({
      next: (response) => {
        if (response.data) {
          const map: Record<string, string> = {};
          const flatten = (cats: any[]) => {
            for (const cat of cats) {
              map[cat.id] = cat.name;
              if (cat.children?.length) flatten(cat.children);
            }
          };
          flatten(response.data);
          this.categoryMap = map;
        }
      }
    });
  }

  categoryName(categoryId: string): string {
    return this.categoryMap[categoryId] || '\u2014';
  }

  private loadProducts(): void {
    this.loading.set(true);
    const user = this.authService.currentUser;
    if (!user) {
      this.loading.set(false);
      this.toastService.error('Please log in to view your products');
      return;
    }

    this.http.get<ApiResponse<any>>(`${this.baseUrl}/products/my`).subscribe({
      next: (response) => {
        const data = response?.data;
        this.products.set(data?.items || []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.products.set([]);
      }
    });
  }
}
