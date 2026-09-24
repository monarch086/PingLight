import { NgSelectComponent } from '@ng-select/ng-select';
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
import { TurnOffHistoryComponent } from './turn-off-history.component';

const device: Device = {
  deviceId: 'home',
  chatId: 'chat-a',
  isActive: true,
  lastTurnOff: {
    startedAt: '2026-09-20T10:00:00Z',
    endedAt: '2026-09-20T11:30:00Z',
  },
  settings: {
    description: 'Home',
    notificationDelaySec: 120,
    isDailyStatsEnabled: true,
    isWeeklyStatsEnabled: false,
    isMonthlyStatsEnabled: false,
  },
};

describe('Management dashboard', () => {
  let fixture: ComponentFixture<AppComponent>;
  let auth: {
    user$: BehaviorSubject<unknown>;
    initialize: jasmine.Spy;
    signIn: jasmine.Spy;
    config: object;
  };
  let api: jasmine.SpyObj<DevicesService>;
  let usersApi: jasmine.SpyObj<UsersService>;

  beforeEach(async () => {
    auth = {
      user$: new BehaviorSubject<unknown>(null),
      initialize: jasmine.createSpy().and.resolveTo(),
      signIn: jasmine.createSpy().and.resolveTo(),
      config: {},
    };
    api = jasmine.createSpyObj('DevicesService', [
      'list',
      'save',
      'setActive',
      'listTurnOffs',
      'removeLastTurnOff',
    ]);
    api.list.and.resolveTo({ items: [structuredClone(device)] });
    api.save.and.resolveTo();
    api.setActive.and.resolveTo();
    api.listTurnOffs.and.resolveTo({
      items: [device.lastTurnOff!],
      page: 1,
      hasPreviousPage: false,
      hasNextPage: true,
    });
    api.removeLastTurnOff.and.resolveTo();
    usersApi = jasmine.createSpyObj('UsersService', ['me', 'list', 'setGrant']);
    usersApi.me.and.resolveTo({
      userId: 'user-a',
      email: 'person@example.test',
      isSystemAdmin: false,
    });
    usersApi.list.and.resolveTo({ items: [] });
    usersApi.setGrant.and.resolveTo();
    await TestBed.configureTestingModule({
      imports: [WelcomeComponent, RouterModule.forRoot(routes), AppComponent],
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: DevicesService, useValue: api },
        { provide: UsersService, useValue: usersApi },
      ],
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
    return fixture.debugElement.query(By.directive(DevicesPageComponent))
      .componentInstance;
  }

  function usersPage(): UsersPageComponent {
    return fixture.debugElement.query(By.directive(UsersPageComponent))
      .componentInstance;
  }

  async function signIn(): Promise<void> {
    auth.user$.next({ profile: { email: 'person@example.test' } });
    await settle();
  }

  it('shows sign-in without requesting private data for anonymous visitors', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(
      'Увійти / створити обліковий запис',
    );
    expect(api.list).not.toHaveBeenCalled();
    expect(usersApi.me).not.toHaveBeenCalled();
  });

  it('opens the PingLight sign-in form without an external redirect', async () => {
    await settle();
    (
      fixture.nativeElement.querySelector(
        '.welcome button',
      ) as HTMLButtonElement
    ).click();
    await settle();
    expect(fixture.nativeElement.querySelector('app-auth-page')).not.toBeNull();
    expect(
      fixture.nativeElement.querySelector('input[autocomplete="email"]'),
    ).not.toBeNull();
    expect(
      fixture.nativeElement.querySelector(
        'input[autocomplete="current-password"]',
      ),
    ).not.toBeNull();
    expect(
      fixture.nativeElement.querySelector('a[href*="amazoncognito"]'),
    ).toBeNull();
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
    expect(
      fixture.debugElement.query(By.directive(DevicesPageComponent)),
    ).toBeNull();
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

  it('shows only the editor while changing device settings', async () => {
    api.list.and.resolveTo({
      items: [
        structuredClone(device),
        {
          ...structuredClone(device),
          deviceId: 'office',
          chatId: 'chat-b',
        },
      ],
    });
    await signIn();
    expect(fixture.nativeElement.querySelectorAll('.device').length).toBe(2);
    (
      fixture.nativeElement.querySelector('.device .full') as HTMLButtonElement
    ).click();
    await settle();
    expect(fixture.nativeElement.querySelectorAll('.device').length).toBe(0);
    expect(fixture.nativeElement.querySelector('.editor')).not.toBeNull();
    (
      fixture.nativeElement.querySelector(
        '.editor button[type="button"]',
      ) as HTMLButtonElement
    ).click();
    await settle();
    expect(fixture.nativeElement.querySelectorAll('.device').length).toBe(2);
    expect(fixture.nativeElement.querySelector('.editor')).toBeNull();
  });

  it('saves only editable settings and confirms success', async () => {
    await signIn();
    const component = devicesPage();
    component.edit(component.devices[0]);
    component.draft!.description = ' Updated ';
    await component.save();
    expect(api.save).toHaveBeenCalledWith('home', 'chat-a', {
      ...device.settings,
      description: 'Updated',
    });
    expect(component.devices[0].settings.description).toBe('Updated');
    expect(component.notice).toContain('Налаштування збережено');
    expect(component.draft).toBeUndefined();
  });

  it('disables notifications directly from the device card', async () => {
    await signIn();
    const component = devicesPage();
    const toggle = fixture.nativeElement.querySelector(
      '.notification-toggle',
    ) as HTMLInputElement;
    toggle.click();
    await settle();
    expect(api.setActive).toHaveBeenCalledWith('home', 'chat-a', false);
    expect(component.devices[0].isActive).toBeFalse();
    expect(toggle.checked).toBeFalse();
    expect(
      fixture.nativeElement.querySelector('.notification-state').textContent,
    ).toContain('Вимкнено');
    expect(component.notice).toContain('вимкнено');
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

  it('shows the last turn-off and opens paged turn-off history', async () => {
    await signIn();
    expect(
      fixture.nativeElement.querySelector('.last-turn-off').textContent,
    ).toContain('1 год 30 хв');
    fixture.nativeElement.querySelector('.history-link').click();
    await settle();
    expect(TestBed.inject(Router).url).toContain(
      '/devices/home/destinations/chat-a/turn-offs',
    );
    expect(
      fixture.debugElement.query(By.directive(TurnOffHistoryComponent)),
    ).not.toBeNull();
    expect(api.listTurnOffs).toHaveBeenCalledWith('home', 'chat-a', 1);
    expect(fixture.nativeElement.querySelectorAll('.period').length).toBe(1);
    (
      fixture.nativeElement.querySelector(
        '.pagination button:last-child',
      ) as HTMLButtonElement
    ).click();
    await settle();
    expect(TestBed.inject(Router).url).toContain('page=2');
    expect(api.listTurnOffs).toHaveBeenCalledWith('home', 'chat-a', 2);
  });

  it('removes the last turn-off from a device card and refreshes the device', async () => {
    api.list.and.returnValues(
      Promise.resolve({ items: [structuredClone(device)] }),
      Promise.resolve({
        items: [{ ...structuredClone(device), lastTurnOff: null }],
      }),
    );
    await signIn();
    (
      fixture.nativeElement.querySelector('.danger-link') as HTMLButtonElement
    ).click();
    await settle();
    expect(fixture.nativeElement.querySelector('dialog.modal')).not.toBeNull();
    expect(api.removeLastTurnOff).not.toHaveBeenCalled();
    (
      fixture.nativeElement.querySelector('.modal .danger') as HTMLButtonElement
    ).click();
    await settle();
    expect(api.removeLastTurnOff).toHaveBeenCalledWith('home', 'chat-a');
    expect(api.list).toHaveBeenCalledTimes(2);
    expect(
      fixture.nativeElement.querySelector('.last-turn-off').textContent,
    ).toContain('Відключень не зафіксовано');
    expect(devicesPage().notice).toContain('видалено');
  });

  it('ignores a device response arriving after sign-out', async () => {
    let resolve!: (value: { items: Device[] }) => void;
    api.list.and.returnValue(new Promise((done) => (resolve = done)));
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
    let resolve!: (value: {
      userId: string;
      email: string;
      isSystemAdmin: boolean;
    }) => void;
    usersApi.me.and.returnValue(new Promise((done) => (resolve = done)));
    auth.user$.next({ profile: { email: 'person@example.test' } });
    auth.user$.next(null);
    resolve({
      userId: 'user-a',
      email: 'person@example.test',
      isSystemAdmin: false,
    });
    await settle();
    expect(TestBed.inject(WorkspaceSession).currentUser).toBeUndefined();
    expect(api.list).not.toHaveBeenCalled();
  });

  it('saves edited device grants only after Save and discards cancelled changes', async () => {
    usersApi.me.and.resolveTo({
      userId: 'admin-a',
      email: 'admin@example.test',
      isSystemAdmin: true,
    });
    usersApi.list.and.resolveTo({
      items: [
        {
          userId: 'user-a',
          email: 'person@example.test',
          isSystemAdmin: false,
          grants: [],
        },
      ],
    });
    await signIn();
    await TestBed.inject(Router).navigateByUrl('/users');
    await settle();
    expect(fixture.nativeElement.textContent).toContain(
      'Доступ користувачів до пристроїв',
    );
    const component = usersPage();
    const user = component.users[0];
    component.edit(user);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('ng-select')).not.toBeNull();
    fixture.debugElement
      .query(By.directive(NgSelectComponent))
      .componentInstance.open();
    await settle();
    expect(document.querySelector('.ng-dropdown-panel')?.textContent).toContain(
      'home · chat-a',
    );
    component.draftKeys = [component.deviceOptions[0].key];
    expect(component.hasChanges(user)).toBeTrue();
    expect(usersApi.setGrant).not.toHaveBeenCalled();
    component.cancelEdit();
    expect(user.grants).toEqual([]);
    expect(usersApi.setGrant).not.toHaveBeenCalled();

    component.edit(user);
    component.draftKeys = [component.deviceOptions[0].key];
    await component.save(user);
    expect(usersApi.setGrant).toHaveBeenCalledOnceWith(
      'user-a',
      'home',
      'chat-a',
      true,
    );
    expect(user.grants).toEqual([{ deviceId: 'home', chatId: 'chat-a' }]);
    expect(component.editingUserId).toBe('');

    component.edit(user);
    component.draftKeys = [];
    await component.save(user);
    expect(usersApi.setGrant).toHaveBeenCalledWith(
      'user-a',
      'home',
      'chat-a',
      false,
    );
    expect(user.grants).toEqual([]);
  });

  it('keeps the editor open after a failed grant save so it can be retried', async () => {
    usersApi.me.and.resolveTo({
      userId: 'admin-a',
      email: 'admin@example.test',
      isSystemAdmin: true,
    });
    usersApi.list.and.resolveTo({
      items: [
        {
          userId: 'user-a',
          email: 'person@example.test',
          isSystemAdmin: false,
          grants: [],
        },
      ],
    });
    await signIn();
    await TestBed.inject(Router).navigateByUrl('/users');
    await settle();
    const component = usersPage();
    const user = component.users[0];
    component.edit(user);
    component.draftKeys = [component.deviceOptions[0].key];
    usersApi.setGrant.and.rejectWith(new Error('Save failed'));
    await component.save(user);
    expect(component.editingUserId).toBe('user-a');
    expect(component.hasChanges(user)).toBeTrue();
    expect(user.grants).toEqual([]);
  });

  it('opens the users page from its URL and preserves it when the app is recreated', async () => {
    usersApi.me.and.resolveTo({
      userId: 'admin-a',
      email: 'admin@example.test',
      isSystemAdmin: true,
    });
    await TestBed.inject(Router).navigateByUrl('/users');
    await signIn();
    fixture.destroy();
    fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    await settle();
    expect(fixture.nativeElement.querySelector('h1').textContent).toBe(
      'Доступ користувачів до пристроїв',
    );
    expect(fixture.nativeElement.querySelector('.device-list')).toBeNull();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(
      fixture.nativeElement
        .querySelector('[aria-current="page"]')
        .getAttribute('href'),
    ).toBe('/users');
  });

  it('navigates between devices and user access using links', async () => {
    usersApi.me.and.resolveTo({
      userId: 'admin-a',
      email: 'admin@example.test',
      isSystemAdmin: true,
    });
    await signIn();
    fixture.nativeElement.querySelector('nav a[href="/users"]').click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(TestBed.inject(Router).url).toBe('/users');
    await settle();
    expect(fixture.nativeElement.querySelector('h1').textContent).toBe(
      'Доступ користувачів до пристроїв',
    );
    fixture.nativeElement.querySelector('nav a[href="/devices"]').click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(TestBed.inject(Router).url).toBe('/devices');
    expect(fixture.nativeElement.querySelector('h1').textContent).toBe(
      'Усі пристрої',
    );
  });

  it('redirects non-admin users away from user access after loading their role', async () => {
    await TestBed.inject(Router).navigateByUrl('/users');
    await signIn();
    await settle();
    expect(TestBed.inject(Router).url).toBe('/devices');
    expect(
      fixture.nativeElement.querySelector('nav a[href="/users"]'),
    ).toBeNull();
    expect(usersApi.list).not.toHaveBeenCalled();
  });
});
