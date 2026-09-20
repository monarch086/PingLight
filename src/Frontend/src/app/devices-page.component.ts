
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Device, DeviceSettings, DevicesService } from './devices.service';
import { errorMessage } from './error-message';
import { WorkspaceSession } from './workspace-session.service';

@Component({
  selector: 'app-devices-page',
  imports: [FormsModule],
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
}
