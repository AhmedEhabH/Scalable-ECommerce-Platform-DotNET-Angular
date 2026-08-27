import { Injectable, signal, effect, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export type Theme = 'light' | 'dark';

const STORAGE_KEY = 'app-theme';

const THEMES: Theme[] = ['light', 'dark'];

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private platformId = inject(PLATFORM_ID);
  
  readonly theme = signal<Theme>(this.getInitialTheme());
  readonly themes = THEMES;
  readonly isDark = () => this.theme() === 'dark';

  constructor() {
    if (isPlatformBrowser(this.platformId)) {
      effect(() => {
        this.applyTheme(this.theme());
      });
    }
  }

  private getInitialTheme(): Theme {
    if (isPlatformBrowser(this.platformId)) {
      const saved = localStorage.getItem(STORAGE_KEY);
      if (saved && THEMES.includes(saved as Theme)) {
        return saved as Theme;
      }
      if (window.matchMedia?.('(prefers-color-scheme: dark)').matches) {
        return 'dark';
      }
    }
    return 'light';
  }

  private applyTheme(theme: Theme): void {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem(STORAGE_KEY, theme);
  }

  toggle(): void {
    const currentIndex = THEMES.indexOf(this.theme());
    const nextIndex = (currentIndex + 1) % THEMES.length;
    this.theme.set(THEMES[nextIndex]);
  }

  setTheme(theme: Theme): void {
    if (THEMES.includes(theme)) {
      this.theme.set(theme);
    }
  }

  getThemeLabel(theme: Theme): string {
    switch (theme) {
      case 'light': return 'Light';
      case 'dark': return 'Dark';
    }
  }
}
