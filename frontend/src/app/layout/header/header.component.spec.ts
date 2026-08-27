import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthUser } from '../../core/models/auth.model';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { ThemeService } from '../../core/services/theme.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { HeaderComponent } from './header.component';

describe('HeaderComponent', () => {
  const authUser$ = new BehaviorSubject<AuthUser | null>(null);
  const cartCount$ = new BehaviorSubject(0);
  const wishlistCount$ = new BehaviorSubject(0);
  const theme = signal<'light' | 'dark' | 'github' | 'github-dark'>('light');

  beforeEach(() => {
    authUser$.next(null);
    cartCount$.next(0);
    wishlistCount$.next(0);
    theme.set('light');

    TestBed.configureTestingModule({
      imports: [HeaderComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { authUser$, logout: vi.fn() } },
        { provide: CartService, useValue: { cartCount$ } },
        { provide: WishlistService, useValue: { wishlistCount$ } },
        {
          provide: ThemeService,
          useValue: {
            theme,
            themes: ['light', 'dark', 'github', 'github-dark'],
            isDark: () => false,
            isGithub: () => false,
            isGithubDark: () => false,
            toggle: vi.fn(),
            setTheme: vi.fn(),
            getThemeLabel: (value: string) => value
          }
        }
      ]
    });
  });

  it('renders role-aware navigation and cart/wishlist badges', () => {
    authUser$.next({ id: '1', email: 'admin@test.local', fullName: 'Admin User', role: 'Admin' });
    cartCount$.next(3);
    wishlistCount$.next(2);
    const fixture = TestBed.createComponent(HeaderComponent);
    fixture.detectChanges();

    const accountButton = fixture.nativeElement.querySelector('.account-menu__trigger') as HTMLButtonElement;
    accountButton.click();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Admin Area');
    expect(text).not.toContain('Seller Dashboard');
    expect(Array.from(fixture.nativeElement.querySelectorAll('.header__cart-count')).map((node: any) => node.textContent.trim()))
      .toEqual(['2', '3']);
  });

  it('opens and closes the mobile menu from the toggle', () => {
    const fixture = TestBed.createComponent(HeaderComponent);
    fixture.detectChanges();
    const toggle = fixture.nativeElement.querySelector('.header__mobile-toggle') as HTMLButtonElement;

    toggle.click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.mobile-menu')).not.toBeNull();
    expect(toggle.getAttribute('aria-expanded')).toBe('true');

    toggle.click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.mobile-menu')).toBeNull();
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
  });
});
