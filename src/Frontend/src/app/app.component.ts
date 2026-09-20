import { Component, OnDestroy, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { AuthService } from './auth.service';
import { Device, DeviceSettings, DevicesService } from './devices.service';
import { CurrentUser, ManagedUser, UsersService } from './users.service';

@Component({
  standalone: false,
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit, OnDestroy {
  ready = false;
  loading = false;
  saving = false;
  error = '';
  notice = '';
  devices: Device[] = [];
  currentUser?: CurrentUser;
  users: ManagedUser[] = [];
  changingGrant = '';
  selected?: Device;
  draft?: DeviceSettings;
  private subscription?: Subscription;
  private generation = 0;

  constructor(public auth: AuthService, private api: DevicesService, private usersApi: UsersService) {}

  async ngOnInit(): Promise<void> {
    this.subscription = this.auth.user$.subscribe(user => {
      this.generation++;
      this.devices = [];
      this.currentUser = undefined;
      this.users = [];
      this.selected = undefined;
      this.draft = undefined;
      this.notice = '';
      if (user) void this.load();
    });
    try { await this.auth.initialize(); }
    catch (error) { this.error = this.message(error); }
    finally { this.ready = true; }
  }

  ngOnDestroy(): void { this.subscription?.unsubscribe(); }

  async signIn(): Promise<void> {
    this.error = '';
    try { await this.auth.signIn(); } catch (error) { this.error = this.message(error); }
  }

  async signOut(): Promise<void> {
    try { await this.auth.signOut(); } catch { this.error = 'Unable to sign out. Please try again.'; }
  }

  async load(): Promise<void> {
    const generation = this.generation;
    this.loading = true;
    this.error = '';
    try {
      const currentUser = await this.usersApi.me();
      const [page, userPage] = await Promise.all([
        this.api.list(),
        currentUser.isSystemAdmin ? this.usersApi.list() : Promise.resolve({ items: [] })
      ]);
      if (generation !== this.generation) return;
      this.currentUser = currentUser;
      this.devices = page.items;
      this.users = userPage.items;
    } catch (error) {
      if (generation === this.generation) this.error = this.message(error);
    } finally { this.loading = false; }
  }

  edit(device: Device): void {
    this.selected = device;
    this.draft = { ...device.settings };
    this.notice = '';
    this.error = '';
  }

  cancel(): void { this.selected = undefined; this.draft = undefined; }

  async save(): Promise<void> {
    if (!this.selected || !this.draft) return;
    const generation = this.generation;
    const device = this.selected;
    const settings = { ...this.draft, description: this.draft.description.trim() };
    this.saving = true;
    this.error = '';
    this.notice = '';
    try {
      await this.api.save(device.deviceId, device.chatId, settings);
      if (generation !== this.generation) return;
      device.settings = settings;
      this.notice = 'Settings saved. Future notifications will use your preferences.';
      this.cancel();
    } catch (error) {
      if (generation === this.generation) this.error = this.message(error);
    } finally { this.saving = false; }
  }

  hasGrant(user: ManagedUser, device: Device): boolean {
    return user.grants.some(grant => grant.deviceId === device.deviceId && grant.chatId === device.chatId);
  }

  async setGrant(user: ManagedUser, device: Device, granted: boolean): Promise<void> {
    const key = user.userId + ':' + device.deviceId + ':' + device.chatId;
    this.changingGrant = key;
    this.error = '';
    this.notice = '';
    try {
      await this.usersApi.setGrant(user.userId, device.deviceId, device.chatId, granted);
      if (granted) user.grants = [...user.grants, { deviceId: device.deviceId, chatId: device.chatId }];
      else user.grants = user.grants.filter(item => item.deviceId !== device.deviceId || item.chatId !== device.chatId);
      this.notice = granted ? 'Device access granted.' : 'Device access removed.';
    } catch (error) { this.error = this.message(error); }
    finally { this.changingGrant = ''; }
  }

  private message(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 401) return 'Your session has ended. Please sign in again.';
      if (error.status === 404 || error.status === 403) return 'This device is no longer available to your account.';
      if (error.status === 400) return 'Please check your settings and try again.';
      return 'Unable to reach your devices. Please try again.';
    }
    return error instanceof Error ? error.message : 'Something went wrong. Please try again.';
  }
}
