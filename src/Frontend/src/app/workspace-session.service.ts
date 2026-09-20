import { Injectable } from '@angular/core';
import { CurrentUser } from './users.service';

@Injectable({ providedIn: 'root' })
export class WorkspaceSession {
  currentUser?: CurrentUser;
  pendingWrites = 0;
}
