import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { BehaviorSubject } from 'rxjs';
import type { UserManager, User } from 'oidc-client-ts';

export interface AppConfig {
  apiUrl: string;
  authority: string;
  clientId: string;
  cognitoDomain: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly user$ = new BehaviorSubject<User | null>(null);
  config?: AppConfig;
  private manager?: UserManager;

  constructor(@Inject(PLATFORM_ID) private platformId: object) {}

  async initialize(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) return;
    const response = await fetch('/assets/app-config.json', { cache: 'no-store' });
    if (!response.ok) throw new Error('Unable to load application configuration. Please try again.');
    const config = await response.json() as AppConfig;
    if (!config.apiUrl || !config.authority || !config.clientId || !config.cognitoDomain)
      throw new Error('Account access is not available yet. Please try again later.');
    for (const address of [config.apiUrl, config.authority, config.cognitoDomain]) {
      const url = new URL(address);
      if (url.protocol !== 'https:' && !(url.protocol === 'http:' && url.hostname === 'localhost'))
        throw new Error('Account access is not available. Please contact support.');
    }
    this.config = config;
    const { UserManager, WebStorageStateStore, InMemoryWebStorage } = await import('oidc-client-ts');
    this.manager = new UserManager({
      authority: config.authority,
      client_id: config.clientId,
      redirect_uri: window.location.origin + '/auth/callback',
      response_type: 'code',
      scope: 'openid email profile',
      automaticSilentRenew: true,
      // Tokens stay in memory. Session storage holds only the short-lived PKCE transaction.
      userStore: new WebStorageStateStore({ store: new InMemoryWebStorage() }),
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
      try {
        await this.manager.signinRedirectCallback();
      } catch {
        throw new Error('Sign-in could not be completed. Please sign in again.');
      } finally {
        window.history.replaceState({}, document.title, '/');
      }
    }
  }

  async signIn(): Promise<void> {
    if (!this.manager) throw new Error('Account access is not available yet.');
    await this.manager.signinRedirect();
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
      throw new Error('Your session has ended. Please sign in again.');
    }
    return user.access_token;
  }
}
