import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  NgOptionTemplateDirective,
  NgSelectComponent,
} from '@ng-select/ng-select';
import { Device, DevicesService } from './devices.service';
import { DeviceOption } from './device-option.model';
import { ManagedUser, UsersService } from './users.service';
import { errorMessage } from './error-message';
import { WorkspaceSession } from './workspace-session.service';

@Component({
  selector: 'app-users-page',
  imports: [FormsModule, NgSelectComponent, NgOptionTemplateDirective],
  templateUrl: './users-page.component.html',
  styleUrl: './users-page.component.scss',
})
export class UsersPageComponent implements OnInit, OnDestroy {
  private api = inject(DevicesService);
  private usersApi = inject(UsersService);
  session = inject(WorkspaceSession);
  private router = inject(Router);

  loading = false;
  error = '';
  notice = '';
  devices: Device[] = [];
  deviceOptions: DeviceOption[] = [];
  users: ManagedUser[] = [];
  editingUserId = '';
  draftKeys: string[] = [];
  saving = false;
  private destroyed = false;

  ngOnInit(): void {
    if (!this.session.currentUser?.isSystemAdmin) {
      void this.router.navigateByUrl('/devices', { replaceUrl: true });
      return;
    }
    void this.load();
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.devices = [];
    this.deviceOptions = [];
    this.users = [];
  }

  async load(): Promise<void> {
    if (
      !this.session.currentUser?.isSystemAdmin ||
      this.saving ||
      this.editingUserId
    )
      return;
    this.loading = true;
    this.error = '';
    try {
      const [devices, users] = await Promise.all([
        this.api.list(),
        this.usersApi.list(),
      ]);
      if (this.destroyed) return;
      this.devices = devices.items;
      this.deviceOptions = this.devices.map((device) => ({
        key: this.deviceKey(device.deviceId, device.chatId),
        label: device.settings.description || device.deviceId,
        details: `${device.deviceId} · ${device.chatId}`,
        device,
      }));
      this.users = users.items;
    } catch (error) {
      if (!this.destroyed) this.error = errorMessage(error);
    } finally {
      this.loading = false;
    }
  }

  selectedLabels(user: ManagedUser): string {
    const labels = this.deviceOptions
      .filter((option) => this.hasGrant(user, option.device))
      .map((option) => option.label);
    return labels.length ? labels.join(', ') : 'Немає призначених пристроїв';
  }

  edit(user: ManagedUser): void {
    if (this.saving || this.editingUserId || user.isSystemAdmin) return;
    this.editingUserId = user.userId;
    this.draftKeys = this.deviceOptions
      .filter((option) => this.hasGrant(user, option.device))
      .map((option) => option.key);
    this.error = '';
    this.notice = '';
  }

  cancelEdit(): void {
    if (this.saving) return;
    this.editingUserId = '';
    this.draftKeys = [];
  }

  hasChanges(user: ManagedUser): boolean {
    return this.deviceOptions.some(
      (option) =>
        this.draftKeys.includes(option.key) !==
        this.hasGrant(user, option.device),
    );
  }

  async save(user: ManagedUser): Promise<void> {
    if (
      this.saving ||
      this.editingUserId !== user.userId ||
      !this.session.currentUser?.isSystemAdmin
    )
      return;
    const changes = this.deviceOptions.filter(
      (option) =>
        this.draftKeys.includes(option.key) !==
        this.hasGrant(user, option.device),
    );
    if (!changes.length) return;
    this.saving = true;
    this.session.pendingWrites++;
    this.error = '';
    this.notice = '';
    try {
      for (const option of changes) {
        const { device } = option;
        const granted = this.draftKeys.includes(option.key);
        await this.usersApi.setGrant(
          user.userId,
          device.deviceId,
          device.chatId,
          granted,
        );
        if (this.destroyed) return;
        user.grants = granted
          ? [
              ...user.grants,
              { deviceId: device.deviceId, chatId: device.chatId },
            ]
          : user.grants.filter(
              (grant) =>
                grant.deviceId !== device.deviceId ||
                grant.chatId !== device.chatId,
            );
      }
      this.editingUserId = '';
      this.draftKeys = [];
      this.notice = 'Доступ до пристроїв збережено.';
    } catch (error) {
      if (!this.destroyed) this.error = errorMessage(error);
    } finally {
      this.saving = false;
      this.session.pendingWrites--;
    }
  }

  private hasGrant(user: ManagedUser, device: Device): boolean {
    return user.grants.some(
      (grant) =>
        grant.deviceId === device.deviceId && grant.chatId === device.chatId,
    );
  }

  private deviceKey(deviceId: string, chatId: string): string {
    return JSON.stringify([deviceId, chatId]);
  }
}
