import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Device, DevicesService } from './devices.service';
import { ManagedUser, UsersService } from './users.service';
import { errorMessage } from './error-message';
import { WorkspaceSession } from './workspace-session.service';

@Component({
  selector: 'app-users-page',
  imports: [CommonModule],
  templateUrl: './users-page.component.html',
  styleUrl: './users-page.component.scss'
})
export class UsersPageComponent implements OnInit, OnDestroy {
  loading = false;
  error = '';
  notice = '';
  devices: Device[] = [];
  users: ManagedUser[] = [];
  changingGrant = '';
  private destroyed = false;

  constructor(private api: DevicesService, private usersApi: UsersService,
    public session: WorkspaceSession, private router: Router) {}

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
    this.users = [];
  }

  async load(): Promise<void> {
    if (!this.session.currentUser?.isSystemAdmin) return;
    this.loading = true;
    this.error = '';
    try {
      const [devices, users] = await Promise.all([this.api.list(), this.usersApi.list()]);
      if (this.destroyed) return;
      this.devices = devices.items;
      this.users = users.items;
    } catch (error) {
      if (!this.destroyed) this.error = errorMessage(error);
    } finally { this.loading = false; }
  }

  hasGrant(user: ManagedUser, device: Device): boolean {
    return user.grants.some(grant => grant.deviceId === device.deviceId && grant.chatId === device.chatId);
  }

  async setGrant(user: ManagedUser, device: Device, granted: boolean): Promise<void> {
    if (this.changingGrant || !this.session.currentUser?.isSystemAdmin) return;
    this.changingGrant = user.userId + ':' + device.deviceId + ':' + device.chatId;
    this.session.pendingWrites++;
    this.error = '';
    this.notice = '';
    try {
      await this.usersApi.setGrant(user.userId, device.deviceId, device.chatId, granted);
      if (this.destroyed) return;
      if (granted) user.grants = [...user.grants, { deviceId: device.deviceId, chatId: device.chatId }];
      else user.grants = user.grants.filter(item => item.deviceId !== device.deviceId || item.chatId !== device.chatId);
      this.notice = granted ? 'Device access granted.' : 'Device access removed.';
    } catch (error) {
      if (!this.destroyed) this.error = errorMessage(error);
    } finally {
      this.changingGrant = '';
      this.session.pendingWrites--;
    }
  }
}
