import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AuthService } from './auth.service';

export interface DeviceSettings {
  description: string;
  notificationDelaySec: number;
  isDailyStatsEnabled: boolean;
  isWeeklyStatsEnabled: boolean;
  isMonthlyStatsEnabled: boolean;
}
export interface Device {
  deviceId: string;
  chatId: string;
  settings: DeviceSettings;
  isActive: boolean;
}
export interface DevicePage { items: Device[]; }

@Injectable({ providedIn: 'root' })
export class DevicesService {
  constructor(private http: HttpClient, private auth: AuthService) {}

  async list(): Promise<DevicePage> {
    const headers = await this.headers();
    return firstValueFrom(this.http.get<DevicePage>(this.baseUrl + '/devices', {
      headers
    }));
  }

  async save(deviceId: string, chatId: string, settings: DeviceSettings): Promise<void> {
    const headers = await this.headers();
    await firstValueFrom(this.http.put(this.baseUrl + '/devices/' + encodeURIComponent(deviceId) + '/destinations/' + encodeURIComponent(chatId) + '/settings',
      settings, { headers }));
  }

  private get baseUrl(): string { return this.auth.config!.apiUrl.replace(/\/$/, ''); }
  private async headers(): Promise<HttpHeaders> {
    return new HttpHeaders({ Authorization: 'Bearer ' + await this.auth.accessToken() });
  }
}
