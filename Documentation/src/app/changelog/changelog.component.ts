import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ThemeSwitcherService } from '../theme-switcher/theme-switcher.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-changelog',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './changelog.component.html',
  styleUrl: './changelog.component.scss'
})
export class ChangelogComponent {
  constructor(public themeSwitcherService:ThemeSwitcherService) {}
}
