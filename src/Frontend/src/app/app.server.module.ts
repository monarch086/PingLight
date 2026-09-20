import { NgModule } from '@angular/core';
import { ServerModule } from '@angular/platform-server';
import { provideServerRendering, RenderMode, withRoutes } from '@angular/ssr';

import { AppModule } from './app.module';
import { AppComponent } from './app.component';

@NgModule({
  imports: [
    AppModule,
    ServerModule,
  ],
  bootstrap: [AppComponent],
  providers: [provideServerRendering(withRoutes([{ path: '**', renderMode: RenderMode.Server }]))],
})
export class AppServerModule {}
