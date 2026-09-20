import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { AuthService } from './auth.service';
import { UsersService } from './users.service';
import { WorkspaceSession } from './workspace-session.service';
import { errorMessage } from './error-message';

@Component({
  standalone: false,
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit, OnDestroy {
  ready = false;
  loading = false;
  error = '';
  private subscription?: Subscription;
  private generation = 0;

  constructor(public auth: AuthService, private usersApi: UsersService,
    public session: WorkspaceSession) {}

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
    try { await this.auth.signOut(); } catch { this.error = 'Unable to sign out. Please try again.'; }
  }
}
