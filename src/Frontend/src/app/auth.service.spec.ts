import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { User, UserManager } from 'oidc-client-ts';
import { AuthService } from './auth.service';

describe('Sign-in return route', () => {
  let originalUrl: string;
  let auth: AuthService;
  let router: Router;

  beforeEach(() => {
    originalUrl = window.location.href;
    TestBed.configureTestingModule({ providers: [provideRouter([
      { path: 'users', children: [] }, { path: 'devices', children: [] }
    ])] });
    auth = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
    spyOn(window, 'fetch').and.resolveTo(new Response(JSON.stringify({
      apiUrl: 'https://api.example.test', authority: 'https://auth.example.test',
      clientId: 'test-client', cognitoDomain: 'https://login.example.test'
    })));
    spyOn(UserManager.prototype, 'clearStaleState').and.resolveTo();
  });

  afterEach(() => window.history.replaceState({}, '', originalUrl));

  it('includes the selected page in the sign-in transaction', async () => {
    const signIn = spyOn(UserManager.prototype, 'signinRedirect').and.resolveTo();
    await router.navigateByUrl('/users');
    await auth.initialize();
    await auth.signIn();
    expect(signIn).toHaveBeenCalledWith({ state: { returnUrl: '/users' } });
  });

  it('returns to the selected page after the callback', async () => {
    window.history.replaceState({}, '', '/auth/callback?code=test');
    spyOn(UserManager.prototype, 'signinRedirectCallback').and.resolveTo({ state: { returnUrl: '/users' } } as User);
    await auth.initialize();
    expect(router.url).toBe('/users');
  });

  it('uses the device page for an untrusted return URL', async () => {
    window.history.replaceState({}, '', '/auth/callback?code=test');
    spyOn(UserManager.prototype, 'signinRedirectCallback').and.resolveTo({ state: { returnUrl: '//example.test' } } as User);
    await auth.initialize();
    expect(router.url).toBe('/devices');
  });
});
