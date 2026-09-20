import { Component, OnDestroy, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { AuthService } from './auth.service';
import { Device, DeviceSettings, DevicesService } from './devices.service';

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
  selected?: Device;
  draft?: DeviceSettings;
  private subscription?: Subscription;
  private generation = 0;

  constructor(public auth: AuthService, private api: DevicesService) {}

  async ngOnInit(): Promise<void> {
    this.subscription = this.auth.user$.subscribe(user => {
      this.generation++;
      this.devices = [];
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
      const page = await this.api.list();
      if (generation !== this.generation) return;
      this.devices = page.items;
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
