import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { User, UserManager } from 'oidc-client-ts';
import { AuthService } from './auth.service';

describe('Sign-in return route', () => {
  let originalUrl: string;
  let auth: AuthService;
  let router: Router;
  let getUser: jasmine.Spy;

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
    getUser = spyOn(UserManager.prototype, 'getUser').and.resolveTo(null);
  });

  afterEach(() => window.history.replaceState({}, '', originalUrl));

  it('includes the selected page in the sign-in transaction', async () => {
    const signIn = spyOn(UserManager.prototype, 'signinRedirect').and.resolveTo();
    await router.navigateByUrl('/users');
    await auth.initialize();
    await auth.signIn();
    expect(signIn).toHaveBeenCalledWith({ state: { returnUrl: '/users' } });
  });

  it('restores an unexpired user after a page refresh', async () => {
    const restored = { expired: false, profile: { email: 'person@example.test' } } as User;
    getUser.and.resolveTo(restored);
    await auth.initialize();
    expect(auth.user$.value).toBe(restored);
  });

  it('clears an expired stored user', async () => {
    getUser.and.resolveTo({ expired: true } as User);
    const removeUser = spyOn(UserManager.prototype, 'removeUser').and.resolveTo();
    await auth.initialize();
    expect(removeUser).toHaveBeenCalled();
    expect(auth.user$.value).toBeNull();
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
