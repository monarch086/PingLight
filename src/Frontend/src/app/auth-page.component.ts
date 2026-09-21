import { Component, EventEmitter, Output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from './auth.service';
import { errorMessage } from './error-message';

type AuthMode =
  | 'sign-in'
  | 'sign-up'
  | 'confirm-sign-up'
  | 'forgot-password'
  | 'confirm-password';

@Component({
  selector: 'app-auth-page',
  imports: [FormsModule],
  templateUrl: './auth-page.component.html',
  styleUrl: './auth-page.component.scss',
})
export class AuthPageComponent {
  private auth = inject(AuthService);

  @Output() accountClosed = new EventEmitter<void>();

  mode: AuthMode = 'sign-in';
  email = '';
  password = '';
  passwordConfirmation = '';
  code = '';
  busy = false;
  error = '';
  notice = '';

  async submitSignIn(): Promise<void> {
    await this.run(async () => {
      const result = await this.auth.signIn(this.email, this.password);
      if (result === 'CONFIRM_SIGN_UP') this.show('confirm-sign-up');
      if (result === 'RESET_PASSWORD') {
        await this.auth.requestPasswordReset(this.email);
        this.show('confirm-password');
        this.notice = 'Ми надіслали код для зміни пароля.';
      }
    });
  }

  async submitSignUp(): Promise<void> {
    if (!this.passwordsMatch()) return;
    await this.run(async () => {
      await this.auth.signUp(this.email, this.password);
      this.show('confirm-sign-up');
      this.notice = 'Ми надіслали код підтвердження на вашу електронну адресу.';
    });
  }

  async submitConfirmation(): Promise<void> {
    await this.run(async () => {
      await this.auth.confirmSignUp(this.email, this.code);
      this.password = '';
      this.code = '';
      this.show('sign-in');
      this.notice = 'Обліковий запис підтверджено. Тепер увійдіть.';
    });
  }

  async resendConfirmation(): Promise<void> {
    await this.run(async () => {
      await this.auth.resendSignUpCode(this.email);
      this.notice = 'Новий код підтвердження надіслано.';
    });
  }

  async beginPasswordReset(): Promise<void> {
    await this.run(async () => {
      await this.auth.requestPasswordReset(this.email);
      this.show('confirm-password');
      this.notice =
        'Якщо обліковий запис існує, ми надіслали код для зміни пароля.';
    });
  }

  async submitPasswordReset(): Promise<void> {
    if (!this.passwordsMatch()) return;
    await this.run(async () => {
      await this.auth.confirmPasswordReset(
        this.email,
        this.code,
        this.password,
      );
      this.password = '';
      this.passwordConfirmation = '';
      this.code = '';
      this.show('sign-in');
      this.notice = 'Пароль змінено. Тепер увійдіть.';
    });
  }

  show(mode: AuthMode): void {
    this.mode = mode;
    this.error = '';
    this.notice = '';
    this.code = '';
    this.password = '';
    this.passwordConfirmation = '';
  }

  private passwordsMatch(): boolean {
    if (this.password === this.passwordConfirmation) return true;
    this.error = 'Паролі не збігаються.';
    return false;
  }

  private async run(action: () => Promise<void>): Promise<void> {
    if (this.busy) return;
    this.busy = true;
    this.error = '';
    this.notice = '';
    try {
      await action();
    } catch (error) {
      this.error = errorMessage(error);
    } finally {
      this.busy = false;
    }
  }
}
