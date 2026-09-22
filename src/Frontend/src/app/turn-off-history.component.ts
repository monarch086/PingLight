import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DevicesService, TurnOffPeriod } from './devices.service';
import { errorMessage } from './error-message';

@Component({
  selector: 'app-turn-off-history',
  imports: [DatePipe, RouterLink],
  templateUrl: './turn-off-history.component.html',
  styleUrl: './turn-off-history.component.scss',
})
export class TurnOffHistoryComponent implements OnInit, OnDestroy {
  private api = inject(DevicesService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  deviceId = '';
  chatId = '';
  page = 1;
  periods: TurnOffPeriod[] = [];
  hasPreviousPage = false;
  hasNextPage = false;
  loading = false;
  error = '';
  private destroyed = false;

  ngOnInit(): void {
    this.deviceId = this.route.snapshot.paramMap.get('deviceId') ?? '';
    this.chatId = this.route.snapshot.paramMap.get('chatId') ?? '';
    const requestedPage = Number(
      this.route.snapshot.queryParamMap.get('page') ?? '1',
    );
    this.page =
      Number.isInteger(requestedPage) && requestedPage > 0 ? requestedPage : 1;
    void this.load();
  }

  ngOnDestroy(): void {
    this.destroyed = true;
  }

  async load(): Promise<void> {
    this.loading = true;
    this.error = '';
    try {
      const result = await this.api.listTurnOffs(
        this.deviceId,
        this.chatId,
        this.page,
      );
      if (this.destroyed) return;
      this.periods = result.items;
      this.page = result.page;
      this.hasPreviousPage = result.hasPreviousPage;
      this.hasNextPage = result.hasNextPage;
    } catch (error) {
      if (!this.destroyed) this.error = errorMessage(error);
    } finally {
      if (!this.destroyed) this.loading = false;
    }
  }

  async goToPage(page: number): Promise<void> {
    if (page < 1 || this.loading) return;
    await this.router.navigate([], {
      relativeTo: this.route,
      queryParams: page === 1 ? {} : { page },
    });
    this.page = page;
    await this.load();
  }

  duration(period: TurnOffPeriod): string {
    if (!period.endedAt) return 'Триває';
    const minutes = Math.max(
      0,
      Math.round(
        (Date.parse(period.endedAt) - Date.parse(period.startedAt)) / 60000,
      ),
    );
    const hours = Math.floor(minutes / 60);
    const remainder = minutes % 60;
    return hours ? `${hours} год ${remainder} хв` : `${remainder} хв`;
  }
}
