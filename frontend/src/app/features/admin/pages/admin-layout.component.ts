import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="admin-layout">
      <aside class="admin-sidebar">
        <h2>Admin</h2>
        <nav>
          <a routerLink="/admin/dashboard" routerLinkActive="active">Dashboard</a>
          <a routerLink="/admin/products" routerLinkActive="active">Products</a>
          <a routerLink="/admin/categories" routerLinkActive="active">Categories</a>
          <a routerLink="/admin/orders" routerLinkActive="active">Orders</a>
          <a routerLink="/admin/users" routerLinkActive="active" class="disabled">Users</a>
        </nav>
        <div class="admin-footer">
          <a routerLink="/">Back to Store</a>
        </div>
      </aside>
      <main class="admin-content">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    .admin-layout {
      display: flex;
      min-height: 100vh;
    }
    .admin-sidebar {
      width: 240px;
      background: var(--card-bg);
      border-right: 1px solid var(--border-color);
      padding: 1.5rem;
      display: flex;
      flex-direction: column;
    }
    .admin-sidebar h2 {
      margin: 0 0 1.5rem;
      color: var(--primary);
    }
    .admin-sidebar nav {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }
    .admin-sidebar a {
      padding: 0.75rem 1rem;
      border-radius: 6px;
      color: var(--text-secondary);
      text-decoration: none;
      transition: all 0.2s;
    }
    .admin-sidebar a:hover {
      background: var(--hover-bg);
      color: var(--text-primary);
    }
    .admin-sidebar a.active {
      background: var(--primary);
      color: white;
    }
    .admin-sidebar a.disabled {
      opacity: 0.5;
      pointer-events: none;
    }
    .admin-footer {
      margin-top: auto;
      padding-top: 1rem;
      border-top: 1px solid var(--border-color);
    }
    .admin-content {
      flex: 1;
      padding: 2rem;
      overflow-y: auto;
    }
    @media (max-width: 768px) {
      .admin-layout {
        flex-direction: column;
      }
      .admin-sidebar {
        width: 100%;
        border-right: none;
        border-bottom: 1px solid var(--border-color);
      }
      .admin-sidebar nav {
        flex-direction: row;
        overflow-x: auto;
      }
    }
  `]
})
export class AdminLayoutComponent {}