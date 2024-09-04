import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { FormsModule } from '@angular/forms';
import { ThemeSwitcherService } from './theme-switcher.service';

import { Component, EventEmitter, Output } from '@angular/core';

@Component({
  selector: 'app-theme-switcher',
  standalone: true,
  templateUrl: './theme-switcher.component.html',
  styleUrls: ['./theme-switcher.component.scss'],
  imports: [NgbModule, FormsModule]
})
export class ThemeSwitcherComponent {
  @Output() themeChanged = new EventEmitter<boolean>();

  constructor(public themeSwitcher: ThemeSwitcherService) {}

  toggleTheme() {
    this.themeSwitcher.toggleTheme();
    this.themeChanged.emit();
  }
}
