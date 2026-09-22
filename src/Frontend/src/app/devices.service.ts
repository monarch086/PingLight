import { Injectable, inject } from '@angular/core';
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
  lastTurnOff?: TurnOffPeriod | null;
}
export interface DevicePage {
  items: Device[];
}
export interface TurnOffPeriod {
  startedAt: string;
  endedAt: string | null;
}
export interface TurnOffPage {
  items: TurnOffPeriod[];
  page: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

@Injectable({ providedIn: 'root' })
export class DevicesService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);

  async list(): Promise<DevicePage> {
    const headers = await this.headers();
    return firstValueFrom(
      this.http.get<DevicePage>(this.baseUrl + '/devices', {
        headers,
      }),
    );
  }

  async save(
    deviceId: string,
    chatId: string,
    settings: DeviceSettings,
  ): Promise<void> {
    const headers = await this.headers();
    await firstValueFrom(
      this.http.put(
        this.baseUrl +
          '/devices/' +
          encodeURIComponent(deviceId) +
          '/destinations/' +
          encodeURIComponent(chatId) +
          '/settings',
        settings,
        { headers },
      ),
    );
  }

  async setActive(
    deviceId: string,
    chatId: string,
    isActive: boolean,
  ): Promise<void> {
    const headers = await this.headers();
    const url =
      this.baseUrl +
      '/devices/' +
      encodeURIComponent(deviceId) +
      '/destinations/' +
      encodeURIComponent(chatId) +
      '/notifications';
    await firstValueFrom(this.http.put(url, { isActive }, { headers }));
  }

  async listTurnOffs(
    deviceId: string,
    chatId: string,
    page: number,
  ): Promise<TurnOffPage> {
    const headers = await this.headers();
    const url =
      this.baseUrl +
      '/devices/' +
      encodeURIComponent(deviceId) +
      '/destinations/' +
      encodeURIComponent(chatId) +
      '/turn-offs?page=' +
      page;
    return firstValueFrom(this.http.get<TurnOffPage>(url, { headers }));
  }

  async removeLastTurnOff(deviceId: string, chatId: string): Promise<void> {
    const headers = await this.headers();
    const url =
      this.baseUrl +
      '/devices/' +
      encodeURIComponent(deviceId) +
      '/destinations/' +
      encodeURIComponent(chatId) +
      '/turn-offs/latest';
    await firstValueFrom(this.http.delete(url, { headers }));
  }

  private get baseUrl(): string {
    return this.auth.config!.apiUrl.replace(/\/$/, '');
  }
  private async headers(): Promise<HttpHeaders> {
    return new HttpHeaders({
      Authorization: 'Bearer ' + (await this.auth.accessToken()),
    });
  }
}
