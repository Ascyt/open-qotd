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
  private activeId:string = '';

  constructor(public themeSwitcherService:ThemeSwitcherService) {
  }

  ngAfterViewInit(): void {
    const sections:HTMLElement[] = Array.from(document.querySelectorAll('section'));
    
    this.sections = sections.map((section, i) => {
      let id:string = (section as HTMLElement).id;
      if (id === '') {
        id = `section-${i + 1}`;
        (section as HTMLElement).id = id;
      }
      
      const heading:HTMLElement|null = section.querySelector('h1,h2,h3');
      const title:string = heading?.innerText?.trim() || id;
      return { id, title };
    });

    this.observer = new IntersectionObserver(
      entries => {
        entries.forEach(entry => {
          if (entry.isIntersecting) {
            // old element deactivation
            if (this.activeId !== '') {
              const oldElement:HTMLElement|null = document.getElementById(this.activeId);
              if (oldElement !== null) {
                oldElement.classList.remove('active');
              }
            }

            // new element activation
            const element:HTMLElement = entry.target as HTMLElement;
            element.classList.add('active');
            this.activeId = element.id;
          }
        });
      },
      { root: null, rootMargin: '0px 0px -60% 0px', threshold: [0.1, 0.5] }
    );

    sections.forEach(section => {
      this.observer?.observe(section);
    });
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }
}
