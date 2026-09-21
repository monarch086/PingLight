import { LOCALE_ID, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeUk from '@angular/common/locales/uk';
import { Subscription } from 'rxjs';
import { AuthService } from './auth.service';
import { UsersService } from './users.service';
import { WorkspaceSession } from './workspace-session.service';
import { errorMessage } from './error-message';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { WelcomeComponent } from './welcome.component';
import { AsyncPipe } from '@angular/common';

registerLocaleData(localeUk);

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss'],
    imports: [RouterLink, RouterLinkActive, RouterOutlet, WelcomeComponent, AsyncPipe],
    providers: [{ provide: LOCALE_ID, useValue: 'uk-UA' }]
})
export class AppComponent implements OnInit, OnDestroy {
  auth = inject(AuthService);
  private usersApi = inject(UsersService);
  session = inject(WorkspaceSession);

  ready = false;
  loading = false;
  error = '';
  private subscription?: Subscription;
  private generation = 0;

  async ngOnInit(): Promise<void> {
    this.subscription = this.auth.user$.subscribe(user => {
      this.generation++;
      this.session.currentUser = undefined;
      this.error = '';
      this.loading = false;
      if (user) void this.loadUser();
    });
    try { await this.auth.initialize(); }
    catch (error) { this.error = errorMessage(error); }
    finally { this.ready = true; }
  }

  ngOnDestroy(): void {
    this.generation++;
    this.subscription?.unsubscribe();
    this.session.currentUser = undefined;
  }

  async loadUser(): Promise<void> {
    const generation = this.generation;
    this.loading = true;
    this.error = '';
    try {
      const user = await this.usersApi.me();
      if (generation === this.generation) this.session.currentUser = user;
    } catch (error) {
      if (generation === this.generation) this.error = errorMessage(error);
    } finally {
      if (generation === this.generation) this.loading = false;
    }
  }

  async signIn(): Promise<void> {
    this.error = '';
    try { await this.auth.signIn(); } catch (error) { this.error = errorMessage(error); }
  }

  async signOut(): Promise<void> {
    try { await this.auth.signOut(); } catch { this.error = 'Не вдалося вийти. Спробуйте ще раз.'; }
  }
}
