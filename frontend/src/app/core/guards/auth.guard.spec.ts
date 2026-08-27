import { describe, it, expect } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { EnvironmentInjector, runInInjectionContext } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { authGuard, adminGuard, sellerGuard } from './auth.guard';
import { AuthUser } from '../models';

function setupAuth(user: AuthUser | null): EnvironmentInjector {
  if (user) {
    localStorage.setItem('auth_user', JSON.stringify(user));
  } else {
    localStorage.removeItem('auth_user');
  }

  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      AuthService,
      provideHttpClient(),
      provideHttpClientTesting(),
      {
        provide: Router,
        useValue: { navigate: vi.fn(), url: '/' }
      }
    ]
  });

  return TestBed.inject(EnvironmentInjector);
}

describe('authGuard', () => {
  it('should allow access when token exists', () => {
    const user: AuthUser = { id: '1', email: 'a@b.com', fullName: 'A', role: 'Customer' };
    const injector = setupAuth(user);

    const result = runInInjectionContext(injector, () =>
      authGuard(
        { url: '/checkout', queryParams: {}, fragment: null } as any,
        { url: '/checkout' } as any
      )
    );

    expect(result).toBe(true);
  });

  it('should deny access when no token exists', () => {
    const injector = setupAuth(null);

    const result = runInInjectionContext(injector, () =>
      authGuard(
        { url: '/checkout', queryParams: {}, fragment: null } as any,
        { url: '/checkout' } as any
      )
    );

    expect(result).toBe(false);
  });
});

describe('adminGuard', () => {
  it('should allow access for Admin role', () => {
    const user: AuthUser = { id: '1', email: 'admin@test.com', fullName: 'Admin', role: 'Admin' };
    const injector = setupAuth(user);

    const result = runInInjectionContext(injector, () =>
      adminGuard(
        { url: '/admin/dashboard', queryParams: {}, fragment: null } as any,
        { url: '/admin/dashboard' } as any
      )
    );

    expect(result).toBe(true);
  });

  it('should deny access for Seller role', () => {
    const user: AuthUser = { id: '1', email: 'seller@test.com', fullName: 'Seller', role: 'Seller' };
    const injector = setupAuth(user);

    const result = runInInjectionContext(injector, () =>
      adminGuard(
        { url: '/admin/dashboard', queryParams: {}, fragment: null } as any,
        { url: '/admin/dashboard' } as any
      )
    );

    expect(result).toBe(false);
  });

  it('should deny access for Customer/User role', () => {
    const user: AuthUser = { id: '1', email: 'customer@test.com', fullName: 'Customer', role: 'User' };
    const injector = setupAuth(user);

    const result = runInInjectionContext(injector, () =>
      adminGuard(
        { url: '/admin/dashboard', queryParams: {}, fragment: null } as any,
        { url: '/admin/dashboard' } as any
      )
    );

    expect(result).toBe(false);
  });

  it('should deny access when no token', () => {
    const injector = setupAuth(null);

    const result = runInInjectionContext(injector, () =>
      adminGuard(
        { url: '/admin/dashboard', queryParams: {}, fragment: null } as any,
        { url: '/admin/dashboard' } as any
      )
    );

    expect(result).toBe(false);
  });
});

describe('sellerGuard', () => {
  it('should allow access for Seller role', () => {
    const user: AuthUser = { id: '1', email: 'seller@test.com', fullName: 'Seller', role: 'Seller' };
    const injector = setupAuth(user);

    const result = runInInjectionContext(injector, () =>
      sellerGuard(
        { url: '/seller/products', queryParams: {}, fragment: null } as any,
        { url: '/seller/products' } as any
      )
    );

    expect(result).toBe(true);
  });

  it('should allow access for Admin role', () => {
    const user: AuthUser = { id: '1', email: 'admin@test.com', fullName: 'Admin', role: 'Admin' };
    const injector = setupAuth(user);

    const result = runInInjectionContext(injector, () =>
      sellerGuard(
        { url: '/seller/products', queryParams: {}, fragment: null } as any,
        { url: '/seller/products' } as any
      )
    );

    expect(result).toBe(true);
  });

  it('should deny access for Customer/User role', () => {
    const user: AuthUser = { id: '1', email: 'customer@test.com', fullName: 'Customer', role: 'User' };
    const injector = setupAuth(user);

    const result = runInInjectionContext(injector, () =>
      sellerGuard(
        { url: '/seller/products', queryParams: {}, fragment: null } as any,
        { url: '/seller/products' } as any
      )
    );

    expect(result).toBe(false);
  });

  it('should deny access when no token', () => {
    const injector = setupAuth(null);

    const result = runInInjectionContext(injector, () =>
      sellerGuard(
        { url: '/seller/products', queryParams: {}, fragment: null } as any,
        { url: '/seller/products' } as any
      )
    );

    expect(result).toBe(false);
  });
});
