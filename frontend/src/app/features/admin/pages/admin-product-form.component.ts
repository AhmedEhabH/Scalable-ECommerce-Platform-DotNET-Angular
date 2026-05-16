import { Component, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-admin-product-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="product-form">
      <h1>{{ isEdit() ? 'Edit' : 'Add' }} Product</h1>

      <form (ngSubmit)="onSubmit()">
        <div class="form-group">
          <label for="name">Name *</label>
          <input id="name" [(ngModel)]="product.name" name="name" required />
        </div>

        <div class="form-group">
          <label for="slug">Slug *</label>
          <input id="slug" [(ngModel)]="product.slug" name="slug" required />
        </div>

        <div class="form-row">
          <div class="form-group">
            <label for="price">Price *</label>
            <input id="price" type="number" [(ngModel)]="product.price" name="price" step="0.01" required />
          </div>
          <div class="form-group">
            <label for="sku">SKU *</label>
            <input id="sku" [(ngModel)]="product.sku" name="sku" required />
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label for="stockQuantity">Stock *</label>
            <input id="stockQuantity" type="number" [(ngModel)]="product.stockQuantity" name="stockQuantity" required />
          </div>
          <div class="form-group">
            <label for="categoryId">Category *</label>
            <select id="categoryId" [(ngModel)]="product.categoryId" name="categoryId" required>
              <option value="">Select category</option>
              @for (cat of categories(); track cat.id) {
                <option [value]="cat.id">{{ cat.name }}</option>
              }
            </select>
          </div>
        </div>

        <div class="form-group">
          <label for="description">Description</label>
          <textarea id="description" [(ngModel)]="product.description" name="description" rows="4"></textarea>
        </div>

        <div class="form-group">
          <label for="image">Product Image</label>
          <input id="image" type="file" accept="image/png, image/jpeg, image/jpg, image/webp" (change)="onFileSelected($event)" />
          @if (imagePreview()) {
            <div class="image-preview">
              <img [src]="imagePreview()" alt="Product preview" />
            </div>
          } @else if (existingImageUrl()) {
            <div class="image-preview">
              <img [src]="existingImageUrl()" alt="Current product image" />
              <span class="image-hint">Current image (select a new file to replace)</span>
            </div>
          }
        </div>

        <div class="form-group checkbox">
          <input id="isFeatured" type="checkbox" [(ngModel)]="product.isFeatured" name="isFeatured" />
          <label for="isFeatured">Featured product</label>
        </div>

        <div class="form-group checkbox">
          <input id="isActive" type="checkbox" [(ngModel)]="product.isActive" name="isActive" />
          <label for="isActive">Active</label>
        </div>

        <div class="form-actions">
          <button type="button" class="btn-secondary" (click)="cancel()">Cancel</button>
          <button type="submit" class="btn-primary" [disabled]="saving()">
            @if (saving()) {
              <span class="spinner"></span>
            }
            {{ saving() ? 'Saving...' : 'Save' }}
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .product-form { max-width: 600px; }
    h1 { margin: 0 0 1.5rem; }

    .form-group { margin-bottom: 1rem; }
    .form-group label { display: block; margin-bottom: 0.5rem; font-weight: 500; }
    .form-group input, .form-group select, .form-group textarea {
      width: 100%;
      padding: 0.75rem;
      border: 1px solid var(--border-color);
      border-radius: 6px;
      background: var(--input-bg);
      color: var(--text-primary);
      font-size: 1rem;
    }
    .form-group input[type="file"] {
      padding: 0.5rem;
      cursor: pointer;
    }
    .form-group select {
      appearance: none;
      background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 12 12'%3E%3Cpath fill='%23ffffff' d='M6 8L1 3h10z'/%3E%3C/svg%3E");
      background-repeat: no-repeat;
      background-position: right 0.75rem center;
      padding-right: 2.5rem;
      cursor: pointer;
      background-color: var(--input-bg, #374151);
      color: var(--text-primary, #f3f4f6);
      border-color: var(--border-color, #4b5563);
    }
    .form-group select option {
      background: var(--card-bg, #1f2937);
      color: var(--text-primary, #f3f4f6);
      padding: 0.5rem;
    }
    .form-group select:focus {
      outline: none;
      border-color: var(--primary, #3b82f6);
      box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.2);
    }

    .form-group.checkbox {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }
    .form-group.checkbox label { margin: 0; }
    .form-group.checkbox input { width: auto; }

    .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    .form-actions { display: flex; gap: 1rem; margin-top: 1.5rem; align-items: center; }

    .image-preview { margin-top: 0.5rem; }
    .image-preview img { max-width: 200px; max-height: 200px; border-radius: 6px; border: 1px solid var(--border-color); object-fit: cover; }
    .image-hint { display: block; font-size: 0.75rem; color: var(--text-secondary); margin-top: 0.25rem; }

    .btn-primary, .btn-secondary {
      padding: 0.75rem 1.5rem;
      border-radius: 6px;
      font-size: 1rem;
      cursor: pointer;
      border: none;
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
    }
    .btn-primary { background: var(--primary); color: white; }
    .btn-primary:disabled { opacity: 0.5; cursor: not-allowed; }
    .btn-secondary { background: var(--text-muted); color: white; }

    .spinner {
      width: 1rem;
      height: 1rem;
      border: 2px solid rgba(255,255,255,0.3);
      border-top-color: white;
      border-radius: 50%;
      animation: spin 0.6s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
  `]
})
export class AdminProductFormComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private adminService = inject(AdminService);
  private toastService = inject(ToastService);

  product: any = this.getEmptyProduct();
  categories = signal<any[]>([]);
  loading = signal(true);
  saving = signal(false);
  isEdit = signal(false);
  selectedFile: File | null = null;
  imagePreview = signal<string | null>(null);
  existingImageUrl = signal<string | null>(null);
  private productId = '';

  private getEmptyProduct() {
    return {
      name: '',
      slug: '',
      price: 0,
      sku: '',
      stockQuantity: 0,
      categoryId: '',
      description: null as string | null,
      compareAtPrice: null as number | null,
      isFeatured: false,
      isActive: true
    };
  }

  ngOnInit(): void {
    this.productId = this.route.snapshot.paramMap.get('id') || '';
    this.isEdit.set(!!this.productId);
    this.loadCategories();

    if (this.productId) {
      this.loadProduct();
    } else {
      this.loading.set(false);
    }
  }

  private loadCategories(): void {
    this.adminService.getCategories().subscribe({
      next: (response: any) => this.categories.set(response?.data || response || []),
      error: () => this.categories.set([])
    });
  }

  reloadCategories(): void {
    this.loadCategories();
  }

  private loadProduct(): void {
    this.adminService.getProduct(this.productId).subscribe({
      next: (response: any) => {
        const p = response?.data || response;
        this.product = {
          name: p.name,
          slug: p.slug,
          price: p.price,
          sku: p.sku,
          stockQuantity: p.stockQuantity,
          categoryId: p.categoryId,
          description: p.description || null,
          compareAtPrice: p.compareAtPrice || null,
          isFeatured: p.isFeatured,
          isActive: p.isActive
        };
        if (p.mainImageUrl) {
          this.existingImageUrl.set(p.mainImageUrl);
        }
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load product');
        this.router.navigate(['/admin/products']);
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    if (!file) return;

    if (file.size > 5 * 1024 * 1024) {
      this.toastService.error('File is too large. Maximum size is 5MB.');
      input.value = '';
      return;
    }

    if (this.imagePreview()) {
      URL.revokeObjectURL(this.imagePreview()!);
    }

    this.selectedFile = file;
    this.imagePreview.set(URL.createObjectURL(file));
  }

  private buildRequestPayload(): any {
    return {
      CategoryId: this.product.categoryId,
      Name: this.product.name,
      Slug: this.product.slug,
      Price: Number(this.product.price) || 0,
      SKU: this.product.sku,
      StockQuantity: Number(this.product.stockQuantity) || 0,
      Description: this.product.description || null,
      CompareAtPrice: this.product.compareAtPrice ? Number(this.product.compareAtPrice) : null,
      LowStockThreshold: 10,
      IsFeatured: Boolean(this.product.isFeatured),
      IsActive: Boolean(this.product.isActive)
    };
  }

  onSubmit(): void {
    if (!this.product.name || !this.product.slug || !this.product.categoryId || !this.product.sku || !this.product.price || !this.product.stockQuantity) {
      this.toastService.error('Please fill in all required fields');
      return;
    }

    if (!this.product.categoryId || this.product.categoryId === '') {
      this.toastService.error('Please select a category');
      return;
    }

    this.saving.set(true);
    const payload = this.buildRequestPayload();

    if (this.isEdit()) {
      this.adminService.updateProduct(this.productId, payload).subscribe({
        next: () => {
          if (this.selectedFile) {
            this.uploadImage(this.productId);
          } else {
            this.toastService.success('Product updated successfully');
            this.router.navigate(['/admin/products']);
          }
        },
        error: (err: any) => {
          this.saving.set(false);
          const msg = err?.error?.message || 'Failed to update product';
          this.toastService.error(msg);
        }
      });
    } else {
      this.adminService.createProduct(payload).subscribe({
        next: (response: any) => {
          const newId = response?.data?.id || response?.id;
          if (this.selectedFile && newId) {
            this.uploadImage(newId);
          } else {
            this.toastService.success('Product created successfully');
            this.router.navigate(['/admin/products']);
          }
        },
        error: (err: any) => {
          this.saving.set(false);
          this.toastService.error(this.extractErrorMessage(err));
        }
      });
    }
  }

  private uploadImage(productId: string): void {
    this.adminService.uploadImage(productId, this.selectedFile!).subscribe({
      next: (response) => {
        if (response?.data) {
          const updatedProduct = response.data;
        }
        this.toastService.success('Product saved with image');
        this.router.navigate(['/admin/products']);
      },
      error: (err) => {
        console.error('Upload handler failed:', err);
        this.toastService.error('Product saved but image upload failed');
        this.router.navigate(['/admin/products']);
      }
    });
  }

  private extractErrorMessage(error: any): string {
    if (!error) return 'Failed to save product. Please verify the form and try again.';
    const errorData = error.error;
    if (errorData?.message) {
      const msg = errorData.message.toLowerCase();
      if (msg.includes('sku')) return 'A product with this SKU already exists';
      if (msg.includes('slug')) return 'A product with this slug already exists';
      if (msg.includes('category')) return 'Invalid category selected';
      return errorData.message;
    }
    if (errorData?.errors) {
      return Object.values(errorData.errors).flat().join(', ') || 'Please check your form input';
    }
    return 'Failed to save product. Please verify the form and try again.';
  }

  cancel(): void {
    this.cleanupPreviewUrl();
    this.router.navigate(['/admin/products']);
  }

  ngOnDestroy(): void {
    this.cleanupPreviewUrl();
  }

  private cleanupPreviewUrl(): void {
    const url = this.imagePreview();
    if (url) {
      URL.revokeObjectURL(url);
      this.imagePreview.set(null);
    }
  }
}
