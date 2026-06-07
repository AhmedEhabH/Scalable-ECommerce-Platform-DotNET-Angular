import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { AuthResponse, AuthUser } from '../models';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should store user and refresh token after successful login', () => {
    const mockResponse = {
      success: true,
      data: {
        userId: 'user-123',
        refreshToken: 'test-refresh-token',
        expiresAt: '2026-01-01T00:00:00Z',
        email: 'admin@test.com',
        fullName: 'Admin User',
        role: 'Admin'
      } as AuthResponse
    };

    service.login({ email: 'admin@test.com', password: 'Admin@123' }, true).subscribe();

    const req = httpMock.expectOne('http://localhost:5000/api/auth/login');
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);

    expect(localStorage.getItem('refresh_token')).toBe('test-refresh-token');

    const storedUser = JSON.parse(localStorage.getItem('auth_user') || '{}');
    expect(storedUser.id).toBe('user-123');
    expect(storedUser.email).toBe('admin@test.com');
    expect(storedUser.role).toBe('Admin');
    expect(storedUser.fullName).toBe('Admin User');
  });

  it('should restore auth state from storage on initialization', () => {
    const user: AuthUser = { id: 'user-id', email: 'test@test.com', fullName: 'Test User', role: 'Seller' };
    localStorage.setItem('auth_user', JSON.stringify(user));

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    const restoredService = TestBed.inject(AuthService);

    expect(restoredService.isAuthenticated).toBe(true);
    expect(restoredService.currentUser).not.toBeNull();
    expect(restoredService.currentUser?.email).toBe('test@test.com');
    expect(restoredService.isSeller).toBe(true);
  });

  it('hasRole should return true for matching role', () => {
    const user: AuthUser = { id: '1', email: 'a@b.com', fullName: 'A', role: 'Admin' };
    localStorage.setItem('auth_user', JSON.stringify(user));

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [AuthService, provideHttpClient(), provideHttpClientTesting()]
    });

    const svc = TestBed.inject(AuthService);
    expect(svc.hasRole('Admin')).toBe(true);
    expect(svc.hasRole('admin')).toBe(true);
    expect(svc.hasRole('ADMIN')).toBe(true);
  });

  it('hasRole should return false for non-matching role', () => {
    const user: AuthUser = { id: '1', email: 'a@b.com', fullName: 'A', role: 'Customer' };
    localStorage.setItem('auth_user', JSON.stringify(user));

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [AuthService, provideHttpClient(), provideHttpClientTesting()]
    });

    const svc = TestBed.inject(AuthService);
    expect(svc.hasRole('Admin')).toBe(false);
    expect(svc.hasRole('Seller')).toBe(false);
  });

  it('isAdmin should return true only for Admin role', () => {
    const adminUser: AuthUser = { id: '1', email: 'a@b.com', fullName: 'A', role: 'Admin' };
    localStorage.setItem('auth_user', JSON.stringify(adminUser));

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [AuthService, provideHttpClient(), provideHttpClientTesting()]
    });

    const svc = TestBed.inject(AuthService);
    expect(svc.isAdmin).toBe(true);
    expect(svc.isSeller).toBe(false);
  });

  it('isSeller should return true only for Seller role', () => {
    const sellerUser: AuthUser = { id: '1', email: 'a@b.com', fullName: 'A', role: 'Seller' };
    localStorage.setItem('auth_user', JSON.stringify(sellerUser));

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [AuthService, provideHttpClient(), provideHttpClientTesting()]
    });

    const svc = TestBed.inject(AuthService);
    expect(svc.isSeller).toBe(true);
    expect(svc.isAdmin).toBe(false);
  });

  it('isAuthenticated should return false when no user is stored', () => {
    localStorage.clear();
    sessionStorage.clear();

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [AuthService, provideHttpClient(), provideHttpClientTesting()]
    });

    const svc = TestBed.inject(AuthService);
    expect(svc.isAuthenticated).toBe(false);
  });

  it('logout should clear storage and reset user', () => {
    const user: AuthUser = { id: '1', email: 'a@b.com', fullName: 'A', role: 'Admin' };
    localStorage.setItem('auth_user', JSON.stringify(user));
    localStorage.setItem('refresh_token', 'refresh-token');
    service.restoreAuthState();

    expect(service.isAuthenticated).toBe(true);

    service.logout();

    const logoutReq = httpMock.expectOne('http://localhost:5000/api/auth/logout');
    expect(logoutReq.request.method).toBe('POST');
    logoutReq.flush({});

    expect(service.isAuthenticated).toBe(false);
    expect(service.currentUser).toBeNull();
    expect(localStorage.getItem('auth_user')).toBeNull();
    expect(localStorage.getItem('refresh_token')).toBeNull();
  });

  it('restoreAuthState should set user to null when no stored user', () => {
    localStorage.clear();

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [AuthService, provideHttpClient(), provideHttpClientTesting()]
    });

    const svc = TestBed.inject(AuthService);
    svc.restoreAuthState();

    expect(svc.currentUser).toBeNull();
  });
});
