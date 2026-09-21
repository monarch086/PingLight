import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { CognitoAuthClient } from './cognito-auth-client.service';

describe('AuthService', () => {
  let auth: AuthService;
  let cognito: jasmine.SpyObj<CognitoAuthClient>;

  beforeEach(() => {
    cognito = jasmine.createSpyObj<CognitoAuthClient>('CognitoAuthClient', [
      'configure', 'getCurrentUser', 'fetchAuthSession', 'signIn', 'signUp', 'confirmSignUp',
      'resendSignUpCode', 'resetPassword', 'confirmResetPassword', 'signOut'
    ]);
    cognito.getCurrentUser.and.rejectWith(Object.assign(new Error(), { name: 'UserUnAuthenticatedException' }));
    TestBed.configureTestingModule({ providers: [{ provide: CognitoAuthClient, useValue: cognito }] });
    auth = TestBed.inject(AuthService);
    spyOn(window, 'fetch').and.resolveTo(new Response(JSON.stringify({
      apiUrl: 'https://api.example.test', clientId: 'test-client', userPoolId: 'eu-central-1_test'
    })));
  });

  it('configures Cognito without opening a hosted login page', async () => {
    await auth.initialize();
    expect(cognito.configure).toHaveBeenCalledWith('eu-central-1_test', 'test-client');
    expect(auth.user$.value).toBeNull();
  });

  it('restores an existing session after a page refresh', async () => {
    cognito.getCurrentUser.and.resolveTo({ username: 'person', userId: 'user-a' });
    cognito.fetchAuthSession.and.resolveTo(session('person@example.test'));
    await auth.initialize();
    expect(auth.user$.value?.profile.email).toBe('person@example.test');
  });

  it('signs in through the SRP flow and publishes the session', async () => {
    cognito.signIn.and.resolveTo({ isSignedIn: true, nextStep: { signInStep: 'DONE' } });
    cognito.fetchAuthSession.and.resolveTo(session('person@example.test'));
    await auth.initialize();
    const result = await auth.signIn(' Person@Example.Test ', 'Password1234');
    expect(result).toBe('SIGNED_IN');
    expect(cognito.signIn).toHaveBeenCalledWith({
      username: 'person@example.test', password: 'Password1234', options: { authFlowType: 'USER_SRP_AUTH' }
    });
    expect(auth.user$.value?.profile.email).toBe('person@example.test');
  });

  it('opens confirmation for an unconfirmed account', async () => {
    cognito.signIn.and.rejectWith(Object.assign(new Error(), { name: 'UserNotConfirmedException' }));
    await auth.initialize();
    expect(await auth.signIn('person@example.test', 'Password1234')).toBe('CONFIRM_SIGN_UP');
  });

  it('opens password recovery when Cognito requires a reset', async () => {
    cognito.signIn.and.rejectWith(Object.assign(new Error(), { name: 'PasswordResetRequiredException' }));
    await auth.initialize();
    expect(await auth.signIn('person@example.test', 'Password1234')).toBe('RESET_PASSWORD');
  });

  it('registers a normalized email address', async () => {
    cognito.signUp.and.resolveTo({
      isSignUpComplete: false,
      nextStep: { signUpStep: 'CONFIRM_SIGN_UP', codeDeliveryDetails: { deliveryMedium: 'EMAIL' } }
    });
    await auth.initialize();
    await auth.signUp(' Person@Example.Test ', 'Password1234');
    expect(cognito.signUp).toHaveBeenCalledWith({
      username: 'person@example.test', password: 'Password1234',
      options: { userAttributes: { email: 'person@example.test' } }
    });
  });

  it('returns the Cognito access token for API calls', async () => {
    cognito.fetchAuthSession.and.resolveTo(session('person@example.test'));
    await auth.initialize();
    expect(await auth.accessToken()).toBe('access-token');
  });
});

function session(email: string): never {
  return {
    tokens: {
      accessToken: { payload: {}, toString: () => 'access-token' },
      idToken: { payload: { email }, toString: () => 'id-token' }
    }
  } as never;
}
