import { Routes } from '@angular/router';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { HistoryComponent } from './features/history/history.component';
import { LoginComponent } from './features/auth/login/login.component';

export const routes: Routes = [
    {
        path: '',
        component: DashboardComponent
    },
    {
        path: 'history',
        component: HistoryComponent
    },
    {
        path: 'login',
        component: LoginComponent
    }
];