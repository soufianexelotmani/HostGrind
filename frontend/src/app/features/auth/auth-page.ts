import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { TranslocoDirective, TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { LucideEye as Eye, LucideEyeOff as EyeOff, LucideDynamicIcon } from '@lucide/angular';
import { AuthService } from '../../core/auth/auth.service';
import { AuthLayout } from '../../layouts/auth-layout/auth-layout';

type Mode = 'login' | 'forgot' | 'reset' | 'activate' | 'mfa' | 'security' | 'unauthorized' | 'forbidden';

@Component({
  selector: 'app-auth-page',
  imports: [ReactiveFormsModule, RouterLink, TranslocoDirective, TranslocoPipe, AuthLayout,
    ButtonModule, InputTextModule, LucideDynamicIcon],
  templateUrl: './auth-page.html',
  styleUrl: './auth-page.scss',
})
export class AuthPage {
  protected readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly mode = this.route.snapshot.data['mode'] as Mode;
  // Angular already decodes the fragment. The proof is Base64 and cannot contain the field delimiter.
  private readonly fragment = this.route.snapshot.fragment ?? '';
  private readonly proofStart = this.fragment.lastIndexOf('&token=');
  private readonly linkEmail = this.fragment.startsWith('email=') && this.proofStart >= 6
    ? this.fragment.slice(6, this.proofStart) : '';
  private readonly proof = this.linkEmail ? this.fragment.slice(this.proofStart + '&token='.length) : '';
  protected readonly form = new FormGroup({
    email: new FormControl(this.linkEmail, { nonNullable: true, validators: [Validators.required, Validators.email] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(256)] }),
    newPassword: new FormControl('', { nonNullable: true, validators: [Validators.minLength(12), Validators.maxLength(256)] }),
    code: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(40)] }),
    recovery: new FormControl(false, { nonNullable: true }),
  });
  protected readonly busy = signal(false);
  protected readonly error = signal(false);
  protected readonly notice = signal<'sent' | 'passwordSet' | ''>('');
  protected readonly visiblePassword = signal(false);
  protected readonly eye = Eye;
  protected readonly eyeOff = EyeOff;
  protected readonly togglePassword = (visible: boolean) => !visible;
  protected readonly setupKey = signal('');
  protected readonly recoveryCodes = signal<string[]>([]);
  protected readonly hasLink = !!this.proof;

  constructor() {
    if (this.mode === 'mfa' && this.auth.pending()?.status === 'mfa_enrollment_required') {
      void this.run(async () => {
        this.setupKey.set((await firstValueFrom(this.auth.enrollment())).setupKey);
      });
    }
    if (this.hasLink && (this.mode === 'activate' || this.mode === 'reset')) {
      history.replaceState(history.state, '', location.pathname + location.search);
    }
  }

  protected submit(): void {
    void this.run(async () => {
      const { email, password, code, recovery } = this.form.getRawValue();
      if (this.mode === 'login') {
        const result = await firstValueFrom(this.auth.login(email, password));
        this.form.controls.password.reset();
        await this.router.navigateByUrl(result.challenge ? '/mfa' : this.auth.home());
      } else if (this.mode === 'forgot') {
        await firstValueFrom(this.auth.resetRequest(email));
        this.notice.set('sent');
      } else if (this.mode === 'activate' || this.mode === 'reset') {
        await firstValueFrom(this.auth.completePasswordLink(this.mode === 'activate', email, this.proof, password));
        this.form.controls.password.reset();
        this.notice.set('passwordSet');
      } else if (this.mode === 'mfa') {
        const result = await firstValueFrom(this.auth.verify(code, recovery));
        this.form.controls.code.reset();
        this.setupKey.set('');
        if (result.recoveryCodes?.length) this.recoveryCodes.set(result.recoveryCodes);
        else await this.router.navigateByUrl(this.auth.home());
      }
    });
  }

  protected security(action: 'password' | 'enroll' | 'reset'): void {
    void this.run(async () => {
      const { password, newPassword, code, recovery } = this.form.getRawValue();
      if (action === 'enroll') {
        await firstValueFrom(this.auth.startMfa(password));
        await this.router.navigateByUrl('/mfa');
      } else {
        if (action === 'password') await firstValueFrom(this.auth.changePassword(password, newPassword));
        else await firstValueFrom(this.auth.resetMfa(password, code, recovery));
        await this.router.navigateByUrl('/login');
      }
      this.form.reset();
    });
  }

  private async run(action: () => Promise<void>): Promise<void> {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set(false);
    try {
      await action();
    } catch {
      this.error.set(true);
    } finally {
      this.busy.set(false);
    }
  }

}
