import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-welcome',
  templateUrl: './welcome.component.html',
  styleUrl: './welcome.component.scss'
})
export class WelcomeComponent {
  @Input() ready = false;
  @Input() configured = false;
  @Output() signIn = new EventEmitter<void>();
}
