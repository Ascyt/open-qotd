import { Component, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet, RouterModule, RouterLinkActive, ActivatedRoute } from '@angular/router';
import { ThemeSwitcherComponent } from './theme-switcher/theme-switcher.component';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { ThemeSwitcherService } from './theme-switcher/theme-switcher.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, ThemeSwitcherComponent, RouterModule, RouterLinkActive, NgbModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  isCollapsed:boolean = false;
  private cycleRoutes:string[] = ['home','documentation','about','changelog'];

  constructor(public router:Router, public themeSwitcher:ThemeSwitcherService, public route:ActivatedRoute) {
    this.updateTheme();
  }

  @HostListener('window:keydown', ['$event'])
  handleKeyDown(event:KeyboardEvent):void {
    const routeIndex = this.cycleRoutes.indexOf(this.router.url.split('/')[1]);
    if (routeIndex === -1) 
    {
      if (event.key === 'ArrowRight') {
        event.preventDefault();
        this.router.navigate([this.cycleRoutes[0]]);
      }
      if (event.key === 'ArrowLeft') {
        event.preventDefault();
        this.router.navigate([this.cycleRoutes[this.cycleRoutes.length - 1]]);
      }
    }
    if (event.key === 'ArrowRight') {
      this.router.navigate([this.cycleRoutes[routeIndex + 1] || this.cycleRoutes[routeIndex]]);
    }
    if (event.key === 'ArrowLeft') {
      this.router.navigate([this.cycleRoutes[routeIndex - 1] || this.cycleRoutes[routeIndex]]);
    }
  }

  toggleTheme() {
    this.updateTheme();
  }
  updateTheme() {
    switch (this.themeSwitcher.themeSwitchCounter) { // ignore this
      case 10:
        alert('bro stop')
        break;
      case 20:
        alert('seriously')
        break;
      case 30:
        alert('stop it')
        break;
      case 40:
        alert('you\'re gonna break it')
        break;
      case 50:
        document.body.className = 'what'
        alert('great. you broke it.')
        return;
      case 60:
        alert('it\'s broken')
        return;
      case 70:
        alert('it\'s still broken')
        return;
      case 80:
        alert('it\'s no use man. it\'s broken')
        return;
      case 90:
        alert('you\'re still here?')
        return;
      case 100:
        alert('fine. you win. i\'ll fix it.')
        this.themeSwitcher.themeSwitchCounter = 0;
        break;
    } if (this.themeSwitcher.themeSwitchCounter > 50) return;


    document.body.className = this.themeSwitcher.themeClass;
  }
}
