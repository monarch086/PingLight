import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DevicesPageComponent } from './devices-page.component';
import { UsersPageComponent } from './users-page.component';
import { AuthCallbackComponent } from './auth-callback.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'devices' },
  { path: 'devices', component: DevicesPageComponent, title: 'Devices · PingLight' },
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
