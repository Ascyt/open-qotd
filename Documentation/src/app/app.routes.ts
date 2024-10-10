import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { RedirectGuard } from './redirect.guard';
import { RedirectingComponent } from './redirecting/redirecting.component';
import { NotFoundComponent } from './not-found/not-found.component';
import { PrivacyPolicyComponent } from './privacy-policy/privacy-policy.component';
import { TermsOfServiceComponent } from './terms-of-service/terms-of-service.component';

export const routes: Routes = [
    {path: '', redirectTo: '/home', pathMatch: 'full'},

    {path: 'home', component: HomeComponent},
    {path: 'source', component: RedirectingComponent, canActivate: [RedirectGuard], data: {externalUrl: 'https://github.com/Ascyt/open-qotd'}},
    {path: 'license', component: RedirectingComponent, canActivate: [RedirectGuard], data: {externalUrl: 'https://github.com/Ascyt/open-qotd/blob/main/LICENSE'}},
    {path: 'src', redirectTo: '/source', pathMatch: 'full'},

    {path: 'privacy-policy', component: PrivacyPolicyComponent},
    {path: 'terms-of-service', component:TermsOfServiceComponent},

    {path: '**', component: NotFoundComponent}
];
