import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AuthService } from './auth.service';

export interface CurrentUser { userId: string; email: string; isSystemAdmin: boolean; }
export interface DeviceGrant { deviceId: string; chatId: string; }
export interface ManagedUser extends CurrentUser { grants: DeviceGrant[]; }
export interface UserPage { items: ManagedUser[]; }

@Injectable({ providedIn: 'root' })
export class UsersService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);


  async me(): Promise<CurrentUser> {
    return firstValueFrom(this.http.get<CurrentUser>(this.baseUrl + '/users/me', { headers: await this.headers() }));
  }

  async list(): Promise<UserPage> {
    return firstValueFrom(this.http.get<UserPage>(this.baseUrl + '/users', { headers: await this.headers() }));
  }

  async setGrant(userId: string, deviceId: string, chatId: string, granted: boolean): Promise<void> {
    const url = this.baseUrl + '/users/' + encodeURIComponent(userId) + '/devices/' + encodeURIComponent(deviceId) +
      '/destinations/' + encodeURIComponent(chatId);
    const request = granted ? this.http.put(url, null, { headers: await this.headers() })
      : this.http.delete(url, { headers: await this.headers() });
    await firstValueFrom(request);
  }

  private get baseUrl(): string { return this.auth.config!.apiUrl.replace(/\/$/, ''); }
  private async headers(): Promise<HttpHeaders> {
    return new HttpHeaders({ Authorization: 'Bearer ' + await this.auth.accessToken() });
  }
}
