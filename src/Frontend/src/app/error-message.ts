import { HttpErrorResponse } from '@angular/common/http';

export function errorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 401) return 'Your session has ended. Please sign in again.';
    if (error.status === 404 || error.status === 403) return 'This device is no longer available to your account.';
    if (error.status === 400) return 'Please check your settings and try again.';
    return 'Unable to reach your devices. Please try again.';
  }
  return error instanceof Error ? error.message : 'Something went wrong. Please try again.';
}
