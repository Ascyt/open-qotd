import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { RedirectGuard } from './redirect.guard';
import { RedirectingComponent } from './redirecting/redirecting.component';
import { NotFoundComponent } from './not-found/not-found.component';
import { PrivacyPolicyComponent } from './privacy-policy/privacy-policy.component';
import { TermsOfServiceComponent } from './terms-of-service/terms-of-service.component';
import { DocumentationComponent } from './documentation/documentation.component';
import { AboutComponent } from './about/about.component';

export const routes: Routes = [
    {path: '', redirectTo: '/home', pathMatch: 'full'},

    {path: 'home', component: HomeComponent},
    {path: 'documentation', component: DocumentationComponent},
    {path: 'about', component: AboutComponent},

    {path: 'source', component: RedirectingComponent, canActivate: [RedirectGuard], data: {externalUrl: 'https://github.com/Ascyt/open-qotd'}},
    {path: 'license', component: RedirectingComponent, canActivate: [RedirectGuard], data: {externalUrl: 'https://github.com/Ascyt/open-qotd/blob/main/LICENSE'}},
    {path: 'src', redirectTo: '/source', pathMatch: 'full'},

    {path: 'privacy-policy', component: PrivacyPolicyComponent},
    {path: 'terms-of-service', component:TermsOfServiceComponent},

    {path: 'community', component: RedirectingComponent, canActivate: [RedirectGuard], data: {externalUrl: 'https://discord.gg/85TtrwuKn8'}},
    {path: 'add', component: RedirectingComponent, canActivate: [RedirectGuard], data: {externalUrl: 'https://discord.com/oauth2/authorize?client_id=1275472589375930418&permissions=141312&integration_type=0&scope=applications.commands+bot'}},

    {path: '**', component: NotFoundComponent}
];
