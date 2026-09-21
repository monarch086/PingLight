
import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Device, DeviceSettings, DevicesService } from './devices.service';
import { errorMessage } from './error-message';
import { WorkspaceSession } from './workspace-session.service';

@Component({
  selector: 'app-devices-page',
  imports: [DatePipe, FormsModule, RouterLink],
  templateUrl: './devices-page.component.html',
  styleUrl: './devices-page.component.scss'
})
export class DevicesPageComponent implements OnInit, OnDestroy {
  private api = inject(DevicesService);
  session = inject(WorkspaceSession);

  loading = false;
  saving = false;
  error = '';
  notice = '';
  devices: Device[] = [];
  selected?: Device;
  draft?: DeviceSettings;
  private changingActive = new Set<string>();
  private removingTurnOff = new Set<string>();
  private destroyed = false;

  get currentUser() { return this.session.currentUser; }

  ngOnInit(): void { void this.load(); }
  ngOnDestroy(): void {
    this.destroyed = true;
    this.devices = [];
    this.cancel();
  }

  async load(): Promise<void> {
    this.loading = true;
    this.error = '';
    try {
      const page = await this.api.list();
      if (!this.destroyed) this.devices = page.items;
    } catch (error) {
      if (!this.destroyed) this.error = errorMessage(error);
    } finally { this.loading = false; }
  }

  edit(device: Device): void {
    this.selected = device;
    this.draft = { ...device.settings };
    this.notice = '';
    this.error = '';
  }

  cancel(): void { this.selected = undefined; this.draft = undefined; }

  isChangingActive(device: Device): boolean {
    return this.changingActive.has(this.deviceKey(device));
  }

  isRemovingTurnOff(device: Device): boolean {
    return this.removingTurnOff.has(this.deviceKey(device));
  }

  async setActive(device: Device, isActive: boolean): Promise<void> {
    const key = this.deviceKey(device);
    if (this.changingActive.has(key)) return;
    this.changingActive.add(key);
    this.session.pendingWrites++;
    this.error = '';
    this.notice = '';
    try {
      await this.api.setActive(device.deviceId, device.chatId, isActive);
      if (this.destroyed) return;
      device.isActive = isActive;
      this.notice = isActive ? 'Device notifications enabled.' : 'Device notifications disabled.';
    } catch (error) {
      if (!this.destroyed) this.error = errorMessage(error);
    } finally {
      this.changingActive.delete(key);
      this.session.pendingWrites--;
    }
  }

  async removeLastTurnOff(device: Device): Promise<void> {
    const key = this.deviceKey(device);
    if (!device.lastTurnOff || this.removingTurnOff.has(key) ||
        !window.confirm('Remove the last turn-off period? This cannot be undone.')) return;
    this.removingTurnOff.add(key);
    this.session.pendingWrites++;
    this.error = '';
    this.notice = '';
    try {
      await this.api.removeLastTurnOff(device.deviceId, device.chatId);
      if (this.destroyed) return;
      await this.load();
      if (!this.destroyed) this.notice = 'Last turn-off removed.';
    } catch (error) {
      if (!this.destroyed) this.error = errorMessage(error);
    } finally {
      this.removingTurnOff.delete(key);
      this.session.pendingWrites--;
    }
  }

  async save(): Promise<void> {
    if (!this.selected || !this.draft || this.saving) return;
    const device = this.selected;
    const settings = { ...this.draft, description: this.draft.description.trim() };
    this.saving = true;
    this.session.pendingWrites++;
    this.error = '';
    this.notice = '';
    try {
      await this.api.save(device.deviceId, device.chatId, settings);
      if (this.destroyed) return;
      device.settings = settings;
      this.notice = 'Settings saved. Future notifications will use your preferences.';
      this.cancel();
    } catch (error) {
      if (!this.destroyed) this.error = errorMessage(error);
    } finally {
      this.saving = false;
      this.session.pendingWrites--;
    }
  }

  private deviceKey(device: Device): string {
    return device.deviceId + ':' + device.chatId;
  }

  duration(startedAt: string, endedAt: string | null): string {
    if (!endedAt) return 'Ongoing';
    const minutes = Math.max(0, Math.round((Date.parse(endedAt) - Date.parse(startedAt)) / 60000));
    const hours = Math.floor(minutes / 60);
    const remainder = minutes % 60;
    return hours ? `${hours}h ${remainder}m` : `${remainder}m`;
  }
}
