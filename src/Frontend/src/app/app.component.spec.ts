import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { BehaviorSubject } from 'rxjs';
import { AppComponent } from './app.component';
import { AuthService } from './auth.service';
import { DevicesService, Device } from './devices.service';

const device: Device = {
  deviceId: 'home', chatId: 'chat-a', isActive: true,
  settings: { description: 'Home', notificationDelaySec: 120,
    isDailyStatsEnabled: true, isWeeklyStatsEnabled: false, isMonthlyStatsEnabled: false }
};

describe('Management dashboard', () => {
  let fixture: ComponentFixture<AppComponent>;
  let auth: { user$: BehaviorSubject<unknown>; initialize: jasmine.Spy; signIn: jasmine.Spy; config: object };
  let api: jasmine.SpyObj<DevicesService>;

  beforeEach(async () => {
    auth = { user$: new BehaviorSubject<unknown>(null), initialize: jasmine.createSpy().and.resolveTo(),
      signIn: jasmine.createSpy().and.resolveTo(), config: {} };
    api = jasmine.createSpyObj('DevicesService', ['list', 'save']);
    api.list.and.resolveTo({ items: [structuredClone(device)] });
    api.save.and.resolveTo();
    await TestBed.configureTestingModule({
      imports: [FormsModule], declarations: [AppComponent],
      providers: [{ provide: AuthService, useValue: auth }, { provide: DevicesService, useValue: api }]
    }).compileComponents();
    fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  async function signIn(): Promise<void> {
    auth.user$.next({ profile: { email: 'person@example.test' } });
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('shows sign-in without requesting private data for anonymous visitors', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Sign in / create account');
    expect(api.list).not.toHaveBeenCalled();
  });

  it('loads assigned devices after sign-in and clears them on sign-out', async () => {
    await signIn();
    expect(fixture.nativeElement.textContent).toContain('Home');
    fixture.componentInstance.edit(fixture.componentInstance.devices[0]);
    auth.user$.next(null);
    fixture.detectChanges();
    expect(fixture.componentInstance.devices).toEqual([]);
    expect(fixture.componentInstance.draft).toBeUndefined();
  });

  it('edits a copy and leaves saved settings intact after a failed save', async () => {
    await signIn();
    const component = fixture.componentInstance;
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
    const component = fixture.componentInstance;
    component.edit(component.devices[0]);
    component.draft!.description = ' Updated ';
    await component.save();
    expect(api.save).toHaveBeenCalledWith('home', 'chat-a', { ...device.settings, description: 'Updated' });
    expect(component.devices[0].settings.description).toBe('Updated');
    expect(component.notice).toContain('Settings saved');
    expect(component.draft).toBeUndefined();
  });

  it('ignores a private-data response arriving after sign-out', async () => {
    let resolve!: (value: { items: Device[] }) => void;
    api.list.and.returnValue(new Promise(done => resolve = done));
    auth.user$.next({ profile: { email: 'person@example.test' } });
    auth.user$.next(null);
    resolve({ items: [device] });
    await fixture.whenStable();
    expect(fixture.componentInstance.devices).toEqual([]);
  });
});
