import { AfterViewInit, Component, OnDestroy } from '@angular/core';
import { ThemeSwitcherService } from '../theme-switcher/theme-switcher.service';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-documentation',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './documentation.component.html',
  styleUrl: './documentation.component.scss'
})
export class DocumentationComponent implements AfterViewInit, OnDestroy {
  public sections:{id:string, title:string}[] = [];
  private observer:IntersectionObserver|null = null;
  public activeIds:string[] = [];

  constructor(public themeSwitcherService:ThemeSwitcherService) {
  }

  ngAfterViewInit(): void {
    const sections: HTMLElement[] = Array.from(document.querySelectorAll('section'));

    this.sections = sections.map((section, i) => {
      let id: string = section.id;
      if (id === '') {
        id = `section-${i + 1}`;
        section.id = id;
      }
      const heading: HTMLElement | null = section.querySelector('h3');
      const title: string = heading?.innerText?.trim() || id;
      return { id, title };
    });

    // Keep a Set of currently-intersecting IDs
    const currentlyActive = new Set<string>();

    this.observer = new IntersectionObserver(
      entries => {
        for (const entry of entries) {
          const sectionId = (entry.target as HTMLElement).id;
          if (entry.isIntersecting) {
            currentlyActive.add(sectionId);
          } else {
            currentlyActive.delete(sectionId);
          }
        }
        // Convert to array. Optionally, sort according to your desired order.
        this.activeIds = Array.from(currentlyActive);
      },
      { root: null, rootMargin: '-10% 0px -10% 0px', threshold: [0.25] }
    );
    sections.forEach(section => {
      this.observer?.observe(section);
    });
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }
}
