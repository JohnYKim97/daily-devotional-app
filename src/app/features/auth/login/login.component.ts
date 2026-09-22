import { Component, inject } from '@angular/core';

import { AuthService } from '../../../core/services/auth.service';
import { LighthouseIconComponent } from '../../../shared/lighthouse-icon/lighthouse-icon.component';

@Component({
  selector: 'app-login',
  imports: [LighthouseIconComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  protected authService = inject(AuthService);
}
