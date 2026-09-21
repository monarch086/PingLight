import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DevicesPageComponent } from './devices-page.component';
import { UsersPageComponent } from './users-page.component';
import { TurnOffHistoryComponent } from './turn-off-history.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'devices' },
  { path: 'devices', component: DevicesPageComponent, title: 'Пристрої · PingLight' },
  { path: 'devices/:deviceId/destinations/:chatId/turn-offs', component: TurnOffHistoryComponent, title: 'Історія відключень · PingLight' },
  { path: 'users', component: UsersPageComponent, title: 'Доступ користувачів · PingLight' },
  { path: '**', redirectTo: 'devices' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, {
    initialNavigation: 'enabledBlocking'
})],
  exports: [RouterModule]
})
export class AppRoutingModule { }
