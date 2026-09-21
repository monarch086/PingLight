import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { BehaviorSubject } from 'rxjs';
import { Router } from '@angular/router';
import type { UserManager, User } from 'oidc-client-ts';
import { UserFacingError } from './error-message';

export interface AppConfig {
  apiUrl: string;
  authority: string;
  clientId: string;
  cognitoDomain: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private platformId = inject(PLATFORM_ID);
  private router = inject(Router);

  readonly user$ = new BehaviorSubject<User | null>(null);
  config?: AppConfig;
  private manager?: UserManager;

  async initialize(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) return;
    const response = await fetch('/assets/app-config.json', { cache: 'no-store' });
    if (!response.ok) throw new UserFacingError('Не вдалося завантажити конфігурацію застосунку. Спробуйте ще раз.');
    const config = await response.json() as AppConfig;
    if (!config.apiUrl || !config.authority || !config.clientId || !config.cognitoDomain)
      throw new UserFacingError('Доступ до облікового запису поки недоступний. Спробуйте пізніше.');
    for (const address of [config.apiUrl, config.authority, config.cognitoDomain]) {
      const url = new URL(address);
      if (url.protocol !== 'https:' && !(url.protocol === 'http:' && url.hostname === 'localhost'))
        throw new UserFacingError('Доступ до облікового запису недоступний. Зверніться до служби підтримки.');
    }
    this.config = config;
    const { UserManager, WebStorageStateStore } = await import('oidc-client-ts');
    this.manager = new UserManager({
      authority: config.authority,
      client_id: config.clientId,
      redirect_uri: window.location.origin + '/auth/callback',
      response_type: 'code',
      scope: 'openid email profile',
      automaticSilentRenew: true,
      // Keep the login within this browser tab so a page refresh can restore it.
      userStore: new WebStorageStateStore({ store: window.sessionStorage }),
      stateStore: new WebStorageStateStore({ store: window.sessionStorage }),
      loadUserInfo: false,
      revokeTokenTypes: ['refresh_token']
    });
    this.manager.events.addUserLoaded(user => this.user$.next(user));
    this.manager.events.addUserUnloaded(() => this.user$.next(null));
    this.manager.events.addAccessTokenExpired(() => this.user$.next(null));
    this.manager.events.addSilentRenewError(() => this.user$.next(null));
    await this.manager.clearStaleState();
    if (window.location.pathname === '/auth/callback') {
      let returnUrl = '/devices';
      try {
        const user = await this.manager.signinRedirectCallback();
        this.user$.next(user);
        const state = user.state as { returnUrl?: unknown } | undefined;
        if (typeof state?.returnUrl === 'string' && /^\/(devices|users)([?#]|$)/.test(state.returnUrl))
          returnUrl = state.returnUrl;
      } catch {
        throw new UserFacingError('Не вдалося завершити вхід. Спробуйте увійти ще раз.');
      } finally {
        await this.router.navigateByUrl(returnUrl, { replaceUrl: true });
      }
      return;
    }
    const user = await this.manager.getUser();
    if (user && !user.expired) this.user$.next(user);
    else if (user) await this.manager.removeUser();
  }

  async signIn(): Promise<void> {
    if (!this.manager) throw new UserFacingError('Доступ до облікового запису поки недоступний.');
    await this.manager.signinRedirect({ state: { returnUrl: this.router.url } });
  }

  async signOut(): Promise<void> {
    if (!this.manager || !this.config) return;
    try { await this.manager.revokeTokens(['refresh_token']); } catch { /* Always clear the local session. */ }
    await this.manager.removeUser();
    const logout = new URL('/logout', this.config.cognitoDomain);
    logout.searchParams.set('client_id', this.config.clientId);
    logout.searchParams.set('logout_uri', window.location.origin + '/');
    window.location.assign(logout.toString());
  }

  async accessToken(): Promise<string> {
    const user = await this.manager?.getUser();
    if (!user || user.expired) {
      this.user$.next(null);
      throw new UserFacingError('Сеанс завершено. Увійдіть ще раз.');
    }
    return user.access_token;
  }
}
