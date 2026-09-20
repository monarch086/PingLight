import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DevicesPageComponent } from './devices-page.component';
import { UsersPageComponent } from './users-page.component';
import { WelcomeComponent } from './welcome.component';
import { WorkspaceSession } from './workspace-session.service';
import { BehaviorSubject } from 'rxjs';
import { AppComponent } from './app.component';
import { AuthService } from './auth.service';
import { DevicesService, Device } from './devices.service';
import { UsersService } from './users.service';
import { Router, RouterModule } from '@angular/router';
import { routes } from './app-routing.module';

const device: Device = {
  deviceId: 'home', chatId: 'chat-a', isActive: true,
  settings: { description: 'Home', notificationDelaySec: 120,
    isDailyStatsEnabled: true, isWeeklyStatsEnabled: false, isMonthlyStatsEnabled: false }
};

describe('Management dashboard', () => {
  let fixture: ComponentFixture<AppComponent>;
  let auth: { user$: BehaviorSubject<unknown>; initialize: jasmine.Spy; signIn: jasmine.Spy; config: object };
  let api: jasmine.SpyObj<DevicesService>;
  let usersApi: jasmine.SpyObj<UsersService>;

  beforeEach(async () => {
    auth = { user$: new BehaviorSubject<unknown>(null), initialize: jasmine.createSpy().and.resolveTo(),
      signIn: jasmine.createSpy().and.resolveTo(), config: {} };
    api = jasmine.createSpyObj('DevicesService', ['list', 'save', 'setActive']);
    api.list.and.resolveTo({ items: [structuredClone(device)] });
    api.save.and.resolveTo();
    api.setActive.and.resolveTo();
    usersApi = jasmine.createSpyObj('UsersService', ['me', 'list', 'setGrant']);
    usersApi.me.and.resolveTo({ userId: 'user-a', email: 'person@example.test', isSystemAdmin: false });
    usersApi.list.and.resolveTo({ items: [] });
    usersApi.setGrant.and.resolveTo();
    await TestBed.configureTestingModule({
    imports: [WelcomeComponent, RouterModule.forRoot(routes), AppComponent],
    providers: [{ provide: AuthService, useValue: auth }, { provide: DevicesService, useValue: api },
        { provide: UsersService, useValue: usersApi }]
}).compileComponents();
    fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    await TestBed.inject(Router).navigateByUrl('/devices');
  });

  async function settle(): Promise<void> {
    await fixture.whenStable();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function devicesPage(): DevicesPageComponent {
    return fixture.debugElement.query(By.directive(DevicesPageComponent)).componentInstance;
  }

  function usersPage(): UsersPageComponent {
    return fixture.debugElement.query(By.directive(UsersPageComponent)).componentInstance;
  }

  async function signIn(): Promise<void> {
    auth.user$.next({ profile: { email: 'person@example.test' } });
    await settle();
  }

  it('shows sign-in without requesting private data for anonymous visitors', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Sign in / create account');
    expect(api.list).not.toHaveBeenCalled();
    expect(usersApi.me).not.toHaveBeenCalled();
  });

  it('loads assigned devices after sign-in and clears them on sign-out', async () => {
    await signIn();
    expect(fixture.nativeElement.textContent).toContain('Home');
    const component = devicesPage();
    component.edit(component.devices[0]);
    auth.user$.next(null);
    fixture.detectChanges();
    expect(component.devices).toEqual([]);
    expect(component.draft).toBeUndefined();
    expect(fixture.debugElement.query(By.directive(DevicesPageComponent))).toBeNull();
  });

  it('edits a copy and leaves saved settings intact after a failed save', async () => {
    await signIn();
    const component = devicesPage();
    component.edit(component.devices[0]);
    component.draft!.description = 'Changed';
    api.save.and.rejectWith(new Error('Network unavailable'));
    await component.save();
    expect(component.devices[0].settings.description).toBe('Home');
    expect(component.draft!.description).toBe('Changed');
    expect(component.error).toBeTruthy();
  });

  it('saves only editable settings and confirms success', async () => {
    await signIn();
    const component = devicesPage();
    component.edit(component.devices[0]);
    component.draft!.description = ' Updated ';
    await component.save();
    expect(api.save).toHaveBeenCalledWith('home', 'chat-a', { ...device.settings, description: 'Updated' });
    expect(component.devices[0].settings.description).toBe('Updated');
    expect(component.notice).toContain('Settings saved');
    expect(component.draft).toBeUndefined();
  });

  it('disables notifications directly from the device card', async () => {
    await signIn();
    const component = devicesPage();
    const toggle = fixture.nativeElement.querySelector('.notification-toggle') as HTMLButtonElement;
    toggle.click();
    await settle();
    expect(api.setActive).toHaveBeenCalledWith('home', 'chat-a', false);
    expect(component.devices[0].isActive).toBeFalse();
    expect(toggle.textContent).toContain('Enable notifications');
    expect(component.notice).toContain('disabled');
  });

  it('keeps the notification state when the direct update fails', async () => {
    api.setActive.and.rejectWith(new Error('Network unavailable'));
    await signIn();
    const component = devicesPage();
    await component.setActive(component.devices[0], false);
    expect(component.devices[0].isActive).toBeTrue();
    expect(component.error).toBeTruthy();
    expect(TestBed.inject(WorkspaceSession).pendingWrites).toBe(0);
  });

  it('ignores a device response arriving after sign-out', async () => {
    let resolve!: (value: { items: Device[] }) => void;
    api.list.and.returnValue(new Promise(done => resolve = done));
    await signIn();
    const component = devicesPage();
    auth.user$.next(null);
    fixture.detectChanges();
    resolve({ items: [device] });
    await settle();
    expect(component.devices).toEqual([]);
    expect(TestBed.inject(WorkspaceSession).currentUser).toBeUndefined();
  });

  it('ignores an account response arriving after sign-out', async () => {
    let resolve!: (value: { userId: string; email: string; isSystemAdmin: boolean }) => void;
    usersApi.me.and.returnValue(new Promise(done => resolve = done));
    auth.user$.next({ profile: { email: 'person@example.test' } });
    auth.user$.next(null);
    resolve({ userId: 'user-a', email: 'person@example.test', isSystemAdmin: false });
    await settle();
    expect(TestBed.inject(WorkspaceSession).currentUser).toBeUndefined();
    expect(api.list).not.toHaveBeenCalled();
  });

  it('shows user access controls to system administrators and grants a device', async () => {
    usersApi.me.and.resolveTo({ userId: 'admin-a', email: 'admin@example.test', isSystemAdmin: true });
    usersApi.list.and.resolveTo({ items: [{ userId: 'user-a', email: 'person@example.test', isSystemAdmin: false, grants: [] }] });
    await signIn();
    await TestBed.inject(Router).navigateByUrl('/users');
    await settle();
    expect(fixture.nativeElement.textContent).toContain('User device access');
    const component = usersPage();
    const user = component.users[0];
    await component.setGrant(user, component.devices[0], true);
    expect(usersApi.setGrant).toHaveBeenCalledWith('user-a', 'home', 'chat-a', true);
    expect(component.hasGrant(user, component.devices[0])).toBeTrue();
  });

  it('opens the users page from its URL and preserves it when the app is recreated', async () => {
    usersApi.me.and.resolveTo({ userId: 'admin-a', email: 'admin@example.test', isSystemAdmin: true });
    await TestBed.inject(Router).navigateByUrl('/users');
    await signIn();
    fixture.destroy();
    fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    await settle();
    expect(fixture.nativeElement.querySelector('h1').textContent).toBe('User device access');
    expect(fixture.nativeElement.querySelector('.device-list')).toBeNull();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[aria-current="page"]').getAttribute('href')).toBe('/users');
  });

  it('navigates between devices and user access using links', async () => {
    usersApi.me.and.resolveTo({ userId: 'admin-a', email: 'admin@example.test', isSystemAdmin: true });
    await signIn();
    fixture.nativeElement.querySelector('nav a[href="/users"]').click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(TestBed.inject(Router).url).toBe('/users');
    await settle();
    expect(fixture.nativeElement.querySelector('h1').textContent).toBe('User device access');
    fixture.nativeElement.querySelector('nav a[href="/devices"]').click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(TestBed.inject(Router).url).toBe('/devices');
    expect(fixture.nativeElement.querySelector('h1').textContent).toBe('All devices');
  });

  it('redirects non-admin users away from user access after loading their role', async () => {
    await TestBed.inject(Router).navigateByUrl('/users');
    await signIn();
    await settle();
    expect(TestBed.inject(Router).url).toBe('/devices');
    expect(fixture.nativeElement.querySelector('nav a[href="/users"]')).toBeNull();
    expect(usersApi.list).not.toHaveBeenCalled();
  });
});
