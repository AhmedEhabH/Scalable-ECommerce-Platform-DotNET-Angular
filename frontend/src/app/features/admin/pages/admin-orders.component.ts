import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { OrderDto } from '../../../core/models/order.model';

const STATUS_TRANSITIONS: Record<string, string[]> = {
  Pending: ['Confirmed', 'Cancelled'],
  Confirmed: ['Processing', 'Cancelled'],
  Processing: ['Shipped', 'Cancelled'],
  Shipped: ['Delivered'],
  Delivered: ['Refunded'],
  Cancelled: [],
  Refunded: []
};

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DatePipe],
  template: `
    <div class="orders-page">
      <div class="page-header">
        <h1>Orders</h1>
      </div>

      @if (loading()) {
        <p class="loading">Loading...</p>
      } @else if (orders().length === 0) {
        <div class="empty">
          <p>No orders yet.</p>
        </div>
      } @else {
        <div class="orders-table-wrapper">
          <table class="data-table">
            <thead>
              <tr>
                <th>Order#</th>
                <th>Customer</th>
                <th>Items</th>
                <th>Total</th>
                <th>Status</th>
                <th>Date</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (order of orders(); track order.id) {
                <tr>
                  <td>{{ order.orderNumber }}</td>
                  <td>{{ order.userEmail || '—' }}</td>
                  <td>{{ order.totalItems }}</td>
                  <td>{{ order.totalAmount | currency }}</td>
                  <td>
                    <span class="status-badge status-badge--{{ order.status.toLowerCase() }}">
                      {{ order.status }}
                    </span>
                  </td>
                  <td>{{ order.createdAt | date:'medium' }}</td>
                  <td class="actions">
                    @if (updatingOrderId() === order.id) {
                      <span class="updating">Updating…</span>
                    } @else {
                      <select
                        class="status-select"
                        [value]="order.status"
                        (change)="onStatusChange(order.id, $event)"
                      >
                        @for (status of getValidTransitions(order.status); track status) {
                          <option value="{{status}}">{{status}}</option>
                        }
                      </select>
                    }
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
    .orders-page { max-width: 1200px; }
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1.5rem;
    }
    .page-header h1 { margin: 0; }

    .loading, .empty { text-align: center; padding: 3rem; color: var(--text-secondary); }

    .orders-table-wrapper { overflow-x: auto; }
    .data-table {
      width: 100%;
      border-collapse: collapse;
      background: var(--card-bg);
      border-radius: 8px;
    }
    .data-table th, .data-table td {
      padding: 1rem;
      text-align: left;
      border-bottom: 1px solid var(--border-color);
    }
    .data-table th { font-size: 0.75rem; color: var(--text-secondary); text-transform: uppercase; }

    .status-badge {
      padding: 0.25rem 0.5rem;
      border-radius: 4px;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
    }
    .status-badge--pending { background: rgba(245, 158, 11, 0.1); color: #d97706; }
    .status-badge--confirmed { background: rgba(59, 130, 246, 0.1); color: #2563eb; }
    .status-badge--processing { background: rgba(59, 130, 246, 0.1); color: #2563eb; }
    .status-badge--shipped { background: rgba(139, 92, 246, 0.1); color: #7c3aed; }
    .status-badge--delivered { background: rgba(34, 197, 94, 0.1); color: #16a34a; }
    .status-badge--cancelled { background: rgba(239, 68, 68, 0.1); color: #dc2626; }
    .status-badge--refunded { background: rgba(239, 68, 68, 0.1); color: #dc2626; }

    .actions { min-width: 120px; }
    .status-select {
      padding: 0.4rem 0.5rem;
      border: 1px solid var(--border-color);
      border-radius: 4px;
      background: var(--card-bg);
      font: inherit;
      font-size: 0.8rem;
      cursor: pointer;
    }
    .updating { color: var(--text-secondary); font-size: 0.8rem; font-style: italic; }
  `]
})
export class AdminOrdersComponent implements OnInit {
  private adminService = inject(AdminService);
  private toastService = inject(ToastService);

  orders = signal<OrderDto[]>([]);
  loading = signal(true);
  updatingOrderId = signal<string | null>(null);

  ngOnInit(): void {
    this.loadOrders();
  }

  getValidTransitions(currentStatus: string): string[] {
    return STATUS_TRANSITIONS[currentStatus] ?? [];
  }

  onStatusChange(orderId: string, event: Event): void {
    const select = event.target as HTMLSelectElement;
    const newStatus = select.value;
    const previousStatus = this.orders().find(o => o.id === orderId)?.status;

    if (!confirm(`Change order status from "${previousStatus}" to "${newStatus}"?`)) {
      select.value = previousStatus || '';
      return;
    }

    this.updatingOrderId.set(orderId);

    this.adminService.updateOrderStatus(orderId, newStatus).subscribe({
      next: () => {
        this.toastService.success(`Order status updated to ${newStatus}`);
        this.updatingOrderId.set(null);
        this.loadOrders();
      },
      error: (err) => {
        this.toastService.error(err.error?.message || 'Failed to update order status');
        this.updatingOrderId.set(null);
        this.loadOrders();
      }
    });
  }

  private loadOrders(): void {
    this.loading.set(true);
    this.adminService.getOrders().subscribe({
      next: (response: any) => {
        this.orders.set(response?.data || []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
