import { Injectable, inject, NgZone, OnDestroy } from '@angular/core';
import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { AuthService } from './auth.service';
import { ToastService } from '../../shared/components/toast/toast.service';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class NotificationService implements OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);
  private readonly zone = inject(NgZone);
  private hubConnection: HubConnection | null = null;

  readonly orderStatusUpdates$ = new Subject<{ orderId: string; status: string }>();

  startConnection(): void {
    const url = environment.apiBaseUrl.replace('/api', '') + '/hubs/notifications';

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveNotification', (message: string) => {
      this.zone.run(() => {
        this.toastService.success(message);
      });
    });

    this.hubConnection.on('OrderStatusUpdated', (orderId: string, newStatus: string) => {
      this.zone.run(() => {
        this.orderStatusUpdates$.next({ orderId, status: newStatus });
      });
    });

    this.hubConnection.start().catch(() => {});
  }

  ngOnDestroy(): void {
    this.hubConnection?.stop();
    this.orderStatusUpdates$.complete();
  }
}
