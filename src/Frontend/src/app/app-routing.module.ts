import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DevicesPageComponent } from './devices-page.component';
import { UsersPageComponent } from './users-page.component';
import { AuthCallbackComponent } from './auth-callback.component';
import { TurnOffHistoryComponent } from './turn-off-history.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'devices' },
  { path: 'devices', component: DevicesPageComponent, title: 'Devices · PingLight' },
  { path: 'devices/:deviceId/destinations/:chatId/turn-offs', component: TurnOffHistoryComponent, title: 'Turn-off history · PingLight' },
  { path: 'users', component: UsersPageComponent, title: 'User device access · PingLight' },
  { path: 'auth/callback', component: AuthCallbackComponent },
  { path: '**', redirectTo: 'devices' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, {
    initialNavigation: 'enabledBlocking'
})],
  exports: [RouterModule]
})
export class AppRoutingModule { }
