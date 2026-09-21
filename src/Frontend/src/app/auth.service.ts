import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { CognitoAuthClient } from './cognito-auth-client.service';
import { UserFacingError } from './error-message';

export interface AppConfig {
  apiUrl: string;
  clientId: string;
  userPoolId: string;
}

export interface SignedInUser {
  profile: { email?: string };
}

export type SignInResult = 'SIGNED_IN' | 'CONFIRM_SIGN_UP' | 'RESET_PASSWORD';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private platformId = inject(PLATFORM_ID);
  private cognito = inject(CognitoAuthClient);

  readonly user$ = new BehaviorSubject<SignedInUser | null>(null);
  config?: AppConfig;

  async initialize(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) return;
    const response = await fetch('/assets/app-config.json', {
      cache: 'no-store',
    });
    if (!response.ok)
      throw new UserFacingError(
        'Не вдалося завантажити конфігурацію застосунку. Спробуйте ще раз.',
      );
    const config = (await response.json()) as AppConfig;
    if (!config.apiUrl || !config.clientId || !config.userPoolId)
      throw new UserFacingError(
        'Доступ до облікового запису поки недоступний. Спробуйте пізніше.',
      );
    const apiUrl = new URL(config.apiUrl);
    if (
      apiUrl.protocol !== 'https:' &&
      !(apiUrl.protocol === 'http:' && apiUrl.hostname === 'localhost')
    )
      throw new UserFacingError(
        'Доступ до облікового запису недоступний. Зверніться до служби підтримки.',
      );
    this.config = config;
    this.cognito.configure(config.userPoolId, config.clientId);
    try {
      await this.cognito.getCurrentUser();
      await this.publishSession();
    } catch (error) {
      if (!isNoCurrentUser(error)) throw error;
    }
  }

  async signIn(email: string, password: string): Promise<SignInResult> {
    this.ensureConfigured();
    try {
      const result = await this.cognito.signIn({
        username: normalizeEmail(email),
        password,
        options: { authFlowType: 'USER_SRP_AUTH' },
      });
      switch (result.nextStep.signInStep) {
        case 'DONE':
          await this.publishSession();
          return 'SIGNED_IN';
        case 'CONFIRM_SIGN_UP':
          return 'CONFIRM_SIGN_UP';
        case 'RESET_PASSWORD':
          return 'RESET_PASSWORD';
        default:
          throw new UserFacingError(
            'Цей спосіб входу не підтримується. Зверніться до служби підтримки.',
          );
      }
    } catch (error) {
      if (errorName(error) === 'UserNotConfirmedException')
        return 'CONFIRM_SIGN_UP';
      if (errorName(error) === 'PasswordResetRequiredException')
        return 'RESET_PASSWORD';
      throw authError(error);
    }
  }

  async signUp(email: string, password: string): Promise<void> {
    this.ensureConfigured();
    try {
      await this.cognito.signUp({
        username: normalizeEmail(email),
        password,
        options: { userAttributes: { email: normalizeEmail(email) } },
      });
    } catch (error) {
      throw authError(error);
    }
  }

  async confirmSignUp(email: string, code: string): Promise<void> {
    this.ensureConfigured();
    try {
      await this.cognito.confirmSignUp({
        username: normalizeEmail(email),
        confirmationCode: code.trim(),
      });
    } catch (error) {
      throw authError(error);
    }
  }

  async resendSignUpCode(email: string): Promise<void> {
    this.ensureConfigured();
    try {
      await this.cognito.resendSignUpCode({ username: normalizeEmail(email) });
    } catch (error) {
      throw authError(error);
    }
  }

  async requestPasswordReset(email: string): Promise<void> {
    this.ensureConfigured();
    try {
      await this.cognito.resetPassword({ username: normalizeEmail(email) });
    } catch (error) {
      throw authError(error);
    }
  }

  async confirmPasswordReset(
    email: string,
    code: string,
    newPassword: string,
  ): Promise<void> {
    this.ensureConfigured();
    try {
      await this.cognito.confirmResetPassword({
        username: normalizeEmail(email),
        confirmationCode: code.trim(),
        newPassword,
      });
    } catch (error) {
      throw authError(error);
    }
  }

  async signOut(): Promise<void> {
    try {
      await this.cognito.signOut();
    } finally {
      this.user$.next(null);
    }
  }

  async accessToken(): Promise<string> {
    try {
      const session = await this.cognito.fetchAuthSession();
      const token = session.tokens?.accessToken;
      if (!token) throw new Error('No access token');
      return token.toString();
    } catch {
      this.user$.next(null);
      throw new UserFacingError('Сеанс завершено. Увійдіть ще раз.');
    }
  }

  private async publishSession(): Promise<void> {
    const session = await this.cognito.fetchAuthSession();
    const email = session.tokens?.idToken?.payload['email'];
    this.user$.next({
      profile: { email: typeof email === 'string' ? email : undefined },
    });
  }

  private ensureConfigured(): void {
    if (!this.config)
      throw new UserFacingError(
        'Доступ до облікового запису поки недоступний.',
      );
  }
}

function normalizeEmail(email: string): string {
  return email.trim().toLowerCase();
}

function errorName(error: unknown): string {
  return typeof error === 'object' && error !== null && 'name' in error
    ? String(error.name)
    : '';
}

function isNoCurrentUser(error: unknown): boolean {
  return errorName(error) === 'UserUnAuthenticatedException';
}

function authError(error: unknown): UserFacingError {
  const messages: Record<string, string> = {
    AliasExistsException:
      'Обліковий запис із цією електронною адресою вже існує.',
    CodeMismatchException:
      'Код неправильний. Перевірте його та спробуйте ще раз.',
    ExpiredCodeException: 'Термін дії коду минув. Запросіть новий код.',
    InvalidPasswordException: 'Пароль не відповідає вимогам безпеки.',
    LimitExceededException:
      'Забагато спроб. Зачекайте трохи та спробуйте ще раз.',
    NotAuthorizedException: 'Неправильна електронна адреса або пароль.',
    PasswordResetRequiredException: 'Потрібно створити новий пароль.',
    TooManyRequestsException:
      'Забагато спроб. Зачекайте трохи та спробуйте ще раз.',
    UserAlreadyAuthenticatedException: 'Ви вже ввійшли до облікового запису.',
    UsernameExistsException:
      'Обліковий запис із цією електронною адресою вже існує.',
    UserNotFoundException: 'Неправильна електронна адреса або пароль.',
  };
  return new UserFacingError(
    messages[errorName(error)] || 'Не вдалося виконати дію. Спробуйте ще раз.',
  );
}
